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
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
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

    public virtual async Task<NomiChatResponseEnvelope> SendNomiChatAsync(
        string nomiId,
        string messageText,
        CancellationToken ct = default)
    {
        var responseJson = await PostJsonAsync(
            $"{BaseUrl}/nomis/{nomiId}/chat",
            new { messageText },
            ct);

        return DeserializeChatEnvelope<NomiChatResponseEnvelope, NomiChatResponse>(responseJson);
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
        var response = await PostJsonAsync(
            $"{BaseUrl}/rooms",
            new { name, note, backchannelingEnabled, nomiUuids },
            ct);

        return Deserialize<RoomRecord>(response);
    }

    public async Task<RoomChatResponseEnvelope> SendRoomChatAsync(
        string roomId,
        string messageText,
        CancellationToken ct = default)
    {
        var responseJson = await PostJsonAsync(
            $"{BaseUrl}/rooms/{roomId}/chat",
            new { messageText },
            ct);

        return DeserializeChatEnvelope<RoomChatResponseEnvelope, RoomChatResponse>(responseJson);
    }

    public async Task<RoomReplyResponseEnvelope> RequestRoomReplyAsync(
        string roomId,
        string nomiUuid,
        CancellationToken ct = default)
    {
        var responseJson = await PostJsonAsync(
            $"{BaseUrl}/rooms/{roomId}/chat/request",
            new { nomiUuid },
            ct);

        return DeserializeChatEnvelope<RoomReplyResponseEnvelope, RoomReplyResponse>(responseJson);
    }

    public async Task<RoomRecord> UpdateRoomAsync(
        string roomId,
        string? name = null,
        string? note = null,
        bool? backchannelingEnabled = null,
        CancellationToken ct = default)
    {
        var payload = new Dictionary<string, object?>();

        if (name != null)
            payload["name"] = name;

        if (note != null)
            payload["note"] = note;

        if (backchannelingEnabled != null)
            payload["backchannelingEnabled"] = backchannelingEnabled;

        var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOpts),
            Encoding.UTF8,
            "application/json");

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
        var content = new StringContent(
            JsonSerializer.Serialize(body, JsonOpts),
            Encoding.UTF8,
            "application/json");

        var response = await _http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }

    private T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOpts)
               ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");
    }

    private static TEnvelope DeserializeChatEnvelope<TEnvelope, TResponse>(string json)
        where TEnvelope : ChatResponseEnvelope<TResponse>, new()
    {
        var typedResponse = JsonSerializer.Deserialize<TResponse>(json, JsonOpts)
                            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(TResponse).Name}.");

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement.Clone();

        var envelope = new TEnvelope
        {
            Response = typedResponse,
            RawJson = json,
            RawRoot = root,
            Metadata = ExtractJsonElement(root, "metadata"),
            ToolCalls = ExtractJsonElement(root, "toolCalls"),
            AdditionalData = ExtractAdditionalData(root)
        };

        ApplyNomiEmbeddedPayload(envelope, typedResponse);

        return envelope;
    }

    private static void ApplyNomiEmbeddedPayload<TResponse>(
        ChatResponseEnvelope<TResponse> envelope,
        TResponse response)
    {
        var replyMessage = GetReplyMessage(response);

        if (replyMessage == null || string.IsNullOrWhiteSpace(replyMessage.Text))
            return;

        var embeddedPayload = ExtractNomiEmbeddedPayload(replyMessage.Text);

        if (embeddedPayload == null)
        {
            envelope.Text = replyMessage.Text;
            return;
        }

        if (embeddedPayload.Metadata.HasValue)
            envelope.Metadata = embeddedPayload.Metadata.Value;

        if (embeddedPayload.ToolCalls.HasValue)
            envelope.ToolCalls = embeddedPayload.ToolCalls.Value;

        envelope.Text = embeddedPayload.CleanedText;
        envelope.CleanedReplyText = embeddedPayload.CleanedText;

        replyMessage.Text = embeddedPayload.CleanedText;
    }

    private static ChatMessage? GetReplyMessage<TResponse>(TResponse response)
    {
        return response switch
        {
            NomiChatResponse nomiChatResponse => nomiChatResponse.ReplyMessage,
            RoomReplyResponse roomReplyResponse => roomReplyResponse.ReplyMessage,
            _ => null
        };
    }

    private static NomiEmbeddedPayloadExtraction? ExtractNomiEmbeddedPayload(string text)
    {
        var extractedJson = ExtractFirstJsonObjectContainingProperty(text, "actionRequests");

        if (string.IsNullOrWhiteSpace(extractedJson.Json))
            return null;

        using var document = JsonDocument.Parse(extractedJson.Json);
        var root = document.RootElement.Clone();

        if (root.ValueKind != JsonValueKind.Object)
            return null;

        if (!root.TryGetProperty("actionRequests", out var actionRequests))
            return null;

        if (actionRequests.ValueKind != JsonValueKind.Array)
            return null;

        JsonElement? metadata = null;

        if (root.TryGetProperty("metadata", out var metadataElement))
            metadata = metadataElement.Clone();

        return new NomiEmbeddedPayloadExtraction
        {
            Metadata = metadata,
            ToolCalls = actionRequests.Clone(),
            CleanedText = RemoveRange(text, extractedJson.StartIndex, extractedJson.Length).Trim()
        };
    }

    private static ExtractedJsonRange ExtractFirstJsonObjectContainingProperty(string text, string propertyName)
    {
        var propertyToken = $"\"{propertyName}\"";
        var propertyIndex = text.IndexOf(propertyToken, StringComparison.OrdinalIgnoreCase);

        if (propertyIndex < 0)
            return ExtractedJsonRange.Empty;

        for (var startIndex = propertyIndex; startIndex >= 0; startIndex--)
        {
            if (text[startIndex] != '{')
                continue;

            var candidateJson = ExtractBalancedJsonObject(text, startIndex);

            if (string.IsNullOrWhiteSpace(candidateJson))
                continue;

            if (!JsonObjectContainsRootProperty(candidateJson, propertyName))
                continue;

            return new ExtractedJsonRange
            {
                Json = candidateJson,
                StartIndex = startIndex,
                Length = candidateJson.Length
            };
        }

        return ExtractedJsonRange.Empty;
    }

    private static string? ExtractBalancedJsonObject(string text, int startIndex)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = startIndex; i < text.Length; i++)
        {
            var current = text[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                escaped = true;
                continue;
            }

            if (current == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (current == '{')
                depth++;

            if (current == '}')
                depth--;

            if (depth == 0)
                return text.Substring(startIndex, i - startIndex + 1);
        }

        return null;
    }

    private static bool JsonObjectContainsRootProperty(string json, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty(propertyName, out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string RemoveRange(string text, int startIndex, int length)
    {
        if (startIndex < 0 || length <= 0 || startIndex >= text.Length)
            return text;

        if (startIndex + length > text.Length)
            length = text.Length - startIndex;

        return text.Remove(startIndex, length);
    }

    private static JsonElement? ExtractJsonElement(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(propertyName, out var value))
        {
            return value.Clone();
        }

        return null;
    }

    private static Dictionary<string, JsonElement> ExtractAdditionalData(JsonElement root)
    {
        var additionalData = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (root.ValueKind != JsonValueKind.Object)
            return additionalData;

        foreach (var property in root.EnumerateObject())
        {
            if (IsKnownChatResponseProperty(property.Name))
                continue;

            additionalData[property.Name] = property.Value.Clone();
        }

        return additionalData;
    }

    private static bool IsKnownChatResponseProperty(string propertyName)
    {
        return propertyName.Equals("sentMessage", StringComparison.OrdinalIgnoreCase)
               || propertyName.Equals("replyMessage", StringComparison.OrdinalIgnoreCase)
               || propertyName.Equals("metadata", StringComparison.OrdinalIgnoreCase)
               || propertyName.Equals("toolCalls", StringComparison.OrdinalIgnoreCase);
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

        [JsonPropertyName("metadata")]
        public JsonElement? Metadata { get; set; }

        [JsonPropertyName("toolCalls")]
        public JsonElement? ToolCalls { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }
    }

    public abstract class ChatResponseEnvelope<TResponse>
    {
        [JsonPropertyName("response")]
        public TResponse Response { get; set; } = default!;

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("rawJson")]
        public string RawJson { get; set; } = string.Empty;

        [JsonPropertyName("rawRoot")]
        public JsonElement RawRoot { get; set; }

        [JsonPropertyName("metadata")]
        public JsonElement? Metadata { get; set; }

        [JsonPropertyName("toolCalls")]
        public JsonElement? ToolCalls { get; set; }

        [JsonPropertyName("cleanedReplyText")]
        public string? CleanedReplyText { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalData { get; set; } = new();
    }

    private class NomiListResponse
    {
        [JsonPropertyName("nomis")] public List<NomiRecord>? Nomis { get; set; }
    }

    private class RoomListResponse
    {
        [JsonPropertyName("rooms")] public List<RoomRecord>? Rooms { get; set; }
    }

    private class NomiEmbeddedPayloadExtraction
    {
        public JsonElement? Metadata { get; set; }
        public JsonElement? ToolCalls { get; set; }
        public string? CleanedText { get; set; }
    }

    private struct ExtractedJsonRange
    {
        public static ExtractedJsonRange Empty
        {
            get
            {
                return new ExtractedJsonRange
                {
                    Json = null,
                    StartIndex = -1,
                    Length = 0
                };
            }
        }

        public string? Json { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
    }

    public class NomiChatResponseEnvelope : ChatResponseEnvelope<NomiChatResponse>
    {
        [JsonIgnore]
        public ChatMessage? SentMessage
        {
            get { return Response?.SentMessage; }
        }

        [JsonIgnore]
        public ChatMessage? ReplyMessage
        {
            get { return Response?.ReplyMessage; }
        }
    }

    public class RoomChatResponseEnvelope : ChatResponseEnvelope<RoomChatResponse>
    {
        [JsonIgnore]
        public ChatMessage? SentMessage
        {
            get { return Response?.SentMessage; }
        }
    }

    public class RoomReplyResponseEnvelope : ChatResponseEnvelope<RoomReplyResponse>
    {
        [JsonIgnore]
        public ChatMessage? ReplyMessage
        {
            get { return Response?.ReplyMessage; }
        }
    }
}