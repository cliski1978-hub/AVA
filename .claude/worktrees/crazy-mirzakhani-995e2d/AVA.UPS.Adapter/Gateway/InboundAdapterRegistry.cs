// ─────────────────────────────────────────────────────────────────────────────
//  Class     : InboundAdapterRegistry
//  Namespace : AVA.UPS.Adapter.Gateway
//  Purpose   : Registry of all inbound adapters.
//              Supports explicit format lookup and auto-detection by priority.
//              This is the only place adapter selection logic lives.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AVA.UPS.Adapter.Gateway
{
    public class InboundAdapterRegistry
    {
        private readonly ConcurrentDictionary<string, IInboundAdapter> _Adapters =
            new(StringComparer.OrdinalIgnoreCase);

        // ── Registration ──────────────────────────────────────────────────────

        public void Register(IInboundAdapter Adapter)
        {
            if (Adapter == null)
                throw new ArgumentNullException(nameof(Adapter));

            _Adapters[Adapter.FormatName] = Adapter;
        }

        // ── Explicit Lookup ───────────────────────────────────────────────────

        public IInboundAdapter? Get(string FormatName)
        {
            if (string.IsNullOrWhiteSpace(FormatName))
                return null;

            _Adapters.TryGetValue(FormatName, out var Adapter);
            return Adapter;
        }

        // ── Auto-Detection ────────────────────────────────────────────────────

        /// <summary>
        /// Inspects raw bytes and headers to find the first adapter
        /// that claims it can handle the payload, ordered by Priority.
        /// Returns null if no adapter matches.
        /// </summary>
        public IInboundAdapter? Detect(
            byte[] Payload,
            Dictionary<string, string> Headers)
        {
            return _Adapters.Values
                .OrderBy(A => A.Priority)
                .FirstOrDefault(A => A.CanHandle(Payload, Headers));
        }

        /// <summary>
        /// Resolves an adapter by explicit format name first.
        /// Falls back to auto-detection if format is null or unknown.
        /// </summary>
        public IInboundAdapter? Resolve(
            byte[] Payload,
            Dictionary<string, string> Headers,
            string? FormatHint = null)
        {
            if (!string.IsNullOrWhiteSpace(FormatHint))
            {
                var Explicit = Get(FormatHint);
                if (Explicit != null)
                    return Explicit;
            }

            return Detect(Payload, Headers);
        }

        // ── Diagnostics ───────────────────────────────────────────────────────

        public IEnumerable<string> RegisteredFormats =>
            _Adapters.Keys.OrderBy(K => K);
    }
}