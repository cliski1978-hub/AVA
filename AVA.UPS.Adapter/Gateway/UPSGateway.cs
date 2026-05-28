// ─────────────────────────────────────────────────────────────────────────────
//  Class     : UPSGateway
//  Namespace : AVA.UPS.Adapter.Gateway
//  Purpose   : THE single entry point for all inbound UPS traffic.
//              Accepts raw bytes from any transport.
//              Resolves the correct inbound adapter.
//              Routes through the protocol router.
//              Returns raw bytes in the caller's format.
//              Nothing format-specific lives here — ever.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Routing;

namespace AVA.UPS.Adapter.Gateway
{
    public class UPSGateway
    {
        private readonly InboundAdapterRegistry _InboundAdapters;
        private readonly UPSRoutingService _Router;

        public UPSGateway(
            InboundAdapterRegistry InboundAdapters,
            UPSRoutingService Router)
        {
            _InboundAdapters = InboundAdapters;
            _Router = Router;
        }

        // ── Single Entry Point ────────────────────────────────────────────────

        /// <summary>
        /// Accepts any inbound payload as raw bytes.
        /// FormatHint is optional — if null auto-detection is used.
        /// Headers carry transport metadata, auth hints, routing hints.
        /// Returns raw bytes in the caller's expected format.
        /// </summary>
        public async Task<byte[]> SendAsync(
            byte[] Payload,
            Dictionary<string, string>? Headers = null,
            string? FormatHint = null,
            CancellationToken Token = default)
        {
            Headers ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // ── 1. Resolve inbound adapter ────────────────────────────────────
            var Adapter = _InboundAdapters.Resolve(Payload, Headers, FormatHint);

            if (Adapter == null)
            {
                return BuildErrorBytes(
                    "NO_ADAPTER",
                    $"No inbound adapter could handle the payload. " +
                    $"Registered formats: {string.Join(", ", _InboundAdapters.RegisteredFormats)}");
            }

            // ── 2. Translate payload → UPSMessageEnvelope ─────────────────────
            UPSMessageEnvelope Envelope;

            try
            {
                Envelope = await Adapter.TranslateAsync(Payload, Headers, Token);
            }
            catch (Exception Ex)
            {
                return BuildErrorBytes("TRANSLATION_ERROR", Ex.Message);
            }

            // ── 3. Route envelope ─────────────────────────────────────────────
            UPSRouteResult RouteResult;

            try
            {
                RouteResult = await _Router.RouteAsync(Envelope, Token);
            }
            catch (Exception Ex)
            {
                return BuildErrorBytes("ROUTING_ERROR", Ex.Message);
            }

            // ── 4. Build response envelope ────────────────────────────────────
            var ResponseEnvelope = RouteResult.Success
                ? RouteResult.Envelope ?? BuildErrorEnvelope("NULL_RESPONSE", "Router returned null envelope.")
                : BuildErrorEnvelope(
                    RouteResult.Error?.Code ?? "ROUTE_FAILED",
                    RouteResult.Error?.Message ?? "Routing failed.");

            // ── 5. Translate response → caller's format ───────────────────────
            try
            {
                return await Adapter.TranslateResponseAsync(ResponseEnvelope, Token);
            }
            catch (Exception Ex)
            {
                return BuildErrorBytes("RESPONSE_TRANSLATION_ERROR", Ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static UPSMessageEnvelope BuildErrorEnvelope(string Code, string Message)
        {
            return new UPSMessageEnvelope
            {
                ID = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Error = new UPSMessageError
                {
                    Code = Code,
                    Message = Message
                }
            };
        }

        private static byte[] BuildErrorBytes(string Code, string Message)
        {
            var Envelope = BuildErrorEnvelope(Code, Message);
            return AVA.UPS.Adapter.Utils.UPSJsonSerializer.SerializeToBytes(Envelope);
        }
    }
}