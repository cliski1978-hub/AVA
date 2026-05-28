using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AVA.Nomi.Bridge;

public class NomiApiClient
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.nomi.ai/v1";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NomiApiClient(string apiKey)
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3)
        };
        _http.DefaultRequestHeaders.Add("Authorization", apiKey);
        _http.DefaultRequestHeaders.Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ── Nomis ──────────────────────────────────────────────────────────────────

    public async Task<List<NomiRecord>> GetNomisAsync(CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync($"{BaseUrl}/nomis", ct);
        var doc = JsonSerializer.Deserialize<NomiListResponse>(json, JsonOpts);
        return doc?.Nomis ?? new List<NomiRecord>();
    }

    public async Task<NomiRecord> GetNomiAsync(string nomiId, CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync($"{BaseUrl}/nomis/{nomiId}", ct);
        return JsonSerializer.Deserialize<NomiRecord>(json, JsonOpts)
               ?? throw new InvalidOperationException($"Nomi {nomiId} not found.");
    }

    public async Task<byte[]> GetNomiAvatarAsync(string nomiId, CancellationToken ct = default)
    {
        return await _http.GetByteArrayAsync($"{BaseUrl}/nomis/{nomiId}/avatar", ct);
    }

    public virtual async Task<NomiChatResponse> SendNomiChatAsync(
        string nomiId,
        string messageText,
        CancellationToken ct = default)
    {
       
        var response = await PostJsonAsync($"{BaseUrl}/nomis/{nomiId}/chat",
            new { messageText }, ct);
        return Deserialize<NomiChatResponse>(response);
    }

    // ── Rooms ──────────────────────────────────────────────────────────────────

    public async Task<List<RoomRecord>> GetRoomsAsync(CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync($"{BaseUrl}/rooms", ct);
        var doc = JsonSerializer.Deserialize<RoomListResponse>(json, JsonOpts);
        return doc?.Rooms ?? new List<RoomRecord>();
    }

    public async Task<RoomRecord> GetRoomAsync(string roomId, CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync($"{BaseUrl}/rooms/{roomId}", ct);
        return JsonSerializer.Deserialize<RoomRecord>(json, JsonOpts)
               ?? throw new InvalidOperationException($"Room {roomId} not found.");
    }

    public async Task<RoomRecord> CreateRoomAsync(
        string name,
        string note,
        bool backchannelingEnabled,
        List<string> nomiUuids,
        CancellationToken ct = default)
    {
        var response = await PostJsonAsync($"{BaseUrl}/rooms",
            new { name, note, backchannelingEnabled, nomiUuids }, ct);
        return Deserialize<RoomRecord>(response);
    }

    public async Task<RoomChatResponse> SendRoomChatAsync(
        string roomId,
        string messageText,
        CancellationToken ct = default)
    {
        var response = await PostJsonAsync($"{BaseUrl}/rooms/{roomId}/chat",
            new { messageText }, ct);
        return Deserialize<RoomChatResponse>(response);
    }

    public async Task<RoomReplyResponse> RequestRoomReplyAsync(
        string roomId,
        string nomiUuid,
        CancellationToken ct = default)
    {
        var response = await PostJsonAsync($"{BaseUrl}/rooms/{roomId}/chat/request",
            new { nomiUuid }, ct);
        return Deserialize<RoomReplyResponse>(response);
    }

    public async Task<RoomRecord> UpdateRoomAsync(
        string roomId,
        string? name = null,
        string? note = null,
        bool? backchannelingEnabled = null,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>();
        if (name != null) payload["name"] = name;
        if (note != null) payload["note"] = note;
        if (backchannelingEnabled != null) payload["backchannelingEnabled"] = backchannelingEnabled;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"{BaseUrl}/rooms/{roomId}", content, ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return Deserialize<RoomRecord>(json);
    }

    public async Task DeleteRoomAsync(string roomId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"{BaseUrl}/rooms/{roomId}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private async Task<string> PostJsonAsync(string url, object body, CancellationToken ct)
    {
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOpts)
               ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");
    }

    // ── Models ─────────────────────────────────────────────────────────────────

    public class NomiRecord
    {
        [JsonPropertyName("uuid")] public string Uuid { get; set; } = default!;
        [JsonPropertyName("name")] public string Name { get; set; } = default!;
        [JsonPropertyName("gender")] public string Gender { get; set; } = default!;
        [JsonPropertyName("created")] public string Created { get; set; } = default!;
        [JsonPropertyName("relationshipType")] public string RelationshipType { get; set; } = default!;
    }

    public class RoomRecord
    {
        [JsonPropertyName("uuid")] public string Uuid { get; set; } = default!;
        [JsonPropertyName("name")] public string Name { get; set; } = default!;
        [JsonPropertyName("created")] public string Created { get; set; } = default!;
        [JsonPropertyName("updated")] public string Updated { get; set; } = default!;
        [JsonPropertyName("status")] public string Status { get; set; } = default!;
        [JsonPropertyName("backchannelingEnabled")] public bool BackchannelingEnabled { get; set; }
        [JsonPropertyName("nomis")] public List<NomiRecord> Nomis { get; set; } = new();
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    public class NomiChatResponse
    {
        [JsonPropertyName("sentMessage")] public ChatMessage? SentMessage { get; set; }
        [JsonPropertyName("replyMessage")] public ChatMessage? ReplyMessage { get; set; }
    }

    public class RoomChatResponse
    {
        [JsonPropertyName("sentMessage")] public ChatMessage? SentMessage { get; set; }
    }

    public class RoomReplyResponse
    {
        [JsonPropertyName("replyMessage")] public ChatMessage? ReplyMessage { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("uuid")] public string? Uuid { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("sent")] public string? Sent { get; set; }
    }

    private class NomiListResponse
    {
        [JsonPropertyName("nomis")] public List<NomiRecord>? Nomis { get; set; }
    }

    private class RoomListResponse
    {
        [JsonPropertyName("rooms")] public List<RoomRecord>? Rooms { get; set; }
    }
}