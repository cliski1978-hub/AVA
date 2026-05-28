using System.Collections.ObjectModel;
using System.Text.Json;
using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.ChatContext.Services;
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models;
using AVA.UI.CORE.Models.Chat;
using AVA.UI.CORE.Models.UI;

namespace AVA.UI.Features.Chat.State;

/// <summary>
/// Snapshot of session context captured at send time so responses are persisted
/// to the correct session even if the user navigates away before the model replies.
/// </summary>
public record ChatSessionContext(string? SessionId, string? VaultId, string? ProjectId);

/// <summary>
/// Runtime state for all chat conversations.
/// Owns per-model message history, output segments, and prompt preview state.
/// Writes every message to ISessionChatHistoryService for full session recovery.
/// </summary>
public class ChatConversationState
{
    private readonly ISessionChatHistoryService _chatHistory;
    private readonly IAvaIdService _ids;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Active session context ────────────────────────────────────────────────
    public string? ActiveSessionId   { get; set; }
    public string? ActiveSessionName { get; set; }
    public string? ActiveVaultId     { get; set; }
    public string? ActiveProjectId   { get; set; }

    // ── Index — loaded on startup, never auto-loads full sessions ─────────────
    public List<ChatSessionIndexItem> SessionIndex { get; private set; } = new();

    public ChatConversationState(ISessionChatHistoryService chatHistory, IAvaIdService ids)
    {
        _chatHistory = chatHistory;
        _ids         = ids;
    }

    /// <summary>
    /// Called once on startup. Loads index only.
    /// Full session logs are NOT loaded until the user selects a session.
    /// </summary>
    public async Task LoadSessionIndexAsync()
    {
        SessionIndex = await _chatHistory.GetIndexAsync();
        Notify();
    }

    /// <summary>
    /// Captures the current session context. Call this immediately before an async model
    /// send so the response can be persisted to the correct session if the user navigates away.
    /// </summary>
    public ChatSessionContext CaptureContext()
        => new(ActiveSessionId, ActiveVaultId, ActiveProjectId);

    /// <summary>
    /// Loads the full session log only when the user selects it.
    /// Returns null if the session does not exist.
    /// </summary>
    public async Task<SessionChatHistory?> LoadSelectedSessionAsync(string sessionId)
    {
        await _chatHistory.SetActiveSessionAsync(sessionId);
        var history = await _chatHistory.LoadHistoryAsync(sessionId, ActiveVaultId, ActiveProjectId);
        RestoreFromHistory(history);
        return history;
    }

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, List<Message>> _modelConversations =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _persistedBroadcastUserTurns =
        new(StringComparer.OrdinalIgnoreCase);

    // Tracks which models are currently awaiting a response — persists across VM re-creation.
    private readonly HashSet<string> _typingModels = new(StringComparer.OrdinalIgnoreCase);

    public void SetTyping(string modelId, bool typing)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return;
        if (typing) _typingModels.Add(modelId);
        else        _typingModels.Remove(modelId);
    }

    public bool IsTyping(string modelId)
        => !string.IsNullOrWhiteSpace(modelId) && _typingModels.Contains(modelId);

    // Tracks which workspace session has an in-flight Chat send — persists across VM re-creation.
    public string? ChatSendingSessionId { get; private set; }

    public void SetChatSending(bool sending, string? sessionId = null)
        => ChatSendingSessionId = sending ? sessionId : null;

    public bool IsChatSendingFor(string? sessionId)
        => !string.IsNullOrWhiteSpace(ChatSendingSessionId)
        && ChatSendingSessionId == sessionId;

    public ObservableCollection<OutputSegment> OutputSegments { get; } = new();

    public string PromptPreviewText    { get; set; } = string.Empty;
    public int    PromptTokenEstimate  { get; set; }
    public bool   ShowPromptPreview    { get; set; }

    // ── Conversation Access ───────────────────────────────────────────────────
    public IReadOnlyList<Message> GetConversation(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return Array.Empty<Message>();

        return GetOrCreate(modelId);
    }

    public List<Message> GetBroadcastConversation(IEnumerable<string> modelIds)
    {
        var ordered = modelIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(id => GetConversation(id))
            .OrderBy(m => m.Timestamp)
            .ToList();

        var seenTurns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged    = new List<Message>(ordered.Count);

        foreach (var msg in ordered)
        {
            if (!string.Equals(msg.Role, "user", StringComparison.OrdinalIgnoreCase))
            {
                merged.Add(msg);
                continue;
            }

            if (string.IsNullOrWhiteSpace(msg.TurnId))
            {
                merged.Add(msg);
                continue;
            }

            if (seenTurns.Add(msg.TurnId))
                merged.Add(msg);
        }

        return merged;
    }

    // ── Conversation Mutations ────────────────────────────────────────────────
    public void AppendUserMessage(
        string modelId,
        string content,
        string? modelLabel = null,
        string? turnId     = null)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return;

        var message = new Message
        {
            MessageId  = _ids.NewMessageId(),
            Role       = SessionChatRole.User,
            Content    = content,
            ModelId    = modelId,
            ModelLabel = modelLabel,
            TurnId     = turnId
        };

        GetOrCreate(modelId).Add(message);

        var persistSessionId = ActiveSessionId;
        if (ShouldPersistUserMessage(persistSessionId, message))
            _ = _chatHistory.AddMessageAsync(
                    persistSessionId!, ToHistory(message), ActiveVaultId, ActiveProjectId);

        Notify();
    }

    public void AppendAssistantMessage(
        string modelId,
        string content,
        string? modelLabel    = null,
        bool   isError        = false,
        string? turnId        = null,
        Dictionary<string, object>? responseMetadata = null,
        IEnumerable<ChatToolCallLog>? toolCalls      = null,
        bool   requiresApproval  = false,
        string? approvalTitle    = null,
        string? approvalDetails  = null,
        string? approvalStatus   = null,
        ChatSessionContext? sessionContext = null)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return;

        var message = new Message
        {
            MessageId        = _ids.NewMessageId(),
            Role             = SessionChatRole.Assistant,
            Content          = content,
            ModelId          = modelId,
            ModelLabel       = modelLabel,
            IsError          = isError,
            TurnId           = turnId,
            ResponseMetadata = responseMetadata == null
                                   ? new Dictionary<string, object>()
                                   : new Dictionary<string, object>(responseMetadata),
            ToolCalls        = toolCalls?.ToList() ?? new List<ChatToolCallLog>(),
            RequiresApproval = requiresApproval,
            ApprovalTitle    = approvalTitle,
            ApprovalDetails  = approvalDetails,
            ApprovalStatus   = approvalStatus
        };

        // Reply arrived — model is no longer typing.
        _typingModels.Remove(modelId);

        // Use captured context for persistence so the reply always lands in the
        // correct session file even if the user switched sessions mid-await.
        var persistId      = sessionContext?.SessionId ?? ActiveSessionId;
        var persistVaultId = sessionContext?.VaultId   ?? ActiveVaultId;
        var persistProjId  = sessionContext?.ProjectId ?? ActiveProjectId;

        if (persistId != null)
            _ = _chatHistory.AddMessageAsync(persistId, ToHistory(message), persistVaultId, persistProjId);

        // Only update in-memory conversation when the user is still on the same session.
        // If they've switched away, the persisted reply will appear when they return.
        if (sessionContext == null || sessionContext.SessionId == ActiveSessionId)
        {
            GetOrCreate(modelId).Add(message);
            Notify();
        }
    }

    /// <summary>
    /// Clears runtime state and hydrates _modelConversations from a persisted history.
    /// Called on session load and session switch.
    /// </summary>
    public void RestoreFromHistory(SessionChatHistory? history)
    {
        _modelConversations.Clear();
        _persistedBroadcastUserTurns.Clear();
        OutputSegments.Clear();

        if (history == null)
        {
            Notify();
            return;
        }

        ActiveSessionId   = history.SessionId;
        ActiveSessionName = history.Title;
        ActiveVaultId     = history.VaultId   ?? ActiveVaultId;
        ActiveProjectId   = history.ProjectId;

        foreach (var item in history.Messages.OrderBy(m => m.Timestamp))
        {
            var message = FromHistory(item);
            if (string.IsNullOrWhiteSpace(message.ModelId)) continue;

            if (item.Role == ChatMessageRole.User && !string.IsNullOrWhiteSpace(message.TurnId))
                _persistedBroadcastUserTurns.Add(BuildBroadcastUserTurnKey(history.SessionId, message.TurnId));

            GetOrCreate(message.ModelId).Add(message);
        }

        Notify();
    }

    public void ClearConversation(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId)) return;
        _modelConversations.Remove(modelId);
        Notify();
    }

    public void ClearConversations(IEnumerable<string> modelIds)
    {
        foreach (var id in modelIds.Where(id => !string.IsNullOrWhiteSpace(id)))
            _modelConversations.Remove(id);

        OutputSegments.Clear();
        Notify();
    }

    // ── Output Segments ───────────────────────────────────────────────────────
    public void AppendOutput(OutputSegment segment)
    {
        OutputSegments.Add(segment);
        Notify();
    }

    public void AppendOutputText(string text) =>
        AppendOutput(new OutputSegment { Type = "text", Value = text });

    public void AppendOutputSystem(string text) =>
        AppendOutput(new OutputSegment { Type = "system", Value = text });

    public void ClearOutput()
    {
        OutputSegments.Clear();
        Notify();
    }

    // ── Private ───────────────────────────────────────────────────────────────
    private List<Message> GetOrCreate(string modelId)
    {
        if (!_modelConversations.TryGetValue(modelId, out var conv))
        {
            conv = new List<Message>();
            _modelConversations[modelId] = conv;
        }

        return conv;
    }

    private bool ShouldPersistUserMessage(string? sessionId, Message message)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        if (!string.Equals(message.Role, SessionChatRole.User, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.IsNullOrWhiteSpace(message.TurnId)) return true;

        return _persistedBroadcastUserTurns.Add(BuildBroadcastUserTurnKey(sessionId, message.TurnId));
    }

    private static string BuildBroadcastUserTurnKey(string sessionId, string turnId)
        => $"{sessionId.Trim()}:{turnId.Trim()}";

    private static ChatMessageRole ParseChatRole(string role) => role switch
    {
        "system"      => ChatMessageRole.System,
        "assistant"   => ChatMessageRole.Assistant,
        "tool_call"   => ChatMessageRole.ToolCall,
        "tool_result" => ChatMessageRole.ToolResult,
        "metadata"    => ChatMessageRole.Metadata,
        _             => ChatMessageRole.User
    };

    private static SessionChatMessage ToHistory(Message message)
    {
        var meta = new Dictionary<string, string>
        {
            ["modelProfileId"]   = message.ModelId       ?? string.Empty,
            ["modelLabel"]       = message.ModelLabel     ?? string.Empty,
            ["isError"]          = message.IsError.ToString().ToLowerInvariant(),
            ["turnId"]           = message.TurnId         ?? string.Empty,
            ["requiresApproval"] = message.RequiresApproval.ToString().ToLowerInvariant(),
            ["approvalTitle"]    = message.ApprovalTitle  ?? string.Empty,
            ["approvalDetails"]  = message.ApprovalDetails ?? string.Empty,
            ["approvalStatus"]   = message.ApprovalStatus ?? string.Empty,
        };

        if (message.ToolCalls?.Count > 0)
        {
            try { meta["toolCalls"] = JsonSerializer.Serialize(message.ToolCalls, _json); }
            catch { /* non-serialisable content — skip */ }
        }

        return new SessionChatMessage
        {
            MessageId = message.MessageId,
            Role      = message.IsError ? ChatMessageRole.Assistant : ParseChatRole(message.Role),
            Timestamp = message.Timestamp.Kind == DateTimeKind.Utc
                            ? message.Timestamp
                            : message.Timestamp.ToUniversalTime(),
            Content   = message.Content,
            ModelId   = message.ModelId,
            Metadata  = meta
        };
    }

    private static Message FromHistory(SessionChatMessage m)
    {
        var isError = m.Metadata.TryGetValue("isError", out var ie) && ie == "true";
        var turnId  = m.Metadata.GetValueOrDefault("turnId");
        var modelLabel = m.Metadata.GetValueOrDefault("modelLabel");
        var requiresApproval = m.Metadata.TryGetValue("requiresApproval", out var ra) && ra == "true";

        List<ChatToolCallLog> toolCalls = new();
        if (m.Metadata.TryGetValue("toolCalls", out var tcJson) && !string.IsNullOrWhiteSpace(tcJson))
        {
            try { toolCalls = JsonSerializer.Deserialize<List<ChatToolCallLog>>(tcJson, _json) ?? new(); }
            catch { /* malformed — leave empty */ }
        }

        return new Message
        {
            MessageId        = m.MessageId,
            Role             = m.Role.ToString().ToLowerInvariant(),
            Content          = m.Content,
            ModelId          = m.ModelId,
            ModelLabel       = string.IsNullOrWhiteSpace(modelLabel) ? m.ModelId : modelLabel,
            Timestamp        = m.Timestamp.ToLocalTime(),
            IsError          = isError,
            TurnId           = string.IsNullOrWhiteSpace(turnId) ? null : turnId,
            ToolCalls        = toolCalls,
            ResponseMetadata = new Dictionary<string, object>(),
            RequiresApproval = requiresApproval,
            ApprovalTitle    = m.Metadata.GetValueOrDefault("approvalTitle"),
            ApprovalDetails  = m.Metadata.GetValueOrDefault("approvalDetails"),
            ApprovalStatus   = m.Metadata.GetValueOrDefault("approvalStatus")
        };
    }
}
