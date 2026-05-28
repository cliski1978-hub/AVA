using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Utils;
using AVA.UPS.Adapter.Transport;
using AVA.UPS.Adapter.Registry;
using AVA.UPS.Adapter.Adapter;

namespace AVA.UPS.Adapter.Routing
{
    public class ProtocolRouter
    {
        private readonly UPSModuleRegistry _modules;
        private readonly AdapterRegistry _adapters;

        public ProtocolRouter(UPSModuleRegistry modules, AdapterRegistry adapters)
        {
            _modules = modules;
            _adapters = adapters;
        }

        public async Task<UPSRouteResult> RouteAsync(
            UPSMessageEnvelope envelope,
            CancellationToken token = default)
        {
            var diagnostics = new Dictionary<string, object>();
            var decision = new UPSRouteDecision
            {
                Target = envelope.Target,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // 1. Resolve module
                var module = _modules.Resolve(envelope.Target);
                if (module == null)
                {
                    return UPSRouteResult.FromError(
                        UPSRouteError.ModuleNotFound(envelope.Target),
                        decision,
                        diagnostics);
                }

                decision.Transport = module.Transport;
                decision.Endpoint = module.Endpoint;

                // 2. Resolve adapter
                var adapter = _adapters.GetAdapter(module.Transport);
                if (adapter == null)
                {
                    return UPSRouteResult.FromError(
                        UPSRouteError.AdapterNotFound(module.Transport),
                        decision,
                        diagnostics);
                }

                diagnostics["adapter"] = adapter.ProtocolName;
                diagnostics["endpoint"] = module.Endpoint;

                // 3. Serialize
                var payload = UPSJsonSerializer.SerializeToBytes(envelope);
                diagnostics["payloadBytes"] = payload.Length;

                // 4. Send
                var responseBytes = await adapter.SendAsync(payload, token);
                if (responseBytes == null)
                {
                    return UPSRouteResult.FromError(
                        UPSRouteError.NullResponse(module.Name),
                        decision,
                        diagnostics);
                }

                diagnostics["responseBytes"] = responseBytes.Length;

                // 5. Deserialize
                var responseEnvelope =
                    UPSJsonSerializer.DeserializeFromBytes<UPSMessageEnvelope>(responseBytes);

                if (responseEnvelope == null)
                {
                    return UPSRouteResult.FromError(
                        UPSRouteError.InvalidEnvelope(module.Name),
                        decision,
                        diagnostics);
                }

                return UPSRouteResult.FromSuccess(responseEnvelope, decision, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics["exception"] = ex.Message;
                return UPSRouteResult.FromError(
                    UPSRouteError.ExceptionThrown(ex),
                    decision,
                    diagnostics);
            }
        }
    }
}
