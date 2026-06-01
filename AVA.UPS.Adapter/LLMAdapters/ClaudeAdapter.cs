// ─────────────────────────────────────────────────────────────────────────────
//  Class     : ClaudeAdapter
//  Namespace : AVA.UPS.Adapter.LLMAdapters
//  Purpose   : ILLMAdapter for Anthropic Claude.
//              Uses the Anthropic Messages API (/v1/messages) which differs
//              from the OpenAI format. Config comes from LLMAdapterConfig
//              (populated by UPSClientService from LLMProfile + ModelProfile).
//              Default model: claude-sonnet-4-20250514 (per AVA CLAUDE.md).
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
    /// ILLMAdapter for Anthropic Claude.
    /// POST /v1/messages with x-api-key and anthropic-version headers.
    /// </summary>
    public sealed class ClaudeAdapter : ILLMAdapter
    {
        private readonly LLMAdapterConfig _config;
        private readonly HttpClient _http;

        private const string AnthropicVersion = "2023-06-01";
        private const string DefaultEndpoint  = "https://api.anthropic.com";
        private const string DefaultModel     = "claude-sonnet-4-20250514";

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <inheritdoc />
        public string ProviderId => "anthropic";

        /// <inheritdoc />
        public string DisplayName => _config.ProfileName;

        /// <inheritdoc />
        public string ModelName => string.IsNullOrWhiteSpace(_config.ModelId) ? DefaultModel : _config.ModelId;

        /// <summary>
        /// Initializes the adapter from a config bag.
        /// </summary>
        public ClaudeAdapter(LLMAdapterConfig config, HttpClient? httpClient = null)
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
                return new LLMConnectionResult { IsSuccess = test.IsSuccess, Message = test.Message, ConnectedAt = DateTime.UtcNow };
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

            var systemPrompt = _config.SystemPrompt;
            if (payload.Headers.TryGetValue("system-prompt", out var sp) && !string.IsNullOrWhiteSpace(sp))
                systemPrompt = sp;

            var request = new AnthropicRequest
            {
                Model       = ModelName,
                MaxTokens   = _config.MaxTokens > 0 ? _config.MaxTokens : 2048,
                Temperature = _config.Temperature > 0 ? _config.Temperature : 0.7,
                System      = systemPrompt,
                Messages    = new List<AnthropicMessage>
                {
                    new AnthropicMessage { Role = "user", Content = payload.Content }
                }
            };

            var requestJson = JsonSerializer.Serialize(request, _jsonOpts);
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildEndpointUrl())
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.TryAddWithoutValidation("x-api-key", _config.ApiKey);
            httpRequest.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);

            try
            {
                var httpResponse = await _http.SendAsync(httpRequest, cancellationToken);
                var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return new UPSResponse
                    {
                        Success = false,
                        ErrorMessage = BuildHttpErrorMessage(httpResponse, responseJson),
                        ModelId = _config.ModelId,
                        ProviderResponse = responseJson,
                        RespondedAt = DateTime.UtcNow
                    };
                }

                var result = JsonSerializer.Deserialize<AnthropicResponse>(responseJson, _jsonOpts);
                var responseText = result?.Content?[0]?.Text ?? "(empty response)";

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

        private static string BuildHttpErrorMessage(HttpResponseMessage response, string responseBody)
        {
            var body = string.IsNullOrWhiteSpace(responseBody)
                ? string.Empty
                : $" Body: {responseBody[..Math.Min(500, responseBody.Length)]}";

            return $"HTTP {(int)response.StatusCode} {response.StatusCode}.{body}";
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
                MaxTokens                = 8192,
                MaxTemperature           = 1.0f,
                SupportsStreaming        = false,
                SystemPromptCapabilities = new List<string> { "full", "context-aware" }
            });

        private string BuildEndpointUrl()
        {
            var baseUrl = string.IsNullOrWhiteSpace(_config.Endpoint) ? DefaultEndpoint : _config.Endpoint.TrimEnd('/');

            if (baseUrl.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase))
            {
                return baseUrl;
            }

            if (baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return $"{baseUrl}/messages";
            }

            return $"{baseUrl}/v1/messages";
        }

        private sealed class AnthropicRequest
        {
            [JsonPropertyName("model")]       public string? Model { get; set; }
            [JsonPropertyName("max_tokens")]  public int MaxTokens { get; set; }
            [JsonPropertyName("temperature")] public double Temperature { get; set; }
            [JsonPropertyName("system")]      public string? System { get; set; }
            [JsonPropertyName("messages")]    public List<AnthropicMessage>? Messages { get; set; }
        }

        private sealed class AnthropicMessage
        {
            [JsonPropertyName("role")]    public string? Role { get; set; }
            [JsonPropertyName("content")] public string? Content { get; set; }
        }

        private sealed class AnthropicResponse
        {
            [JsonPropertyName("content")] public List<AnthropicContent>? Content { get; set; }
        }

        private sealed class AnthropicContent
        {
            [JsonPropertyName("text")] public string? Text { get; set; }
        }
    }
}
