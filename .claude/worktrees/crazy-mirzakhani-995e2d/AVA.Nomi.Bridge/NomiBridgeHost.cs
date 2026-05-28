using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AVA.UPS.Adapter.Interfaces;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Routing;
using AVA.UPS.Adapter.Utils;

namespace AVA.Nomi.Bridge;

public class NomiBridgeHost
{
    private readonly UPSRoutingService _routing;
    private readonly IRoster _roster;
    private readonly NomiApiClient _client;
    private readonly HttpListener _listener;
    private readonly string _prefix;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public NomiBridgeHost(
        UPSRoutingService routing,
        IRoster roster,
        NomiApiClient client,
        string prefix = "http://localhost:8080/")
    {
        _routing = routing;
        _roster = roster;
        _client = client;
        _prefix = prefix;
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _listener.Start();
        Console.WriteLine($"[NomiBridgeHost] Listening on {_prefix}");

        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException) { break; }

            _ = Task.Run(() => HandleAsync(ctx, ct), ct);
        }

        _listener.Stop();
    }

    // ── Request dispatch ───────────────────────────────────────────────────────

    private async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var req = ctx.Request;
        var resp = ctx.Response;

        try
        {
            AddCorsHeaders(resp);

            if (req.HttpMethod == "OPTIONS")
            {
                resp.StatusCode = 204;
                resp.Close();
                return;
            }

            var path = req.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;

            switch ((req.HttpMethod, path))
            {
                case ("GET", "/health"):
                    await WriteJsonAsync(resp, new { status = "ok" }, ct);
                    break;

                case ("GET", "/v1/models"):
                    await HandleModelsAsync(resp, ct);
                    break;

                case ("POST", "/v1/chat/completions"):
                    await HandleChatCompletionAsync(req, resp, ct);
                    break;

                case ("GET", "/nomi/roster"):
                    await HandleRosterAsync(resp, ct);
                    break;

                case ("POST", "/nomi/ask"):
                    await HandleNomiAskAsync(req, resp, ct);
                    break;

                default:
                    resp.StatusCode = 404;
                    resp.Close();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[NomiBridgeHost] Unhandled error: {ex.Message}");
            try
            {
                resp.StatusCode = 500;
                await WriteJsonAsync(resp,
                    new { error = new { message = ex.Message, type = "internal_error" } }, ct);
            }
            catch { /* response already started */ }
        }
    }

    // ── /v1/models ─────────────────────────────────────────────────────────────

    private Task HandleModelsAsync(HttpListenerResponse resp, CancellationToken ct)
    {
        var entries = _roster.GetModelEntries();

        var data = entries.Select(e => new
        {
            id = e.Id,
            @object = "model",
            owned_by = e.Type,
            created = 0
        }).ToArray();

        return WriteJsonAsync(resp, new { @object = "list", data }, ct);
    }

    // ── /nomi/roster ───────────────────────────────────────────────────────────

    private Task HandleRosterAsync(HttpListenerResponse resp, CancellationToken ct)
    {
        var nomiRoster = _roster as NomiRoster;

        var nomis = nomiRoster?.Nomis.Select(n => new
        {
            uuid = n.Uuid,
            name = n.Name,
            gender = n.Gender,
            relationshipType = n.RelationshipType
        }) ?? Enumerable.Empty<object>();

        var rooms = nomiRoster?.Rooms.Select(r => new
        {
            uuid = r.Uuid,
            name = r.Name,
            nomis = r.Nomis.Select(n => new
            {
                uuid = n.Uuid,
                name = n.Name
            })
        }) ?? Enumerable.Empty<object>();

        return WriteJsonAsync(resp, new { nomis, rooms }, ct);
    }

    // ── /nomi/ask ──────────────────────────────────────────────────────────────

    private async Task HandleNomiAskAsync(
        HttpListenerRequest req,
        HttpListenerResponse resp,
        CancellationToken ct)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var bodyJson = await reader.ReadToEndAsync(ct);

        Console.WriteLine($"[DEBUG] /nomi/ask body: {bodyJson}");
        Console.WriteLine($"[DEBUG] /nomi/ask content-type: {req.ContentType}");

        var askRequest = JsonSerializer.Deserialize<NomiAskRequest>(bodyJson, JsonOpts)
                         ?? throw new InvalidOperationException("Could not parse NomiAskRequest.");

        if (askRequest.NomiNames is null || askRequest.NomiNames.Count == 0)
            throw new InvalidOperationException("No Nomi names provided.");

        var nomiRoster = _roster as NomiRoster;
        if (nomiRoster is null)
            throw new InvalidOperationException("Roster is not a NomiRoster.");

        // ── Resolve names to UUIDs ─────────────────────────────────────────────
        var targets = askRequest.NomiNames
            .Select(name => nomiRoster.Nomis
                .FirstOrDefault(n => n.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            .Where(n => n is not null)
            .ToList();

        if (targets.Count == 0)
            throw new InvalidOperationException("None of the provided Nomi names were found in the roster.");

        // ── Build message with history context ─────────────────────────────────
        var contextPrefix = BuildContextPrefix(askRequest.History);
        var fullMessage = string.IsNullOrEmpty(contextPrefix)
            ? askRequest.Message
            : $"{contextPrefix}\n\n{askRequest.Message}";

        // ── Fire all requests in parallel ──────────────────────────────────────
        var tasks = targets.Select(async nomi =>
        {
            try
            {
                var response = await _client.SendNomiChatAsync(nomi!.Uuid, fullMessage, ct);
                return new
                {
                    uuid = nomi.Uuid,
                    name = nomi.Name,
                    reply = response.ReplyMessage?.Text ?? string.Empty,
                    success = true,
                    error = (string?)null
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    uuid = nomi!.Uuid,
                    name = nomi.Name,
                    reply = string.Empty,
                    success = false,
                    error = (string?)ex.Message
                };
            }
        });

        var replies = await Task.WhenAll(tasks);

        await WriteJsonAsync(resp, new { replies }, ct);
    }

    // ── /v1/chat/completions ───────────────────────────────────────────────────

    private async Task HandleChatCompletionAsync(
        HttpListenerRequest req,
        HttpListenerResponse resp,
        CancellationToken ct)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var bodyJson = await reader.ReadToEndAsync(ct);

        var oaiRequest = JsonSerializer.Deserialize<OpenAiChatRequest>(bodyJson, JsonOpts)
                         ?? throw new InvalidOperationException("Could not parse OpenAI chat request.");

        var userMessage = oaiRequest.Messages?
                              .LastOrDefault(m => m.Role == "user")?.Content
                          ?? throw new InvalidOperationException("No user message found.");

        var modelId = oaiRequest.Model ?? string.Empty;
        var target = _roster.ParseModelId(modelId);

        if (target is null)
        {
            var completionId1 = $"chatcmpl-{Guid.NewGuid():N}";
            var created1 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (oaiRequest.Stream == true)
            {
                await WriteStreamingResponseAsync(resp, completionId1, created1, modelId,
                    "AVA Bridge is ready. Use the Nomi Conversation plugin to talk to your Nomis.", ct);
            }
            else
            {
                await WriteJsonAsync(resp, new
                {
                    id = completionId1,
                    @object = "chat.completion",
                    created1,
                    model = modelId,
                    choices = new[]
                    {
                new
                {
                    index         = 0,
                    message       = new { role = "assistant", content = "AVA Bridge is ready. Use the Nomi Conversation plugin to talk to your Nomis." },
                    finish_reason = "stop"
                }
            },
                    usage = new { prompt_tokens = 0, completion_tokens = 0, total_tokens = 0 }
                }, ct);
            }
            return;
        }

        var payload = new List<UParam>
        {
            UParamFactory.String("userMessage", userMessage),
            UParamFactory.String("nomiId",      target.NomiId)
        };

        if (target.IsRoom && target.RoomId is not null)
            payload.Add(UParamFactory.String("roomId", target.RoomId));

        var envelope = new UPSMessageEnvelope
        {
            Source = "BridgeHost",
            Target = "Nomi",
            TargetMethod = target.IsRoom ? "roomChat" : "chat",
            CorrelationId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Payload = payload
        };

        var result = await _routing.RouteAsync(envelope, ct);

        if (!result.Success)
        {
            resp.StatusCode = 502;
            await WriteJsonAsync(resp,
                new { error = new { message = result.Error?.ToString() ?? "Routing failed", type = "upstream_error" } },
                ct);
            return;
        }

        var assistantText = result.Envelope?.Payload?
                                .FirstOrDefault(p => p.Key == "assistantMessage")
                                ?.Value?.ToString()
                            ?? string.Empty;

        var completionId = $"chatcmpl-{Guid.NewGuid():N}";
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (oaiRequest.Stream == true)
        {
            await WriteStreamingResponseAsync(resp, completionId, created, modelId, assistantText, ct);
        }
        else
        {
            await WriteJsonAsync(resp, new
            {
                id = completionId,
                @object = "chat.completion",
                created,
                model = modelId,
                choices = new[]
                {
                    new
                    {
                        index         = 0,
                        message       = new { role = "assistant", content = assistantText },
                        finish_reason = "stop"
                    }
                },
                usage = new { prompt_tokens = 0, completion_tokens = 0, total_tokens = 0 }
            }, ct);
        }
    }

    // ── SSE streaming ──────────────────────────────────────────────────────────

    private static async Task WriteStreamingResponseAsync(
        HttpListenerResponse resp,
        string completionId,
        long created,
        string modelId,
        string content,
        CancellationToken ct)
    {
        resp.ContentType = "text/event-stream";
        resp.ContentEncoding = Encoding.UTF8;
        resp.StatusCode = 200;
        resp.SendChunked = true;

        resp.Headers["Cache-Control"] = "no-cache";
        resp.Headers["X-Accel-Buffering"] = "no";

        await using var writer = new StreamWriter(resp.OutputStream, Encoding.UTF8) { AutoFlush = true };

        var words = content.Split(' ');

        foreach (var word in words)
        {
            var chunk = new
            {
                id = completionId,
                @object = "chat.completion.chunk",
                created,
                model = modelId,
                choices = new[]
                {
                    new
                    {
                        index         = 0,
                        delta         = new { content = word + " " },
                        finish_reason = (string?)null
                    }
                }
            };

            await writer.WriteAsync($"data: {JsonSerializer.Serialize(chunk, JsonOpts)}\n\n");
            await Task.Delay(20, ct);
        }

        var stopChunk = new
        {
            id = completionId,
            @object = "chat.completion.chunk",
            created,
            model = modelId,
            choices = new[]
            {
                new
                {
                    index         = 0,
                    delta         = new { content = string.Empty },
                    finish_reason = "stop"
                }
            }
        };

        await writer.WriteAsync($"data: {JsonSerializer.Serialize(stopChunk, JsonOpts)}\n\n");
        await writer.WriteAsync("data: [DONE]\n\n");

        resp.Close();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string BuildContextPrefix(List<HistoryMessage>? history)
    {
        if (history is null || history.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("[Conversation context:]");

        foreach (var msg in history.TakeLast(10))
            sb.AppendLine($"{msg.Role}: {msg.Content}");

        return sb.ToString().TrimEnd();
    }

    private static void AddCorsHeaders(HttpListenerResponse resp)
    {
        resp.Headers["Access-Control-Allow-Origin"] = "*";
        resp.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        resp.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
    }

    private static async Task WriteJsonAsync(
        HttpListenerResponse resp,
        object body,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json);

        resp.ContentType = "application/json";
        resp.ContentEncoding = Encoding.UTF8;
        resp.ContentLength64 = bytes.Length;
        resp.StatusCode = resp.StatusCode == 0 ? 200 : resp.StatusCode;

        await resp.OutputStream.WriteAsync(bytes, ct);
        resp.Close();
    }

    // ── Request models ─────────────────────────────────────────────────────────

    private sealed class NomiAskRequest
    {
        [JsonPropertyName("nomiNames")] public List<string>? NomiNames { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = default!;
        [JsonPropertyName("history")] public List<HistoryMessage>? History { get; set; }
    }

    private sealed class HistoryMessage
    {
        [JsonPropertyName("role")] public string Role { get; set; } = default!;
        [JsonPropertyName("content")] public string Content { get; set; } = default!;
    }

    private sealed class OpenAiChatRequest
    {
        [JsonPropertyName("model")] public string? Model { get; set; }
        [JsonPropertyName("messages")] public List<OpenAiChatMessage>? Messages { get; set; }
        [JsonPropertyName("stream")] public bool? Stream { get; set; }
    }

    private sealed class OpenAiChatMessage
    {
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}