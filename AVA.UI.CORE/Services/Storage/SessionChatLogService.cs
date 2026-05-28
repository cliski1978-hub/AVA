using System.Text.Json;
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models.Chat;

namespace AVA.UI.CORE.Services.Storage;

/// <summary>
/// Persists full chat logs per session to disk.
/// File pattern: %LOCALAPPDATA%\AVA\sessions\vault_{vaultId}\project_{projectId}\session_{sessionId}\history.json
/// Root-session file pattern: %LOCALAPPDATA%\AVA\sessions\vault_{vaultId}\session_{sessionId}\history.json
/// Index file:   %LOCALAPPDATA%\AVA\sessions\_index.json
/// Active session ID stored via ISessionStorageService.
///
/// Index loaded on startup — full logs loaded on demand only.
///
/// Upgrade path (Section 12):
/// This service is the permanent home for raw session logs — files are never
/// promoted to the database. The Vault module reads from these files and decides
/// what gets extracted and persisted to long-term memory.
/// Memory storage is abstracted: SQL, vector DB, or graph depending on the
/// AVA.Memory provider configured at runtime. Only extracted artifacts
/// (curated notes, summaries, metadata) enter the Vault/Memory layer.
/// </summary>
public class SessionChatLogService : ISessionChatLogService
{
    private readonly ISessionStorageService _storage;
    private readonly IAvaIdService _ids;

    // Serializes concurrent writes to the same session file (e.g. broadcast fire-and-forget).
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private readonly string SessionsFolder;
    private readonly string IndexFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public SessionChatLogService(ISessionStorageService storage, IAvaIdService ids)
        : this(storage, ids, null)
    {
    }

    public SessionChatLogService(ISessionStorageService storage, IAvaIdService ids, string? sessionsPath = null)
    {
        _storage = storage;
        _ids     = ids;

        if (string.IsNullOrWhiteSpace(sessionsPath))
        {
            SessionsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AVA", "sessions");
        }
        else
        {
            SessionsFolder = sessionsPath;
        }

        IndexFile = Path.Combine(SessionsFolder, "_index.json");
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<ChatSessionLog> CreateSessionAsync(string title)
    {
        // 5.1: Generate ID → create empty log → save → update index → set active
        var session = new ChatSessionLog
        {
            SessionId  = _ids.NewSessionId(),
            Title      = string.IsNullOrWhiteSpace(title) ? "New Chat" : title.Trim(),
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        await SaveSessionAsync(session).ConfigureAwait(false);
        await SetActiveSessionIdAsync(session.SessionId).ConfigureAwait(false);
        return session;
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    public async Task<List<ChatSessionIndexItem>> GetSessionIndexAsync()
    {
        EnsureDirectory();
        if (!File.Exists(IndexFile)) return new List<ChatSessionIndexItem>();

        try
        {
            var json = await File.ReadAllTextAsync(IndexFile).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<ChatSessionIndexItem>>(json, JsonOptions)
                   ?? new List<ChatSessionIndexItem>();
        }
        catch
        {
            return new List<ChatSessionIndexItem>();
        }
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    public async Task<ChatSessionLog?> GetSessionAsync(string sessionId, string? vaultId = null, string? projectId = null)
    {
        var path = GetFilePath(sessionId, vaultId, projectId);
        if (!File.Exists(path))
        {
            var indexedPath = await GetIndexedPathAsync(sessionId).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(indexedPath))
                path = indexedPath;
        }

        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ChatSessionLog>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    public async Task SaveSessionAsync(ChatSessionLog session, string? vaultId = null, string? projectId = null)
    {
        var effectiveVaultId = vaultId ?? session.VaultId;
        var effectiveProjectId = projectId ?? session.ProjectId;
        var path = GetFilePath(session.SessionId, effectiveVaultId, effectiveProjectId);
        EnsureDirectory(Path.GetDirectoryName(path));

        session.VaultId = effectiveVaultId;
        session.ProjectId = effectiveProjectId;
        session.UpdatedUtc = DateTime.UtcNow;

        var json = JsonSerializer.Serialize(session, JsonOptions);
        await File.WriteAllTextAsync(path, json).ConfigureAwait(false);

        await UpdateIndexAsync(session, path).ConfigureAwait(false);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteSessionAsync(string sessionId)
    {
        var index = await GetSessionIndexAsync().ConfigureAwait(false);
        var indexed = index.FirstOrDefault(e => e.SessionId == sessionId);
        var path = indexed?.RelativePath == null
            ? GetFilePath(sessionId)
            : Path.Combine(SessionsFolder, indexed.RelativePath);

        if (File.Exists(path)) File.Delete(path);

        index.RemoveAll(e => e.SessionId == sessionId);
        await SaveIndexAsync(index).ConfigureAwait(false);
    }

    // ── Add message ───────────────────────────────────────────────────────────

    public async Task AddMessageAsync(string sessionId, ChatSessionMessage message, string? vaultId = null, string? projectId = null)
    {
        await _writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var session = await GetSessionAsync(sessionId, vaultId, projectId).ConfigureAwait(false);
            if (session == null)
            {
                session = new ChatSessionLog
                {
                    SessionId  = sessionId,
                    VaultId    = vaultId,
                    ProjectId  = projectId,
                    Title      = "Workspace Session",
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow
                };
            }

            session.Messages.Add(message);
            await SaveSessionAsync(session, vaultId, projectId).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    // ── Active session ────────────────────────────────────────────────────────

    public Task SetActiveSessionIdAsync(string sessionId)
        => _storage.SetAsync(SessionStorageKeys.ActiveChatSessionId, sessionId);

    public Task<string?> GetActiveSessionIdAsync()
        => _storage.GetAsync<string>(SessionStorageKeys.ActiveChatSessionId);

    // ── Clear all ─────────────────────────────────────────────────────────────

    public async Task ClearAllAsync()
    {
        if (Directory.Exists(SessionsFolder))
        {
            foreach (var file in Directory.GetFiles(SessionsFolder, "history.json", SearchOption.AllDirectories))
                File.Delete(file);

            if (File.Exists(IndexFile))
                File.Delete(IndexFile);
        }

        await _storage.RemoveAsync(SessionStorageKeys.ActiveChatSessionId).ConfigureAwait(false);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private string GetFilePath(string sessionId, string? vaultId = null, string? projectId = null)
    {
        if (string.IsNullOrWhiteSpace(vaultId))
            return Path.Combine(SessionsFolder, $"session_{SafeSegment(sessionId)}", "history.json");

        var folder = string.IsNullOrWhiteSpace(projectId)
            ? Path.Combine(SessionsFolder, $"vault_{SafeSegment(vaultId)}", $"session_{SafeSegment(sessionId)}")
            : Path.Combine(SessionsFolder, $"vault_{SafeSegment(vaultId)}", $"project_{SafeSegment(projectId)}", $"session_{SafeSegment(sessionId)}");

        return Path.Combine(folder, "history.json");
    }

    private async Task UpdateIndexAsync(ChatSessionLog session, string path)
    {
        var index = await GetSessionIndexAsync().ConfigureAwait(false);

        var entry = index.FirstOrDefault(e => e.SessionId == session.SessionId);
        if (entry == null)
        {
            entry = new ChatSessionIndexItem { SessionId = session.SessionId };
            index.Add(entry);
        }

        entry.Title        = session.Title;
        entry.VaultId      = session.VaultId;
        entry.ProjectId    = session.ProjectId;
        entry.RelativePath = Path.GetRelativePath(SessionsFolder, path);
        entry.CreatedUtc   = session.CreatedUtc;
        entry.UpdatedUtc   = session.UpdatedUtc;
        entry.MessageCount = session.Messages.Count;

        await SaveIndexAsync(index).ConfigureAwait(false);
    }

    private async Task<string?> GetIndexedPathAsync(string sessionId)
    {
        var index = await GetSessionIndexAsync().ConfigureAwait(false);
        var item = index.FirstOrDefault(entry =>
            entry.SessionId.Equals(sessionId, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(item?.RelativePath)
            ? null
            : Path.Combine(SessionsFolder, item.RelativePath);
    }

    private async Task SaveIndexAsync(List<ChatSessionIndexItem> index)
    {
        var json = JsonSerializer.Serialize(index, JsonOptions);
        await File.WriteAllTextAsync(IndexFile, json).ConfigureAwait(false);
    }

    private void EnsureDirectory(string? folder = null)
    {
        var target = string.IsNullOrWhiteSpace(folder) ? SessionsFolder : folder;
        if (!Directory.Exists(target))
            Directory.CreateDirectory(target);
    }

    private static string SafeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }
}
