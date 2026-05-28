// ─────────────────────────────────────────────────────────────────────────────
//  Class     : LLMAdapterConfig
//  Namespace : AVA.UPS.Adapter.LLMAdapters
//  Purpose   : Flat configuration bag for ILLMAdapter instances.
//              UPSClientService translates LLMProfile + ModelProfile into
//              this type before handing to an adapter — keeps AVA.UPS.Adapter
//              free of any dependency on AVA.UI.CORE.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UPS.Adapter.LLMAdapters
{
    /// <summary>
    /// All configuration an ILLMAdapter needs to connect and send.
    /// Populated by UPSClientService from LLMProfile + ModelProfile.
    /// </summary>
    public sealed class LLMAdapterConfig
    {
        /// <summary>Unique identifier for this profile+model combination.</summary>
        public string ProfileId { get; set; } = string.Empty;

        /// <summary>Human-readable profile name (e.g. "My Claude Profile").</summary>
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>Resolved provider name (e.g. "Anthropic", "OpenAI", "DeepSeek").</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Base endpoint URL. Adapters append path suffixes as needed.</summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>API key or bearer token.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Custom headers as raw text (Key=Value, one per line).</summary>
        public string CustomHeadersAsText { get; set; } = string.Empty;

        /// <summary>Model identifier (e.g. "claude-sonnet-4-20250514", "gpt-4o").</summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>Human-readable model label.</summary>
        public string ModelLabel { get; set; } = string.Empty;

        /// <summary>Temperature — model override if set, else profile default.</summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>Max tokens — model override if set, else profile default.</summary>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>Optional system prompt for this model.</summary>
        public string? SystemPrompt { get; set; }
    }
}
