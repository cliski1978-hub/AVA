// ─────────────────────────────────────────────────────────────────────────────
//  Class     : UPSNativeInboundAdapter
//  Namespace : AVA.UPS.Adapter.Adapter
//  Purpose   : Passthrough inbound adapter for native UPSMessageEnvelope format.
//              When the caller already speaks UPS this adapter deserializes
//              the envelope directly with zero translation.
//              Always registered at lowest priority so auto-detection
//              tries all other adapters first.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Gateway;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Utils;

namespace AVA.UPS.Adapter.Adapter
{
    public class UPSNativeInboundAdapter : IInboundAdapter
    {
        public string FormatName => "ups-native";
        public int Priority => 100;

        // ── Detection ─────────────────────────────────────────────────────────

        public bool CanHandle(byte[] Payload, Dictionary<string, string> Headers)
        {
            try
            {
                var Json = System.Text.Encoding.UTF8.GetString(Payload);
                var Doc = JsonDocument.Parse(Json);
                var Root = Doc.RootElement;

                return (Root.TryGetProperty("iD", out _) ||
                        Root.TryGetProperty("ID", out _) ||
                        Root.TryGetProperty("id", out _)) &&
                        Root.TryGetProperty("target", out _);
            }
            catch
            {
                return false;
            }
        }

        // ── Translate Inbound ─────────────────────────────────────────────────

        public Task<UPSMessageEnvelope> TranslateAsync(
            byte[] Payload,
            Dictionary<string, string> Headers,
            CancellationToken Token = default)
        {
            var Envelope = UPSJsonSerializer.DeserializeFromBytes<UPSMessageEnvelope>(Payload)
                ?? throw new InvalidOperationException(
                    "Failed to deserialize native UPSMessageEnvelope.");

            return Task.FromResult(Envelope);
        }

        // ── Translate Response ────────────────────────────────────────────────

        public Task<byte[]> TranslateResponseAsync(
            UPSMessageEnvelope Response,
            CancellationToken Token = default)
        {
            return Task.FromResult(
                UPSJsonSerializer.SerializeToBytes(Response));
        }
    }
}