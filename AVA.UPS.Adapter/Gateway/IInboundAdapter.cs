// ─────────────────────────────────────────────────────────────────────────────
//  Interface : IInboundAdapter
//  Namespace : AVA.UPS.Adapter.Gateway
//  Purpose   : Translates any inbound byte payload into a UPSMessageEnvelope
//              and translates the response envelope back to the caller's format.
//              Implementations are the ONLY place format-specific logic lives.
//              The gateway never sees format details — only bytes in, bytes out.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.Gateway
{
    public interface IInboundAdapter
    {
        /// <summary>
        /// Unique name identifying this format.
        /// e.g. "ups-native", "openai", "raw"
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Priority order for auto-detection.
        /// Lower number = checked first.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Inspects raw bytes and headers to determine if this adapter
        /// can handle the incoming payload. Used for auto-detection when
        /// no format is explicitly specified.
        /// </summary>
        bool CanHandle(byte[] Payload, Dictionary<string, string> Headers);

        /// <summary>
        /// Translates raw inbound bytes into a UPSMessageEnvelope.
        /// This is the ONLY place format-specific inbound logic lives.
        /// </summary>
        Task<UPSMessageEnvelope> TranslateAsync(
            byte[] Payload,
            Dictionary<string, string> Headers,
            CancellationToken Token = default);

        /// <summary>
        /// Translates a UPSMessageEnvelope response back into bytes
        /// in the format the caller expects.
        /// This is the ONLY place format-specific outbound response logic lives.
        /// </summary>
        Task<byte[]> TranslateResponseAsync(
            UPSMessageEnvelope Response,
            CancellationToken Token = default);
    }
}