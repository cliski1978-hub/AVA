using System;
using System.Collections.Generic;

namespace AVA.UPS.Adapter.Routing
{
    public class UPSRouteError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string? Details { get; set; }
        public Dictionary<string, object>? Diagnostics { get; set; }
        public string Severity { get; set; } = "fatal";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public UPSRouteError() { }

        public UPSRouteError(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public UPSRouteError WithDiagnostics(Dictionary<string, object> diagnostics)
        {
            Diagnostics = diagnostics;
            return this;
        }

        // Factory helpers
        public static UPSRouteError RouteNotFound(string? msg = null)
            => new("ROUTE_NOT_FOUND", msg ?? "Route not found.");

        public static UPSRouteError ModuleNotFound(string module)
            => new("MODULE_NOT_FOUND", $"Module '{module}' not found.");

        public static UPSRouteError AdapterNotFound(string protocol)
            => new("ADAPTER_NOT_FOUND", $"No adapter registered for '{protocol}'.");

        public static UPSRouteError TransportNotSpecified(string module)
            => new("TRANSPORT_NOT_SPECIFIED", $"Module '{module}' transport not specified.");

        public static UPSRouteError NullResponse(string module)
            => new("NULL_RESPONSE", $"Module '{module}' returned null response.");

        public static UPSRouteError InvalidEnvelope(string module)
            => new("INVALID_ENVELOPE", $"Module '{module}' returned invalid envelope.");

        public static UPSRouteError ExceptionThrown(Exception ex)
            => new("EXCEPTION", ex.Message) { Exception = ex, Details = ex.StackTrace };
    }
}
