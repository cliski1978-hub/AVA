using System.Collections.Concurrent;
using System.Text.Json;
using AVA.UI.CORE.Interfaces.Storage;

namespace AVA.UI.CORE.Services.Storage;

/// <summary>
/// In-memory session storage for lightweight UI/session state.
/// Stores selected IDs, panel widths, collapse state, and similar view settings.
/// Not for secrets, tokens, passwords, or full database entities.
///
/// In the Photino desktop host this is singleton-scoped (one session per app run).
/// The interface is forward-compatible with browser localStorage for future web hosting.
/// </summary>
public class SessionStorageService : ISessionStorageService
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task SetAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key)) return Task.CompletedTask;
        var json = JsonSerializer.Serialize(value, JsonOptions);
        _store[key] = json;
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || !_store.TryGetValue(key, out var json))
            return Task.FromResult<T?>(default);

        try
        {
            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            return Task.FromResult(value);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public Task RemoveAsync(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
            _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _store.Clear();
        return Task.CompletedTask;
    }
}
