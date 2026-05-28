namespace AVA.UI.CORE.Models.Persistence;

/// <summary>
/// Lightweight index of all available session logs.
/// Stored as: %LOCALAPPDATA%\AVA\sessions\_index.json
/// Loaded on startup — full session logs are NOT loaded until selected.
/// </summary>
public class SessionChatIndex
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<SessionChatIndexEntry> Sessions { get; set; } = new();
}

/// <summary>
/// One entry in the session index — summary only, no message content.
/// </summary>
public class SessionChatIndexEntry
{
    public string SessionId { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public string? VaultId { get; set; }
    public string? ProjectId { get; set; }
    public string? ModelProfileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}
