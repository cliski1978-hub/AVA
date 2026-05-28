using System.Text.Json;
using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models.Chat;
using ChatMessageRole = AVA.UI.CORE.ChatContext.Models.ChatMessageRole;

namespace AVA.UI.CORE.ChatContext.Services
{
    /// <summary>
    /// Adapts ISessionChatLogService into the ChatContext model surface.
    /// All file I/O remains in SessionChatLogService — this layer only translates
    /// between SessionChatMessage ↔ ChatSessionMessage and
    /// SessionChatHistory ↔ ChatSessionLog.
    /// </summary>
    public class SessionChatHistoryService : ISessionChatHistoryService
    {
        private readonly ISessionChatLogService _log;
        private readonly IAvaIdService _ids;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SessionChatHistoryService(ISessionChatLogService log, IAvaIdService ids)
        {
            _log  = log;
            _ids  = ids;
        }

        // ── Create ────────────────────────────────────────────────────────────

        public async Task<SessionChatHistory> CreateSessionAsync(
            string title,
            string? vaultId   = null,
            string? projectId = null,
            CancellationToken ct = default)
        {
            var log = await _log.CreateSessionAsync(title).ConfigureAwait(false);
            log.VaultId   = vaultId;
            log.ProjectId = projectId;
            return ToHistory(log);
        }

        // ── Index ─────────────────────────────────────────────────────────────

        public Task<List<ChatSessionIndexItem>> GetIndexAsync(CancellationToken ct = default)
            => _log.GetSessionIndexAsync();

        // ── Load ──────────────────────────────────────────────────────────────

        public async Task<SessionChatHistory?> LoadHistoryAsync(
            string sessionId,
            string? vaultId   = null,
            string? projectId = null,
            CancellationToken ct = default)
        {
            var log = await _log.GetSessionAsync(sessionId, vaultId, projectId).ConfigureAwait(false);
            return log == null ? null : ToHistory(log);
        }

        // ── Add message ───────────────────────────────────────────────────────

        public Task AddMessageAsync(
            string sessionId,
            SessionChatMessage message,
            string? vaultId   = null,
            string? projectId = null,
            CancellationToken ct = default)
            => _log.AddMessageAsync(sessionId, ToPersisted(message), vaultId, projectId);

        // ── Save full history ─────────────────────────────────────────────────

        public Task SaveHistoryAsync(SessionChatHistory history, CancellationToken ct = default)
            => _log.SaveSessionAsync(ToLog(history), history.VaultId, history.ProjectId);

        // ── Delete ────────────────────────────────────────────────────────────

        public Task DeleteSessionAsync(string sessionId, CancellationToken ct = default)
            => _log.DeleteSessionAsync(sessionId);

        // ── Active session ────────────────────────────────────────────────────

        public Task SetActiveSessionAsync(string sessionId, CancellationToken ct = default)
            => _log.SetActiveSessionIdAsync(sessionId);

        public Task<string?> GetActiveSessionIdAsync(CancellationToken ct = default)
            => _log.GetActiveSessionIdAsync();

        // ── Model adapters ────────────────────────────────────────────────────

        private static ChatSessionMessage ToPersisted(SessionChatMessage m)
        {
            var msg = new ChatSessionMessage
            {
                MessageId      = m.MessageId,
                Role           = RoleToString(m.Role),
                Content        = m.Content,
                ModelId        = m.ModelId ?? string.Empty,
                CreatedUtc     = m.Timestamp.Kind == DateTimeKind.Utc
                                     ? m.Timestamp
                                     : m.Timestamp.ToUniversalTime(),
                ModelProfileId = m.Metadata.GetValueOrDefault("modelProfileId") ?? string.Empty,
                ModelLabel     = m.Metadata.GetValueOrDefault("modelLabel")     ?? string.Empty,
                IsError        = m.Metadata.TryGetValue("isError",           out var ie)  && ie  == "true",
                RequiresApproval = m.Metadata.TryGetValue("requiresApproval", out var ra) && ra  == "true",
                TurnId         = NullIfEmpty(m.Metadata.GetValueOrDefault("turnId")),
                ApprovalTitle  = NullIfEmpty(m.Metadata.GetValueOrDefault("approvalTitle")),
                ApprovalDetails = NullIfEmpty(m.Metadata.GetValueOrDefault("approvalDetails")),
                ApprovalStatus = NullIfEmpty(m.Metadata.GetValueOrDefault("approvalStatus")),
            };

            if (m.Metadata.TryGetValue("toolCalls", out var tcJson) &&
                !string.IsNullOrWhiteSpace(tcJson))
            {
                try
                {
                    msg.ToolCalls = JsonSerializer.Deserialize<List<ChatToolCallLog>>(tcJson, _json)
                                    ?? new List<ChatToolCallLog>();
                }
                catch { /* malformed JSON — leave empty */ }
            }

            return msg;
        }

        private static SessionChatMessage FromPersisted(ChatSessionMessage m)
        {
            var meta = new Dictionary<string, string>
            {
                ["modelProfileId"]   = m.ModelProfileId,
                ["modelLabel"]       = m.ModelLabel,
                ["isError"]          = m.IsError.ToString().ToLowerInvariant(),
                ["turnId"]           = m.TurnId           ?? string.Empty,
                ["requiresApproval"] = m.RequiresApproval.ToString().ToLowerInvariant(),
                ["approvalTitle"]    = m.ApprovalTitle    ?? string.Empty,
                ["approvalDetails"]  = m.ApprovalDetails  ?? string.Empty,
                ["approvalStatus"]   = m.ApprovalStatus   ?? string.Empty,
            };

            if (m.ToolCalls?.Count > 0)
            {
                try { meta["toolCalls"] = JsonSerializer.Serialize(m.ToolCalls, _json); }
                catch { /* skip non-serialisable entries */ }
            }

            return new SessionChatMessage
            {
                MessageId = m.MessageId,
                Role      = ParseRole(m.Role),
                Timestamp = m.CreatedUtc.Kind == DateTimeKind.Utc
                                ? m.CreatedUtc
                                : DateTime.SpecifyKind(m.CreatedUtc, DateTimeKind.Utc),
                Content   = m.Content,
                ModelId   = string.IsNullOrWhiteSpace(m.ModelId) ? null : m.ModelId,
                Metadata  = meta
            };
        }

        private static SessionChatHistory ToHistory(ChatSessionLog log) => new()
        {
            SessionId = log.SessionId,
            VaultId   = log.VaultId,
            ProjectId = log.ProjectId,
            Title     = log.Title,
            CreatedAt = log.CreatedUtc,
            UpdatedAt = log.UpdatedUtc,
            Messages  = log.Messages.Select(FromPersisted).ToList()
        };

        private static ChatSessionLog ToLog(SessionChatHistory h) => new()
        {
            SessionId  = h.SessionId,
            VaultId    = h.VaultId,
            ProjectId  = h.ProjectId,
            Title      = h.Title,
            CreatedUtc = h.CreatedAt,
            UpdatedUtc = h.UpdatedAt,
            Messages   = h.Messages.Select(ToPersisted).ToList()
        };

        private static string RoleToString(ChatMessageRole role) => role switch
        {
            ChatMessageRole.System     => "system",
            ChatMessageRole.User       => "user",
            ChatMessageRole.Assistant  => "assistant",
            ChatMessageRole.ToolCall   => "tool_call",
            ChatMessageRole.ToolResult => "tool_result",
            ChatMessageRole.Metadata   => "metadata",
            _                          => "user"
        };

        private static ChatMessageRole ParseRole(string role) => role switch
        {
            "system"      => ChatMessageRole.System,
            "assistant"   => ChatMessageRole.Assistant,
            "error"       => ChatMessageRole.Assistant,
            "tool_call"   => ChatMessageRole.ToolCall,
            "tool_result" => ChatMessageRole.ToolResult,
            "metadata"    => ChatMessageRole.Metadata,
            _             => ChatMessageRole.User
        };

        private static string? NullIfEmpty(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
