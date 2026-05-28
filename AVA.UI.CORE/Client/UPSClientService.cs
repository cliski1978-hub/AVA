// ─────────────────────────────────────────────────────────────────────────────
//  Class     : UPSClientService
//  Namespace : AVA.UI.CORE.UPS.Client
//  Purpose   : The UI's single interface to the embedded UPS layer.
//              Wires up the module registry, adapter registry, router,
//              and gateway in one place.
//              All UI code calls this — nothing touches UPS internals directly.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter;
using AVA.UPS.Adapter.Adapter;
using AVA.UPS.Adapter.Gateway;
using AVA.UPS.Adapter.LLMAdapters;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Routing;
using AVA.UPS.Adapter.Transport;
using AVA.UPS.Adapter.Utils;
using AVA.UI.CORE.Models.Settings;

namespace AVA.UI.CORE.UPS.Client
{
    /// <summary>
    /// The UI's single interface to the embedded UPS layer.
    /// Manages module registration, adapter registration, and routing.
    /// </summary>
    public class UPSClientService
    {
        private static readonly HttpClient _HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(180)
        };

        private static readonly JsonSerializerOptions _JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly UPSModuleRegistry _ModuleRegistry;
        private readonly AdapterRegistry _AdapterRegistry;
        private readonly InboundAdapterRegistry _InboundRegistry;
        private readonly ProtocolRouter _Router;
        private readonly UPSRoutingService _RoutingService;
        private readonly UPSGateway _Gateway;


        // ── LLM Adapter registry ──────────────────────────────────────────────
        // Keyed by "{profileId}:{modelId}" — one ILLMAdapter per model.
        private readonly ConcurrentDictionary<string, ILLMAdapter> _LLMAdapters =
            new(StringComparer.OrdinalIgnoreCase);

        // ── Constructor ───────────────────────────────────────────────────────

        public UPSClientService()
        {
            _ModuleRegistry = new UPSModuleRegistry();
            _AdapterRegistry = new AdapterRegistry();
            _InboundRegistry = new InboundAdapterRegistry();
            _Router = new ProtocolRouter(_ModuleRegistry, _AdapterRegistry);
            _RoutingService = new UPSRoutingService(_Router);
            _Gateway = new UPSGateway(_InboundRegistry, _RoutingService);

            // Register inbound adapters
            _InboundRegistry.Register(new UPSNativeInboundAdapter());
            _InboundRegistry.Register(new OpenAiInboundAdapter());
        }


       

        // ── HTTP Endpoint Setup ───────────────────────────────────────────────

        /// <summary>
        /// Registers any HTTP endpoint as a UPS module.
        /// Use this for standalone UPS hosts, remote APIs, or other services.
        /// </summary>
        public async Task RegisterHttpEndpointAsync(
            string ModuleName,
            string EndpointUrl,
            Dictionary<string, string>? DefaultHeaders = null,
            CancellationToken Token = default)
        {
            var Adapter = new HttpProtocolAdapter();
            await _AdapterRegistry.RegisterAsync(Adapter, new HttpAdapterConfig
            {
                BaseUrl = EndpointUrl,
                DefaultHeaders = DefaultHeaders
            }, ModuleName);

            _ModuleRegistry.Register(new UPSModuleInfo
            {
                Name = ModuleName,
                Transport = "http",
                Endpoint = EndpointUrl
            });
        }

        // ── LLM Profile Registration ──────────────────────────────────────────

        /// <summary>
        /// Registers all models within LLM profile.
        /// Creates an ILLMAdapter for each model and stores it in the adapter registry.
        /// Supports Anthropic (Claude) and any OpenAI-compatible endpoint.
        /// </summary>
        public Task RegisterLLMProfileAsync(
            LLMProfile profile,
            CancellationToken token = default)
        {
            return RegisterLLMProfileInternalAsync(profile, token);
        }

        private Task RegisterLLMProfileInternalAsync(
            LLMProfile profile,
            CancellationToken token)
        {
            foreach (var model in profile.Models)
            {
                var config = new LLMAdapterConfig
                {
                    ProfileId          = profile.ProfileId,
                    ProfileName        = profile.Name,
                    Provider           = profile.ResolvedProvider,
                    Endpoint           = profile.Endpoint,
                    ApiKey             = profile.ApiKey,
                    CustomHeadersAsText = profile.CustomHeadersAsText,
                    ModelId            = model.Id,
                    ModelLabel         = model.Label,
                    Temperature        = model.Temperature ?? profile.Temperature,
                    MaxTokens          = model.MaxTokens   ?? profile.MaxTokens,
                    SystemPrompt       = model.SystemPrompt
                };

                var key = AdapterKey(profile.ProfileId, model.Id);

                ILLMAdapter adapter = CreateLLMAdapter(config);

                _LLMAdapters[key] = adapter;
            }

            return Task.CompletedTask;
        }

        private static string AdapterKey(string profileId, string modelId) =>
            $"{profileId}:{modelId}";

        public Task<List<ModelProfile>> DiscoverModelsAsync(
            LLMProfile profile,
            CancellationToken cancellationToken = default)
        {
            return DiscoverModelsInternalAsync(profile, cancellationToken);
        }

        public bool SupportsModelDiscovery(LLMProfile profile)
        {
            return ShouldDiscoverModels(profile);
        }

        private static bool ShouldDiscoverModels(LLMProfile profile)
        {
            return IsOpenAiProvider(profile)
                || IsVeniceProvider(profile);
        }

        private static async Task<List<ModelProfile>> DiscoverModelsInternalAsync(
            LLMProfile profile,
            CancellationToken cancellationToken)
        {
            if (!ShouldDiscoverModels(profile))
            {
                return new List<ModelProfile>();
            }

            if (string.IsNullOrWhiteSpace(profile.Endpoint) || string.IsNullOrWhiteSpace(profile.ApiKey))
            {
                return new List<ModelProfile>();
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, BuildModelsEndpoint(profile.Endpoint));
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {profile.ApiKey}");

                if (!string.IsNullOrWhiteSpace(profile.Secret))
                {
                    request.Headers.TryAddWithoutValidation("OpenAI-Organization", profile.Secret);
                }

                foreach (var header in ParseCustomHeaders(profile.CustomHeadersAsText))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                using var response = await _HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return new List<ModelProfile>();
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var payload = await JsonSerializer.DeserializeAsync<OpenAiModelsResponse>(
                        stream,
                        _JsonOptions,
                        cancellationToken)
                    .ConfigureAwait(false);

                var providerType = GetDiscoveredModelType(profile);
                var modelFilter = GetDiscoveredModelFilter(profile);

                return payload?.Data?
                    .Where(model => !string.IsNullOrWhiteSpace(model.Id))
                    .Select(model => model.Id!.Trim())
                    .Where(modelFilter)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(modelId => new ModelProfile
                    {
                        Id = modelId,
                        Label = modelId,
                        Type = providerType,
                        IsDiscovered = true,
                        IsActive = false
                    })
                    .ToList()
                    ?? new List<ModelProfile>();
            }
            catch
            {
                return new List<ModelProfile>();
            }
        }

        private static string BuildModelsEndpoint(string endpoint)
        {
            var baseUrl = endpoint.Trim().TrimEnd('/');

            if (baseUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return $"{baseUrl[..^"/chat/completions".Length]}/models";
            }

            if (baseUrl.EndsWith("/completions", StringComparison.OrdinalIgnoreCase))
            {
                return $"{baseUrl[..^"/completions".Length]}/models";
            }

            if (baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return $"{baseUrl}/models";
            }

            return $"{baseUrl}/v1/models";
        }

        private static bool IsSupportedOpenAiChatModel(string modelId)
        {
            return modelId.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase)
                || modelId.StartsWith("chatgpt-", StringComparison.OrdinalIgnoreCase)
                || modelId.StartsWith("o1", StringComparison.OrdinalIgnoreCase)
                || modelId.StartsWith("o3", StringComparison.OrdinalIgnoreCase)
                || modelId.StartsWith("o4", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedVeniceModel(string modelId)
        {
            return !string.IsNullOrWhiteSpace(modelId);
        }

        private static Func<string, bool> GetDiscoveredModelFilter(LLMProfile profile)
        {
            if (IsVeniceProvider(profile))
            {
                return IsSupportedVeniceModel;
            }

            return IsSupportedOpenAiChatModel;
        }

        private static string GetDiscoveredModelType(LLMProfile profile)
        {
            return IsVeniceProvider(profile)
                ? "venice"
                : "openai";
        }

        private static bool IsOpenAiProvider(LLMProfile profile)
        {
            var provider = profile.ResolvedProvider?.Trim() ?? string.Empty;
            return provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
                || provider.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase)
                || provider.Contains("openai", StringComparison.OrdinalIgnoreCase)
                || provider.Contains("chatgpt", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsVeniceProvider(LLMProfile profile)
        {
            var provider = profile.ResolvedProvider?.Trim() ?? string.Empty;
            return provider.Equals("Venice", StringComparison.OrdinalIgnoreCase)
                || provider.Contains("venice", StringComparison.OrdinalIgnoreCase);
        }

        private static Dictionary<string, string> ParseCustomHeaders(string customHeadersAsText)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(customHeadersAsText))
            {
                return headers;
            }

            foreach (var rawLine in customHeadersAsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = rawLine.Split('=', 2);
                if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    headers[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return headers;
        }

        private static ILLMAdapter CreateLLMAdapter(LLMAdapterConfig config)
        {
            return config.Provider.Trim() switch
            {
                var provider when provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase) =>
                    new ClaudeAdapter(config),
                var provider when provider.Equals("Claude", StringComparison.OrdinalIgnoreCase) =>
                    new ClaudeAdapter(config),
                var provider when provider.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase) =>
                    new ChatGPTAdapter(config),
                var provider when provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) =>
                    new ChatGPTAdapter(config),
                var provider when provider.Equals("DeepSeek", StringComparison.OrdinalIgnoreCase) =>
                    new DeepSeekAdapter(config),
                var provider when provider.Equals("Venice", StringComparison.OrdinalIgnoreCase) =>
                    new VeniceAdapter(config),
                _ => new OpenAiCompatibleAdapter(config)
            };
        }

        // ── Send ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a message to a registered module via the UPS gateway.
        /// Returns the response envelope.
        /// </summary>
        public async Task<UPSMessageEnvelope?> SendAsync(
            string Target,
            string Method,
            List<UParam> Params,
            string? CorrelationId = null,
            CancellationToken Token = default)
        {
            var Envelope = new UPSMessageEnvelope
            {
                ID = Guid.NewGuid().ToString(),
                Source = "AVA.UI",
                Target = Target,
                TargetMethod = Method,
                CorrelationId = CorrelationId ?? Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Payload = Params
            };

            var Bytes = UPSJsonSerializer.SerializeToBytes(Envelope);
            var Headers = new Dictionary<string, string>
            {
                ["format"] = "ups-native"
            };

            var ResponseBytes = await _Gateway.SendAsync(
                Bytes,
                Headers,
                "ups-native",
                Token);

            return UPSJsonSerializer.DeserializeFromBytes<UPSMessageEnvelope>(ResponseBytes);
        }

        /// <summary>
        /// Sends a logical payload to a specific LLM profile and model.
        /// All others: routes directly through the registered ILLMAdapter.
        /// </summary>
        public async Task<UPSResponse> SendAsync(
            UPSPayload payload,
            LLMProfile profile,
            ModelProfile? model = null,
            CancellationToken Token = default)
        {
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(profile);

            if (model != null)
            {
                var key = AdapterKey(profile.ProfileId, model.Id);
                if (_LLMAdapters.TryGetValue(key, out var adapter))
                {
                    var result = await adapter.SendAsync(payload, Token).ConfigureAwait(false);
                    // Ensure Content is set for callers that use ExtractContent fallback
                    if (result.Success && result.Content != null && result.Payload.Count == 0)
                    {
                        result.Payload.Add(
                            new UParam { Key = "content", Type = "string", Value = result.Content });
                    }
                    return result;
                }

                // Adapter not registered yet — register on demand and retry
                await RegisterLLMProfileAsync(profile, Token).ConfigureAwait(false);
                if (_LLMAdapters.TryGetValue(key, out adapter))
                    return await adapter.SendAsync(payload, Token).ConfigureAwait(false);

                return new UPSResponse
                {
                    Success = false,
                    ErrorMessage = $"No adapter registered for model {model.Id} in profile {profile.Name}."
                };
            }

            var uparams = new List<UParam>
            {
                UParamFactory.String("userMessage", payload.Content),
                UParamFactory.String("formatHint", payload.FormatHint)
            };

            if (payload.Headers.Count > 0)
                uparams.Add(UParamFactory.Json("headers", payload.Headers));

            if (model != null)
            {
                uparams.Add(UParamFactory.String("modelProfileId", model.Id));
                uparams.Add(UParamFactory.String("modelLabel", model.Label));
            }

            var responseEnvelope = await SendAsync(
                "openai", "chat", uparams, Token: Token).ConfigureAwait(false);

            if (responseEnvelope == null)
                return new UPSResponse { Success = false, ErrorMessage = "No response from model." };

            var content = responseEnvelope.Payload?
                .FirstOrDefault(p => p.Key == "assistantMessage" || p.Key == "content")?
                .Value?.ToString();

            return new UPSResponse
            {
                Success      = responseEnvelope.Error == null,
                Content      = content,
                ModelId      = model?.Id,
                Payload      = responseEnvelope.Payload ?? new List<UParam>(),
                ErrorMessage = responseEnvelope.Error?.Message
            };
        }

        // ── Diagnostics ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns all registered module names.
        /// </summary>
        public IEnumerable<string> RegisteredModules =>
            _ModuleRegistry.AllModules.Select(M => M.Name);

        private sealed class OpenAiModelsResponse
        {
            [JsonPropertyName("data")]
            public List<OpenAiModelRecord>? Data { get; set; }
        }

        private sealed class OpenAiModelRecord
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }
    }
}
