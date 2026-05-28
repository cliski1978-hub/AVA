// ─────────────────────────────────────────────────────────────────────────────
//  Class     : OpenAiInboundAdapter
//  Namespace : AVA.UPS.Adapter.Adapter
//  Purpose   : Translates inbound OpenAI chat completions format into a
//              UPSMessageEnvelope and translates the response back.
//              This is the ONLY place OpenAI format logic lives in UPS.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Gateway;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Utils;

namespace AVA.UPS.Adapter.Adapter
{
    public class OpenAiInboundAdapter : IInboundAdapter
    {
        public string FormatName => "openai";
        public int Priority => 10;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly Dictionary<string, string> _ModelPrefixToTarget;

        /// <summary>
        /// Maps model ID prefixes to their target module names.
        /// If modelId starts with a key, its value is the target module.
        /// </summary>
        public OpenAiInboundAdapter(Dictionary<string, string>? ModelPrefixToTarget = null)
        {
            _ModelPrefixToTarget = ModelPrefixToTarget ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "gpt-", "ChatGPT" },
                { "deepseek-", "DeepSeek" },
                { "venice-", "Venice" }
            };
        }

        // ── Detection ─────────────────────────────────────────────────────────

        public bool CanHandle(byte[] Payload, Dictionary<string, string> Headers)
        {
            try
            {
                var Json = Encoding.UTF8.GetString(Payload);
                var Doc = JsonDocument.Parse(Json);
                var Root = Doc.RootElement;

                return Root.TryGetProperty("messages", out _) &&
                       Root.ValueKind == JsonValueKind.Object;
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
            var Json = Encoding.UTF8.GetString(Payload);
            var Request = JsonSerializer.Deserialize<OpenAiChatRequest>(Json, JsonOpts)
                ?? throw new InvalidOperationException(
                    "Failed to deserialize OpenAI request.");

            var UserMessage = Request.Messages?
                .LastOrDefault(M => M.Role?.Equals(
                    "user", StringComparison.OrdinalIgnoreCase) == true)
                ?.Content
                ?? throw new InvalidOperationException(
                    "No user message found in OpenAI request.");

            var ModelId = Request.Model ?? string.Empty;

            var UParams = new List<UParam>
            {
                UParamFactory.String("userMessage", UserMessage),
                UParamFactory.String("modelId",     ModelId)
            };

            if (Request.Messages?.Count > 1)
            {
                var History = Request.Messages
                    .Where(M => M.Role?.Equals(
                        "system", StringComparison.OrdinalIgnoreCase) == false)
                    .Select(M => $"{M.Role}: {M.Content}")
                    .ToList();

                if (History.Count > 0)
                    UParams.Add(UParamFactory.StringList("history", History));
            }

            var Target = ResolveTarget(ModelId);

            var Envelope = new UPSMessageEnvelope
            {
                ID = Guid.NewGuid().ToString(),
                Source = "openai-inbound",
                Target = Target,
                TargetMethod = "chat",
                CorrelationId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Payload = UParams
            };

            return Task.FromResult(Envelope);
        }

        // ── Translate Response ────────────────────────────────────────────────

        public Task<byte[]> TranslateResponseAsync(
            UPSMessageEnvelope Response,
            CancellationToken Token = default)
        {
            var AssistantText = Response.Error != null
                ? $"Error: {Response.Error.Message}"
                : Response.Payload?
                    .FirstOrDefault(P => P.Key == "assistantMessage")
                    ?.Value?.ToString()
                  ?? string.Empty;

            var CompletionId = $"chatcmpl-{Guid.NewGuid():N}";
            var Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var Result = new
            {
                id = CompletionId,
                @object = "chat.completion",
                created = Created,
                model = Response.Source ?? "ups",
                choices = new[]
                {
                    new
                    {
                        index         = 0,
                        message       = new { role = "assistant", content = AssistantText },
                        finish_reason = "stop"
                    }
                },
                usage = new
                {
                    prompt_tokens = 0,
                    completion_tokens = 0,
                    total_tokens = 0
                }
            };

            var Bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Result, JsonOpts));
            return Task.FromResult(Bytes);
        }

        // ── Request Models ────────────────────────────────────────────────────

        private sealed class OpenAiChatRequest
        {
            [JsonPropertyName("model")]
            public string? Model { get; set; }

            [JsonPropertyName("messages")]
            public List<OpenAiChatMessage>? Messages { get; set; }

            [JsonPropertyName("stream")]
            public bool? Stream { get; set; }
        }

        private sealed class OpenAiChatMessage
        {
            [JsonPropertyName("role")]
            public string? Role { get; set; }

            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        // ── Target Resolution ────────────────────────────────────────────────

        private string ResolveTarget(string ModelId)
        {
           //ATTN CLIFF FIX

            foreach (var (Prefix, ModuleName) in _ModelPrefixToTarget)
            {
                if (ModelId.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    return ModuleName;
            }

            return "openai";
        }
    }
}