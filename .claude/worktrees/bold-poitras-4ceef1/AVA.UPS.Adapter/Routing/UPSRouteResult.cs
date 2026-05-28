using System.Collections.Generic;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.Routing
{
    public class UPSRouteResult
    {
        public bool Success { get; set; }
        public UPSMessageEnvelope? Envelope { get; set; }
        public UPSRouteError? Error { get; set; }
        public UPSRouteDecision? Decision { get; set; }
        public Dictionary<string, object>? Diagnostics { get; set; }

        public static UPSRouteResult FromSuccess(
            UPSMessageEnvelope envelope,
            UPSRouteDecision decision,
            Dictionary<string, object> diagnostics)
        {
            return new UPSRouteResult
            {
                Success = true,
                Envelope = envelope,
                Decision = decision,
                Diagnostics = diagnostics
            };
        }

        public static UPSRouteResult FromError(
            UPSRouteError error,
            UPSRouteDecision decision,
            Dictionary<string, object> diagnostics)
        {
            return new UPSRouteResult
            {
                Success = false,
                Error = error,
                Decision = decision,
                Diagnostics = diagnostics
            };
        }
    }
}
