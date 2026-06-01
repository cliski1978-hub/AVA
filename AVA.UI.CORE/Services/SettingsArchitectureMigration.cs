using AVA.UI.CORE.Models.Settings;

namespace AVA.UI.CORE.Services
{
    /// <summary>
    /// Mirrors ProviderProfile/ModelDefinition data into LLMProfile/ModelProfile shapes
    /// for use by the session runtime and adapters.
    /// </summary>
    public static class SettingsArchitectureMigration
    {
        /// <summary>
        /// Mirrors canonical provider and model data into runtime shapes.
        /// </summary>
        public static void Normalize(AppSettings settings)
        {
            if (settings == null) return;

            MirrorProviderModelsForSessionRuntime(settings);
        }

        private static void MirrorProviderModelsForSessionRuntime(AppSettings settings)
        {
            foreach (var provider in settings.ProviderProfiles)
            {
                var runtimeProfile = settings.LLMProfiles.FirstOrDefault(profile =>
                    profile.ProfileId.Equals(provider.ProviderProfileId, StringComparison.OrdinalIgnoreCase));

                if (runtimeProfile == null)
                {
                    runtimeProfile = new LLMProfile
                    {
                        ProfileId = provider.ProviderProfileId,
                        Name = provider.Name,
                        IsActive = false,
                        IsDefault = settings.LLMProfiles.Count == 0
                    };
                    settings.LLMProfiles.Add(runtimeProfile);
                }

                runtimeProfile.Name = provider.Name;
                runtimeProfile.ModelType = provider.ProviderType;
                runtimeProfile.CustomModelType = provider.CustomProviderType;
                runtimeProfile.EndpointType = provider.TransportType;
                runtimeProfile.CustomEndpointType = provider.CustomTransportType;
                runtimeProfile.Endpoint = provider.Endpoint;
                runtimeProfile.ApiKey = provider.ApiKey;
                runtimeProfile.Secret = provider.Secret;
                runtimeProfile.CustomHeadersAsText = provider.CustomHeadersAsText;

                foreach (var definition in settings.ModelDefinitions.Where(model =>
                             model.ProviderProfileId.Equals(provider.ProviderProfileId, StringComparison.OrdinalIgnoreCase)))
                {
                    var model = runtimeProfile.Models.FirstOrDefault(existing =>
                        existing.Id.Equals(definition.ModelId, StringComparison.OrdinalIgnoreCase));

                    if (model == null)
                    {
                        model = new ModelProfile { Id = definition.ModelId };
                        runtimeProfile.Models.Add(model);
                    }

                    model.Label = definition.DisplayName;
                    model.Type = definition.ModelType;
                    model.IsDiscovered = definition.IsDiscovered;
                    model.SystemPrompt = definition.SystemPrompt;
                    model.Temperature = definition.DefaultTemperature;
                    model.MaxTokens = definition.MaxOutputTokens;
                    model.Metadata = new Dictionary<string, string>(definition.Metadata);
                    model.Metadata["MaxInputCharacters"] = Math.Max(0, definition.MaxInputCharacters).ToString();
                }
            }
        }
    }
}
