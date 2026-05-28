// ─────────────────────────────────────────────────────────────────────────────
//  Class     : UPSClientService
//  Namespace : AVA.UI.CORE.UPS.Client
//  Purpose   : The UI's single interface to the embedded UPS layer.
//              Wires up the module registry, adapter registry, router,
//              and gateway in one place.
//              All UI code calls this — nothing touches UPS internals directly.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Nomi.Bridge;
using AVA.UPS.Adapter;
using AVA.UPS.Adapter.Adapter;
using AVA.UPS.Adapter.Gateway;
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
        private readonly UPSModuleRegistry _ModuleRegistry;
        private readonly AdapterRegistry _AdapterRegistry;
        private readonly InboundAdapterRegistry _InboundRegistry;
        private readonly ProtocolRouter _Router;
        private readonly UPSRoutingService _RoutingService;
        private readonly UPSGateway _Gateway;

        private NomiApiClient? _NomiClient;
        private NomiRoster? _NomiRoster;

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

        // ── Nomi Setup ────────────────────────────────────────────────────────

        /// <summary>
        /// Initializes the Nomi adapter with the provided API key.
        /// Loads the roster of Nomis and Rooms from the Nomi API.
        /// </summary>
        public async Task InitializeNomiAsync(
            string ApiKey,
            CancellationToken Token = default)
        {
            _NomiClient = new NomiApiClient(ApiKey);
            _NomiRoster = new NomiRoster(_NomiClient);

            await _NomiRoster.LoadAsync(Token);

            var Adapter = new NomiProtocolAdapter();
            await _AdapterRegistry.RegisterAsync(Adapter, new NomiAdapterConfig
            {
                Client = _NomiClient,
                NomiId = string.Empty
            });

            _ModuleRegistry.Register(new UPSModuleInfo
            {
                Name = "Nomi",
                Transport = "nomi-http",
                Endpoint = "https://api.nomi.ai"
            });
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

        // ── OpenAI-Compatible Endpoint Setup ──────────────────────────────────

        /// <summary>
        /// Registers an OpenAI-compatible LLM endpoint (ChatGPT, DeepSeek, Venice).
        /// The ProtocolName is used to route inbound requests to the correct module.
        /// </summary>
        public async Task RegisterOpenAiCompatibleEndpointAsync(
            string ModuleName,
            string ProtocolName,
            string BaseUrl,
            string ApiKey,
            CancellationToken Token = default)
        {
            IProtocolAdapter Adapter = ProtocolName switch
            {
                "openai-http" => new ChatGptProtocolAdapter(ApiKey),
                "deepseek-http" => new DeepSeekProtocolAdapter(ApiKey),
                "venice-http" => new VeniceProtocolAdapter(ApiKey),
                _ => throw new ArgumentException(
                    $"Unknown OpenAI-compatible protocol: {ProtocolName}")
            };

            await _AdapterRegistry.RegisterAsync(Adapter, null, ModuleName);

            _ModuleRegistry.Register(new UPSModuleInfo
            {
                Name = ModuleName,
                Transport = ProtocolName,
                Endpoint = BaseUrl
            });
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
        /// Sends a logical payload to a specific LLM profile and returns a normalized UPS response.
        /// </summary>
        public async Task<UPSResponse> SendAsync(
            UPSPayload payload,
            LLMProfile profile,
            ModelProfile? model = null,
            CancellationToken Token = default)
        {
            ArgumentNullException.ThrowIfNull(payload);
            ArgumentNullException.ThrowIfNull(profile);

            var uparams = new List<UParam>
            {
                UParamFactory.String("userMessage", payload.Content),
                UParamFactory.String("formatHint", payload.FormatHint)
            };

            if (payload.Headers.Count > 0)
            {
                uparams.Add(UParamFactory.Json("headers", payload.Headers));
            }

            if (model != null)
            {
                uparams.Add(UParamFactory.String("modelProfileId", model.Id));
                uparams.Add(UParamFactory.String("modelLabel", model.Label));
            }

            var target = ResolveTarget(profile);
            if (target.Equals("Nomi", StringComparison.OrdinalIgnoreCase) && model != null)
            {
                uparams.Add(UParamFactory.String("nomiId", model.Id));
            }

            var responseEnvelope = await SendAsync(
                target,
                "chat",
                uparams,
                Token: Token).ConfigureAwait(false);

            if (responseEnvelope == null)
            {
                return new UPSResponse
                {
                    Success = false,
                    ErrorMessage = "No response received from UPS."
                };
            }

            return new UPSResponse
            {
                Success = responseEnvelope.Error == null,
                Payload = responseEnvelope.Payload ?? new List<UParam>(),
                ErrorMessage = responseEnvelope.Error?.Message
            };
        }

        // ── Roster ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all available model entries from the Nomi roster.
        /// Used by the UI to populate session pickers.
        /// </summary>
        public IEnumerable<AVA.UPS.Adapter.Interfaces.IModelEntry> GetModelEntries()
        {
            return _NomiRoster?.GetModelEntries()
                ?? Array.Empty<AVA.UPS.Adapter.Interfaces.IModelEntry>();
        }

        /// <summary>
        /// Returns the NomiRoster for direct access if needed.
        /// </summary>
        public NomiRoster? GetNomiRoster() => _NomiRoster;

        // ── Diagnostics ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns all registered module names.
        /// </summary>
        public IEnumerable<string> RegisteredModules =>
            _ModuleRegistry.AllModules.Select(M => M.Name);

        private static string ResolveTarget(LLMProfile profile)
        {
            return profile.ResolvedProvider.Equals("Nomi", StringComparison.OrdinalIgnoreCase)
                ? "Nomi"
                : profile.Name;
        }
    }
}
