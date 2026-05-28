namespace AVA.UI.CORE.Models.Persistence;

/// <summary>
/// Full recoverable chat log for a single session.
/// Stored as: %LOCALAPPDATA%\AVA\sessions\{sessionId}.json
/// Loaded on demand — not hydrated on startup.
/// </summary>
public class SessionChatLog
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string? VaultId { get; set; }
    public string? ProjectId { get; set; }
    public string? ModelProfileId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public List<SessionChatMessage> Messages { get; set; } = new();
}
