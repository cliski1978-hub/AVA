using System;
using System.Threading;
using AVA.Identity.Abstractions;

namespace AVA.UPS.Adapter.Utils
{
    /// <summary>
    /// UPS ID Generator that consumes Identity layer information to produce
    /// globally unique, swarm-safe identifiers for UPS message envelopes.
    /// </summary>
    public class UPSIdGenerator
    {
        private readonly IUPSIdentityProvider identity;
        private long sequence = 0;

        public UPSIdGenerator(IUPSIdentityProvider identityProvider)
        {
            identity = identityProvider;
        }

        /// <summary>
        /// Generates the primary unique ID for a UPSMessageEnvelope.
        /// </summary>
        public string NewEnvelopeId()
        {
            long seq = Interlocked.Increment(ref sequence);

            return UPSIdComposer.Compose(
                nodeId: identity.GetNodeId(),
                moduleId: identity.GetModuleId(),
                processId: identity.GetProcessId(),
                sequence: seq
            );
        }

        /// <summary>
        /// Generates a correlation ID with a specific prefix (e.g. REQ-xxxxx).
        /// </summary>
        public string NewCorrelationId(string prefix = "REQ")
        {
            return $"{prefix}-{NewEnvelopeId()}";
        }

        /// <summary>
        /// Generates a short hop-level trace ID (router/debug).
        /// </summary>
        public string NewHopId()
        {
            return NewEnvelopeId()[..12];
        }
    }
}
