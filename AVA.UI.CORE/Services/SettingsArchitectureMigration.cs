using AVA.UI.CORE.Models.Settings;

namespace AVA.UI.CORE.Services
{
    /// <summary>
    /// Normalizes settings into the separated provider/model architecture while preserving session connectivity.
    /// </summary>
    public static class SettingsArchitectureMigration
    {
        /// <summary>
        /// Ensures provider profiles and model definitions exist, seeding them from existing session profiles when needed.
        /// </summary>
        public static void Normalize(AppSettings settings)
        {
            if (settings == null) return;

            SeedProviderProfiles(settings);
            SeedModelDefinitions(settings);
            MirrorProviderModelsForSessionRuntime(settings);
        }

        private static void SeedProviderProfiles(AppSettings settings)
        {
            foreach (var sessionProfile in settings.LLMProfiles)
            {
                var provider = settings.ProviderProfiles.FirstOrDefault(existing =>
                    existing.ProviderProfileId.Equals(sessionProfile.ProfileId, StringComparison.OrdinalIgnoreCase));

                if (provider == null)
                {
                    provider = new ProviderProfile
                    {
                        ProviderProfileId = sessionProfile.ProfileId,
                        Metadata = new Dictionary<string, string>
                        {
                            ["SeededFrom"] = "LLMProfile"
                        }
                    };
                    settings.ProviderProfiles.Add(provider);

                    provider.Name = sessionProfile.Name;
                    provider.ProviderType = sessionProfile.ModelType;
                    provider.CustomProviderType = sessionProfile.CustomModelType;
                    provider.TransportType = sessionProfile.EndpointType;
                    provider.CustomTransportType = sessionProfile.CustomEndpointType;
                    provider.Endpoint = sessionProfile.Endpoint;
                    provider.ApiKey = sessionProfile.ApiKey;
                    provider.Secret = sessionProfile.Secret;
                    provider.CustomHeadersAsText = sessionProfile.CustomHeadersAsText;
                }
            }
        }

        private static void SeedModelDefinitions(AppSettings settings)
        {
            foreach (var profile in settings.LLMProfiles)
            {
                foreach (var model in profile.Models)
                {
                    var existing = settings.ModelDefinitions.FirstOrDefault(definition =>
                            definition.ModelId.Equals(model.Id, StringComparison.OrdinalIgnoreCase) &&
                            definition.ProviderProfileId.Equals(profile.ProfileId, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        existing.DisplayName = model.Label;
                        existing.ModelType = model.Type;
                        existing.IsDiscovered = model.IsDiscovered;
                        existing.SystemPrompt = model.SystemPrompt;
                        existing.MaxInputCharacters = ReadInt(model.Metadata, "MaxInputCharacters", existing.MaxInputCharacters);
                        continue;
                    }

                    settings.ModelDefinitions.Add(new ModelDefinition
                    {
                        ModelId = model.Id,
                        ProviderProfileId = profile.ProfileId,
                        DisplayName = model.Label,
                        ModelType = model.Type,
                        IsDiscovered = model.IsDiscovered,
                        MaxOutputTokens = Math.Max(1, model.MaxTokens ?? profile.MaxTokens),
                        MaxInputCharacters = ReadInt(model.Metadata, "MaxInputCharacters", 0),
                        DefaultTemperature = model.Temperature ?? profile.Temperature,
                        SupportsProviderMemory = false, //ATT CLIFFF IF WE FIND A PROVIDER WITH BUILT IN MEMORY THIS NEEDS TO CHANGE
                        SystemPrompt = model.SystemPrompt,
                        Metadata = new Dictionary<string, string>(model.Metadata)
                    });
                }
            }
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

        private static int ReadInt(Dictionary<string, string> metadata, string key, int fallback)
        {
            return metadata != null &&
                   metadata.TryGetValue(key, out var raw) &&
                   int.TryParse(raw, out var value)
                ? Math.Max(0, value)
                : fallback;
        }
    }
}
