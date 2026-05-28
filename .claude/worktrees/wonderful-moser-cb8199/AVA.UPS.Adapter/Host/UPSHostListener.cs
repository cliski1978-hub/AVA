// ─────────────────────────────────────────────────────────────────────────────
//  Class     : UPSHostListener
//  Namespace : AVA.UPS.Adapter.Host
//  Purpose   : Base class for all UPS host listeners.
//              Accepts raw bytes from any transport.
//              Hands everything to UPSGateway — no format logic here.
//              Subclasses override FormatHint and ExtractHeaders to provide
//              transport-specific metadata to the gateway.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Gateway;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Utils;

namespace AVA.UPS.Adapter.Host
{
    /// <summary>
    /// Base class for all UPS host listeners.
    /// Reads raw bytes from any transport stream and routes them
    /// through the UPSGateway — the single entry point for all inbound traffic.
    /// </summary>
    public abstract class UPSHostListener : IUPSHostTransport
    {
        /// <summary>
        /// The host context containing the gateway and options.
        /// </summary>
        protected UPSHostContext _Context = default!;

        /// <summary>
        /// The single UPS gateway entry point.
        /// </summary>
        protected UPSGateway _Gateway = default!;

        /// <summary>
        /// Initializes the listener with the host context.
        /// </summary>
        public virtual Task StartAsync(UPSHostContext Context, CancellationToken Token)
        {
            _Context = Context;
            _Gateway = Context.Gateway;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public virtual Task StopAsync(CancellationToken Token) =>
            Task.CompletedTask;

        // ── Core Request Handling Pipeline ────────────────────────────────────

        /// <summary>
        /// Reads raw bytes from the input stream, routes through the gateway,
        /// and writes the response bytes to the output stream.
        /// This is the only request handling logic — format agnostic.
        /// </summary>
        protected async Task HandleRequestAsync(
            Stream Input,
            Stream Output,
            CancellationToken Token)
        {
            try
            {
                // ── 1. Read raw bytes from transport ──────────────────────────
                byte[] Payload;
                using (var Ms = new MemoryStream())
                {
                    await Input.CopyToAsync(Ms, Token);
                    Payload = Ms.ToArray();
                }

                if (Payload.Length == 0)
                {
                    await WriteErrorAsync(Output, "EMPTY_PAYLOAD", "Request body is empty.", Token);
                    return;
                }

                // ── 2. Extract transport metadata ─────────────────────────────
                var Headers = ExtractHeaders();

                // ── 3. Single entry point — gateway handles everything ─────────
                var ResponseBytes = await _Gateway.SendAsync(
                    Payload,
                    Headers,
                    FormatHint,
                    Token);

                // ── 4. Write response bytes to transport ──────────────────────
                await Output.WriteAsync(ResponseBytes, 0, ResponseBytes.Length, Token);
            }
            catch (Exception Ex)
            {
                await WriteErrorAsync(Output, "HOST_EXCEPTION", Ex.Message, Token);
            }
        }

        // ── Overridable Hooks ─────────────────────────────────────────────────

        /// <summary>
        /// Override to provide a format hint from transport-specific metadata.
        /// For example HTTP Content-Type header, TCP frame prefix, pipe handshake.
        /// Returns null to use auto-detection.
        /// </summary>
        protected virtual string? FormatHint => null;

        /// <summary>
        /// Override to extract transport-specific headers or metadata
        /// and pass them to the gateway for adapter detection and routing hints.
        /// </summary>
        protected virtual Dictionary<string, string> ExtractHeaders() =>
            new Dictionary<string, string>();

        // ── Error Writer ──────────────────────────────────────────────────────

        private static async Task WriteErrorAsync(
            Stream Output,
            string Code,
            string Message,
            CancellationToken Token)
        {
            var Envelope = new UPSMessageEnvelope
            {
                ID = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Error = new UPSMessageError
                {
                    Code = Code,
                    Message = Message
                }
            };

            var Bytes = UPSJsonSerializer.SerializeToBytes(Envelope);
            await Output.WriteAsync(Bytes, 0, Bytes.Length, Token);
        }
    }
}