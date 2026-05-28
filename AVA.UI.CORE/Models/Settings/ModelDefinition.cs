namespace AVA.UI.CORE.Models.Settings
{
    /// <summary>
    /// Capability definition for one model exposed by a provider.
    /// </summary>
    public class ModelDefinition
    {
        /// <summary>Provider-specific model identifier.</summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>Provider profile that exposes this model.</summary>
        public string ProviderProfileId { get; set; } = string.Empty;

        /// <summary>Human-readable model label.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Provider-specific model type hint.</summary>
        public string ModelType { get; set; } = string.Empty;

        /// <summary>Whether this model was discovered from the provider rather than manually entered.</summary>
        public bool IsDiscovered { get; set; }

        /// <summary>Maximum context window supported by this model.</summary>
        public int ContextWindow { get; set; } = 8192;

        /// <summary>Maximum output tokens supported or normally allowed for this model.</summary>
        public int MaxOutputTokens { get; set; } = 1024;

        /// <summary>Maximum input characters supported by this model or provider. Zero means no explicit character limit is configured.</summary>
        public int MaxInputCharacters { get; set; }

        /// <summary>Optional default temperature for this model.</summary>
        public double? DefaultTemperature { get; set; }

        /// <summary>Whether this model supports tool calling.</summary>
        public bool SupportsToolCalls { get; set; }

        /// <summary>Whether this model supports vision inputs.</summary>
        public bool SupportsVision { get; set; }

        /// <summary>Whether this model supports reasoning controls or reasoning-oriented execution.</summary>
        public bool SupportsReasoning { get; set; }

        /// <summary>Whether the provider/model manages its own long-running conversation memory.</summary>
        public bool SupportsProviderMemory { get; set; }

        /// <summary>Optional system prompt default for this model.</summary>
        public string? SystemPrompt { get; set; }

        /// <summary>Model capability metadata and compatibility annotations.</summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
