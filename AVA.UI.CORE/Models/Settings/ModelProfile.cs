// ─────────────────────────────────────────────────────────────────────────────
//  Class     : ModelProfile
//  Namespace : AVA.UI.CORE.Models.Settings
//  Purpose   : Represents a single model within an LLM profile.
//              Universal across all providers — what "model" means varies
//              by provider but the shape is always the same.
//
//              Examples:
//                OpenAI  — gpt-4, gpt-4o, o1, etc
//                Venice  — llama-3.3, mistral, etc
//                Ollama  — llama3, codellama, etc
//
//              Saved profiles persist to settings.json.
//              API-discovered models are merged at connect time.
//              If the same ID exists in both, the saved profile wins.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.Settings
{
    /// <summary>
    /// Represents a single selectable model within an LLM profile.
    /// </summary>
    public class ModelProfile
    {
        /// <summary>
        /// Provider-specific model identifier.
        /// For OpenAI: gpt-4, gpt-4o, o1-preview
        /// For Venice: llama-3.3-70b, mistral-31
        /// For Ollama: llama3, codellama, mistral
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Human readable display name shown in the UI.
        /// For other providers this is the model name.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Provider type hint — "room", "openai", "venice", "ollama", etc.
        /// Used for provider-specific rendering in the UI.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// When true this model is included in active sessions by default.
        /// User can toggle this in settings or the chat model picker.
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// When true this model was discovered via API and not yet saved
        /// to settings. Shown with a New badge in the UI.
        /// User can promote it to a saved profile with one click.
        /// </summary>
        public bool IsDiscovered { get; set; } = false;

        /// <summary>
        /// Optional provider-specific metadata.
        /// For OpenAI: context window, capabilities, etc
        /// Stored as key-value pairs for flexibility.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        /// Optional system prompt override for this specific model.
        /// When set this overrides any system prompt defined at the LLM profile level.
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Optional temperature override for this specific model.
        /// When null the LLM profile temperature is used.
        /// </summary>
        public double? Temperature { get; set; }

        /// <summary>
        /// Optional max tokens override for this specific model.
        /// When null the LLM profile max tokens is used.
        /// </summary>
        public int? MaxTokens { get; set; }

    }
}
