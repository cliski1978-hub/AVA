// ─────────────────────────────────────────────────────────────────────────────
//  Class     : OpenAiCompatibleProtocolAdapter
//  Namespace : AVA.Nomi.Bridge
//  Purpose   : Base class for OpenAI-compatible API adapters (ChatGPT, DeepSeek, Venice).
//              Translates UPS envelopes to OpenAI chat completions format and back.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Transport;
using AVA.UPS.Adapter.Utils;

namespace AVA.Nomi.Bridge;

public abstract class OpenAiCompatibleProtocolAdapter : IProtocolAdapter
{
    private HttpClient? _Http;

    /// <summary>
    /// Protocol name for UPS routing (e.g., "openai-http").
    /// </summary>
    public abstract string ProtocolName { get; }

    /// <summary>
    /// Base URL for the OpenAI-compatible API (e.g., "https://api.openai.com").
    /// </summary>
    protected abstract string BaseUrl { get; }

    /// <summary>
    /// API key for authentication.
    /// </summary>
    protected abstract string ApiKey { get; }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public async Task InitializeAsync(object? config = null)
    {
        _Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        _Http.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

        await Task.CompletedTask;
    }

    public async Task<byte[]> SendAsync(byte[] requestBytes, CancellationToken ct = default)
    {
        if (_Http is null)
            throw new InvalidOperationException($"{GetType().Name} has not been initialized.");

        var envelope = UPSJsonSerializer.DeserializeFromBytes<UPSMessageEnvelope>(requestBytes)
                       ?? throw new InvalidOperationException("Failed to deserialize inbound UPSMessageEnvelope.");

        var userMessage = envelope.Payload?
            .FirstOrDefault(p => p.Key == "userMessage")?
            .Value?.ToString()
                          ?? throw new InvalidOperationException("Inbound envelope missing 'userMessage' parameter.");

        var modelId = envelope.Payload?
            .FirstOrDefault(p => p.Key == "modelId")?
            .Value?.ToString()
                          ?? "gpt-4";

        // Build history from payload if present
        var historyParam = envelope.Payload?
            .FirstOrDefault(p => p.Key == "history");
        var messages = new List<ChatMessage>();

        if (historyParam?.Value is List<string> history)
        {
            foreach (var entry in history)
            {
                var parts = entry.Split(": ", 2);
                if (parts.Length == 2)
                {
                    messages.Add(new ChatMessage { Role = parts[0], Content = parts[1] });
                }
            }
        }

        // Add current user message
        messages.Add(new ChatMessage { Role = "user", Content = userMessage });

        var request = new ChatCompletionRequest
        {
            Model = modelId,
            Messages = messages,
            Temperature = 0.7,
            MaxTokens = 1024
        };

        var requestJson = JsonSerializer.Serialize(request, JsonOpts);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        string assistantMessage;

        try
        {
            var response = await _Http.PostAsync($"{BaseUrl.TrimEnd('/')}/v1/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, JsonOpts)
                         ?? throw new InvalidOperationException("Failed to deserialize API response.");

            assistantMessage = result.Choices?.FirstOrDefault()?.Message?.Content
                               ?? "No response content received.";
        }
        catch (HttpRequestException ex)
        {
            assistantMessage = $"Error: API request failed: {ex.Message}";
        }
        catch (TaskCanceledException)
        {
            assistantMessage = $"Error: API request timed out.";
        }

        var responseEnvelope = new UPSMessageEnvelope
        {
            Source = GetSourceName(),
            Target = envelope.Source,
            TargetMethod = envelope.TargetMethod,
            CorrelationId = envelope.CorrelationId,
            Timestamp = DateTime.UtcNow,
            Payload = new List<UParam>
            {
                UParamFactory.String("assistantMessage", assistantMessage)
            }
        };

        return UPSJsonSerializer.SerializeToBytes(responseEnvelope);
    }

    protected virtual string GetSourceName() => ProtocolName.Replace("-http", "");

    // ── Models ────────────────────────────────────────────────────────────────

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("messages")]
        public List<ChatMessage>? Messages { get; set; }

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }

        public sealed class Choice
        {
            [JsonPropertyName("message")]
            public ChatMessage? Message { get; set; }
        }
    }
}
