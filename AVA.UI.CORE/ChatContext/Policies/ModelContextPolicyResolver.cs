using AVA.UI.CORE.ChatContext.Interfaces;
using AVA.UI.CORE.ChatContext.Models;
using AVA.UI.CORE.Models.Settings;
using AVA.UI.CORE.Services;

namespace AVA.UI.CORE.ChatContext.Policies
{
    /// <summary>
    /// Deterministic resolver for model-level ChatContext prompt construction policies.
    /// Searches separated model definitions by model ID and maps model capabilities to ModelContextSettings.
    /// Always returns fully populated settings — never null.
    /// </summary>
    public class ModelContextPolicyResolver : IModelContextPolicyResolver
    {
        private readonly AvaSettingsService _settings;

        public ModelContextPolicyResolver(AvaSettingsService settings)
        {
            _settings = settings;
        }

        public ModelContextSettings Resolve(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return Defaults(string.Empty);

            var model = _settings.AppSettings.ModelDefinitions.FirstOrDefault(m =>
                m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));

            if (model != null)
                return FromModel(model);

            return Defaults(modelId);
        }

        // ── Mapping ───────────────────────────────────────────────────────────

        private static ModelContextSettings FromModel(ModelDefinition model) => new()
        {
            ModelId                   = model.ModelId,
            ContextWindow             = model.ContextWindow,
            DefaultOutputReserve      = model.MaxOutputTokens,
            SupportsInternalMemory    = model.SupportsProviderMemory
        };

        /// <summary>Safe defaults used when a model cannot be located.</summary>
        private static ModelContextSettings Defaults(string modelId) => new()
        {
            ModelId = modelId
            // All other defaults are defined on ModelContextSettings properties.
        };
    }
}
