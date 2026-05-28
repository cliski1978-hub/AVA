// ─────────────────────────────────────────────────────────────────────────────
//  Class     : OpenAiCompatibleAdapter
//  Namespace : AVA.UPS.Adapter.LLMAdapters
//  Purpose   : Single ILLMAdapter for all OpenAI-compatible endpoints.
//              Handles ChatGPT, DeepSeek, Venice, or any provider that follows
//              the /v1/chat/completions API. Config comes from LLMAdapterConfig
//              (populated by UPSClientService from LLMProfile + ModelProfile).
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.LLMAdapters
{
    /// <summary>
    /// ILLMAdapter for any OpenAI-compatible endpoint.
    /// One class handles ChatGPT, DeepSeek, Venice, and any other provider
    /// that implements /v1/chat/completions.
    /// </summary>
    public sealed class OpenAiCompatibleAdapter : ILLMAdapter
    {
        private readonly LLMAdapterConfig _config;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <inheritdoc />
        public string ProviderId => _config.Provider.ToLowerInvariant();

        /// <inheritdoc />
        public string DisplayName => _config.ProfileName;

        /// <inheritdoc />
        public string ModelName => _config.ModelId;

        /// <summary>
        /// Initializes the adapter from a config bag.
        /// </summary>
        public OpenAiCompatibleAdapter(LLMAdapterConfig config, HttpClient? httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _http   = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        /// <inheritdoc />
        public async Task<LLMConnectionResult> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var test = await TestAsync(cancellationToken);
                return new LLMConnectionResult
                {
                    IsSuccess   = test.IsSuccess,
                    Message     = test.Message,
                    ConnectedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                return new LLMConnectionResult { IsSuccess = false, Message = $"Connection failed: {ex.Message}" };
            }
        }

        /// <inheritdoc />
        public async Task<UPSResponse> SendAsync(UPSPayload payload, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var messages = new List<ChatMessage>();

            var systemPrompt = _config.SystemPrompt;
            if (payload.Headers.TryGetValue("system-prompt", out var sp) && !string.IsNullOrWhiteSpace(sp))
                systemPrompt = sp;
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new ChatMessage { Role = "system", Content = systemPrompt });

            messages.Add(new ChatMessage { Role = "user", Content = payload.Content });

            var request = new ChatCompletionRequest
            {
                Model       = _config.ModelId,
                Messages    = messages,
                Temperature = _config.Temperature,
                MaxTokens   = _config.MaxTokens
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOpts);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildEndpointUrl())
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_config.ApiKey}");

            foreach (var header in ParseCustomHeaders())
                httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);

            try
            {
                var httpResponse = await _http.SendAsync(httpRequest, cancellationToken);
                httpResponse.EnsureSuccessStatusCode();

                var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, _jsonOpts);
                var responseText = result?.Choices?[0]?.Message?.Content ?? "(empty response)";

                return new UPSResponse
                {
                    Success = true,
                    Content = responseText,
                    ModelId = _config.ModelId,
                    ProviderResponse = responseJson,
                    RespondedAt = DateTime.UtcNow,
                    Payload = new List<UParam> { new UParam { Key = "content", Type = "string", Value = responseText } }
                };
            }
            catch (HttpRequestException ex)
            {
                return new UPSResponse { Success = false, ErrorMessage = $"HTTP error: {ex.Message}", ModelId = _config.ModelId };
            }
            catch (TaskCanceledException)
            {
                return new UPSResponse { Success = false, ErrorMessage = "Request timed out.", ModelId = _config.ModelId };
            }

        }

        /// <inheritdoc />
        public async Task<LLMTestResult> TestAsync(CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var response = await SendAsync(new UPSPayload { Content = "Reply with exactly: OK", FormatHint = "text/plain" }, cancellationToken);
                sw.Stop();
                return new LLMTestResult
                {
                    IsSuccess = response.Success,
                    Message   = response.Success ? "Connected" : response.ErrorMessage ?? "Failed",
                    LatencyMs = sw.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new LLMTestResult { IsSuccess = false, Message = ex.Message, LatencyMs = sw.Elapsed.TotalMilliseconds };
            }
        }

        /// <inheritdoc />
        public Task DisconnectAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task<LLMCapabilitySet> GetCapabilitiesAsync() =>
            Task.FromResult(new LLMCapabilitySet
            {
                MaxTokens                = 4096,
                MaxTemperature           = 2.0f,
                SupportsStreaming        = false,
                SystemPromptCapabilities = new List<string> { "full" }
            });

        private string BuildEndpointUrl()
        {
            var baseUrl = _config.Endpoint.TrimEnd('/');
            return baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
                ? baseUrl
                : $"{baseUrl}/v1/chat/completions";
        }

        private Dictionary<string, string> ParseCustomHeaders()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(_config.CustomHeadersAsText)) return headers;
            foreach (var line in _config.CustomHeadersAsText.Split('\n'))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2) headers[parts[0].Trim()] = parts[1].Trim();
            }
            return headers;
        }

        private sealed class ChatCompletionRequest
        {
            [JsonPropertyName("model")]    public string? Model { get; set; }
            [JsonPropertyName("messages")] public List<ChatMessage>? Messages { get; set; }
            [JsonPropertyName("temperature")] public double Temperature { get; set; }
            [JsonPropertyName("max_tokens")]  public int MaxTokens { get; set; }
        }

        private sealed class ChatMessage
        {
            [JsonPropertyName("role")]    public string? Role { get; set; }
            [JsonPropertyName("content")] public string? Content { get; set; }
        }

        private sealed class ChatCompletionResponse
        {
            [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
            public sealed class Choice
            {
                [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
            }
        }
    }
}
