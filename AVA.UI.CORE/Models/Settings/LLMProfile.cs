// ─────────────────────────────────────────────────────────────────────────────
//  Class     : LLMProfile
//  Namespace : AVA.UI.CORE.Models.Settings
//  Purpose   : Represents a single LLM connection profile.
//              Defines HOW to connect — endpoint, auth, protocol.
//              Contains a list of ModelProfiles defining WHO to talk to
//              within this LLM — characters, personalities, model variants.
// ─────────────────────────────────────────────────────────────────────────────

using System.Text.Json.Serialization;

namespace AVA.UI.CORE.Models.Settings
{
    /// <summary>
    /// Represents a single LLM connection profile.
    /// </summary>
    public class LLMProfile
    {
        /// <summary>
        /// Friendly display name shown in the UI.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Selected provider from dropdown: OpenAI, DeepSeek, Anthropic,
        /// HuggingFace, Ollama, vLLM, LmStudio, Venice, Custom
        /// </summary>
        public string ModelType { get; set; } = "OpenAI";

        /// <summary>
        /// Free text provider name when ModelType is Custom.
        /// </summary>
        public string CustomModelType { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint protocol: Http, WebSocket, Grpc, Custom
        /// </summary>
        public string EndpointType { get; set; } = "Http";

        /// <summary>
        /// Free text protocol when EndpointType is Custom.
        /// </summary>
        public string CustomEndpointType { get; set; } = string.Empty;

        /// <summary>
        /// The base endpoint URL for this LLM provider.
        /// All models within this profile share this endpoint.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Primary API key or bearer token for this endpoint.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Optional secondary secret or organisation key.
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Custom headers as raw text (Key=Value, one per line).
        /// Applied to all requests through this profile.
        /// </summary>
        public string CustomHeadersAsText { get; set; } = string.Empty;

        /// <summary>
        /// Default temperature for all models in this profile.
        /// Individual ModelProfiles can override this.
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Default max tokens for all models in this profile.
        /// Individual ModelProfiles can override this.
        /// </summary>
        public int MaxTokens { get; set; } = 512;

        /// <summary>
        /// When true this profile is selected by default on launch.
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// When true this profile is included in active chat sessions.
        /// Multiple profiles can be active simultaneously.
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// The models available within this LLM profile.
        /// For OpenAI: gpt-4, gpt-4o, o1, etc.
        /// For Venice: llama-3.3, mistral, etc.
        /// Saved profiles persist to settings.json.
        /// API-discovered models are merged at connect time.
        /// </summary>
        public List<ModelProfile> Models { get; set; } = new();

        /// <summary>
        /// Unique identifier for this profile.
        /// Used to associate sessions and model selections back to this profile.
        /// </summary>
        public string ProfileId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Resolved display name for provider.
        /// Returns CustomModelType when ModelType is Custom, otherwise ModelType.
        /// </summary>
        [JsonIgnore]
        public string ResolvedProvider =>
            ModelType == "Custom" && !string.IsNullOrWhiteSpace(CustomModelType)
                ? CustomModelType
                : ModelType;

        /// <summary>
        /// All models currently marked as active within this profile.
        /// These are the models that will open as sessions when connecting.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<ModelProfile> ActiveModels =>
            Models.Where(M => M.IsActive);

        /// <summary>
        /// Merges API-discovered models into the saved model list.
        /// Saved profiles win on conflict — existing Id is never overwritten.
        /// New discoveries are added with IsDiscovered = true, IsActive = false.
        /// </summary>
        public void MergeDiscoveredModels(IEnumerable<ModelProfile> Discovered)
        {
            foreach (var Model in Discovered)
            {
                var Existing = Models.FirstOrDefault(M =>
                    M.Id.Equals(Model.Id, StringComparison.OrdinalIgnoreCase));

                if (Existing == null)
                {
                    Model.IsDiscovered = true;
                    Model.IsActive = false;
                    Models.Add(Model);
                }
                // Saved profile wins — no overwrite
            }
        }
    }
}