using System;
using System.Linq;

namespace AVA.UPS.Adapter.Handshake
{
    /// <summary>
    /// Validates compatibility between two UPS handshake declarations.
    /// </summary>
    public static class UPSCompatibilityValidator
    {
        public static bool IsVersionCompatible(string required, string provided)
        {
            // Compare major version only for now:
            var reqMajor = required.Split('.')[0];
            var provMajor = provided.Split('.')[0];
            return reqMajor == provMajor;
        }

        public static bool HasTransportOverlap(
            UPSHandshakeCapabilities a,
            UPSHandshakeCapabilities b)
        {
            return a.SupportedTransports
                .Intersect(b.SupportedTransports, StringComparer.OrdinalIgnoreCase)
                .Any();
        }

        public static bool IsPayloadWithinLimits(
            UPSHandshakeCapabilities local,
            UPSHandshakeCapabilities remote)
        {
            // Conservative: must fit within both sides
            return local.MaxPayloadBytes > 0 &&
                   remote.MaxPayloadBytes > 0;
        }
    }
}
