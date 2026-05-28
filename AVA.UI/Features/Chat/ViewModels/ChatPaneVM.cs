using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.ChatContext.Services;
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models.Chat;

namespace AVA.UI.Features.Chat.ViewModels;

/// <summary>
/// ViewModel for ChatPane — owns the full chat session lifecycle.
/// Responsible for: active session, session index, loading selected sessions,
/// adding messages, capturing assistant responses, tool calls, and persistence.
///
/// Architecture boundary:
/// ChatPaneVM → ISessionChatHistoryService → ISessionChatLogService → file system
///
/// The Vault module decides what gets extracted from session logs and promoted
/// to long-term memory. Memory storage is abstracted — it may be SQL, vector DB,
/// or graph depending on the memory provider configured in AVA.Memory.
/// The full chat log is never written to the DB — only extracted artifacts are.
/// </summary>
public class ChatPaneVM : IDisposable
{
    private readonly ISessionChatHistoryService _chatHistory;
    private readonly IAvaIdService _ids;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── State ─────────────────────────────────────────────────────────────────
    public List<ChatSessionIndexItem> SessionIndex { get; private set; } = new();
    public SessionChatHistory? ActiveSession { get; private set; }
    public string? ActiveSessionId => ActiveSession?.SessionId;
    public bool IsLoading { get; private set; }

    public ChatPaneVM(ISessionChatHistoryService chatHistory, IAvaIdService ids)
    {
        _chatHistory = chatHistory;
        _ids         = ids;
    }

    // ── Load index only on startup — never auto-load full sessions ─────────────
    public async Task InitializeAsync()
    {
        SessionIndex = await _chatHistory.GetIndexAsync();

        var lastId = await _chatHistory.GetActiveSessionIdAsync();
        if (!string.IsNullOrWhiteSpace(lastId))
            await LoadSessionAsync(lastId);

        Notify();
    }

    // ── Create new session ─────────────────────────────────────────────────────
    public async Task CreateNewSessionAsync()
    {
        IsLoading = true;
        Notify();

        try
        {
            ActiveSession = await _chatHistory.CreateSessionAsync("New Chat");
            SessionIndex  = await _chatHistory.GetIndexAsync();
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    // ── Load session on user selection only ────────────────────────────────────
    public async Task LoadSessionAsync(string sessionId)
    {
        IsLoading = true;
        Notify();

        try
        {
            var history = await _chatHistory.LoadHistoryAsync(sessionId);
            if (history != null)
            {
                ActiveSession = history;
                await _chatHistory.SetActiveSessionAsync(sessionId);
            }
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    // ── Add user message ───────────────────────────────────────────────────────
    public async Task AddUserMessageAsync(string content)
    {
        if (ActiveSession == null || string.IsNullOrWhiteSpace(content)) return;

        var message = new SessionChatMessage
        {
            MessageId = _ids.NewMessageId(),
            Role      = ChatMessageRole.User,
            Content   = content.Trim(),
            Timestamp = DateTime.UtcNow
        };

        await _chatHistory.AddMessageAsync(ActiveSession.SessionId, message);
        ActiveSession.Messages.Add(message);
        Notify();
    }

    // ── Add assistant message ──────────────────────────────────────────────────
    public async Task AddAssistantMessageAsync(SessionChatMessage message)
    {
        if (ActiveSession == null) return;

        if (string.IsNullOrWhiteSpace(message.MessageId))
            message.MessageId = _ids.NewMessageId();

        message.Timestamp = DateTime.UtcNow;
        message.Role      = ChatMessageRole.Assistant;

        await _chatHistory.AddMessageAsync(ActiveSession.SessionId, message);
        ActiveSession.Messages.Add(message);
        Notify();
    }

    // ── Add tool call to an existing message ───────────────────────────────────
    public async Task AddToolCallToMessageAsync(string messageId, ChatToolCallLog toolCall)
    {
        if (ActiveSession == null) return;

        var target = ActiveSession.Messages.FirstOrDefault(m => m.MessageId == messageId);
        if (target == null) return;

        if (string.IsNullOrWhiteSpace(toolCall.ToolCallId))
            toolCall.ToolCallId = _ids.NewToolCallId();

        // Append the tool call into the message's toolCalls metadata key.
        List<ChatToolCallLog> existing = new();
        if (target.Metadata.TryGetValue("toolCalls", out var tcJson) &&
            !string.IsNullOrWhiteSpace(tcJson))
        {
            try
            {
                existing = System.Text.Json.JsonSerializer.Deserialize<List<ChatToolCallLog>>(tcJson)
                           ?? new List<ChatToolCallLog>();
            }
            catch { /* leave empty */ }
        }

        existing.Add(toolCall);

        try
        {
            target.Metadata["toolCalls"] = System.Text.Json.JsonSerializer.Serialize(existing);
        }
        catch { /* skip if non-serialisable */ }

        await _chatHistory.SaveHistoryAsync(ActiveSession);
        Notify();
    }

    public void Dispose() { }
}
