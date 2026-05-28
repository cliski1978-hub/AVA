using AVA.UI.CORE.Models.Settings;

namespace AVA.UI.CORE.Services
{
    /// <summary>
    /// Normalizes settings into the separated provider/model architecture while preserving legacy connectivity.
    /// </summary>
    public static class SettingsArchitectureMigration
    {
        /// <summary>
        /// Ensures provider profiles and model definitions exist, seeding them from legacy LLM profiles when needed.
        /// </summary>
        public static void Normalize(AppSettings settings)
        {
            if (settings == null) return;

            SeedProviderProfiles(settings);
            SeedModelDefinitions(settings);
            MirrorProviderModelsForLegacyRuntime(settings);
        }

        private static void SeedProviderProfiles(AppSettings settings)
        {
            foreach (var legacy in settings.LLMProfiles)
            {
                var provider = settings.ProviderProfiles.FirstOrDefault(existing =>
                    existing.ProviderProfileId.Equals(legacy.ProfileId, StringComparison.OrdinalIgnoreCase));

                if (provider == null)
                {
                    provider = new ProviderProfile
                    {
                        ProviderProfileId = legacy.ProfileId,
                        Metadata = new Dictionary<string, string>
                        {
                            ["MigratedFrom"] = "LLMProfile"
                        }
                    };
                    settings.ProviderProfiles.Add(provider);
                }

                provider.Name = legacy.Name;
                provider.ProviderType = legacy.ModelType;
                provider.CustomProviderType = legacy.CustomModelType;
                provider.TransportType = legacy.EndpointType;
                provider.CustomTransportType = legacy.CustomEndpointType;
                provider.Endpoint = legacy.Endpoint;
                provider.ApiKey = legacy.ApiKey;
                provider.Secret = legacy.Secret;
                provider.CustomHeadersAsText = legacy.CustomHeadersAsText;
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

        private static void MirrorProviderModelsForLegacyRuntime(AppSettings settings)
        {
            foreach (var provider in settings.ProviderProfiles)
            {
                var legacy = settings.LLMProfiles.FirstOrDefault(profile =>
                    profile.ProfileId.Equals(provider.ProviderProfileId, StringComparison.OrdinalIgnoreCase));

                if (legacy == null)
                {
                    legacy = new LLMProfile
                    {
                        ProfileId = provider.ProviderProfileId,
                        Name = provider.Name,
                        IsActive = false,
                        IsDefault = settings.LLMProfiles.Count == 0
                    };
                    settings.LLMProfiles.Add(legacy);
                }

                legacy.Name = provider.Name;
                legacy.ModelType = provider.ProviderType;
                legacy.CustomModelType = provider.CustomProviderType;
                legacy.EndpointType = provider.TransportType;
                legacy.CustomEndpointType = provider.CustomTransportType;
                legacy.Endpoint = provider.Endpoint;
                legacy.ApiKey = provider.ApiKey;
                legacy.Secret = provider.Secret;
                legacy.CustomHeadersAsText = provider.CustomHeadersAsText;

                foreach (var definition in settings.ModelDefinitions.Where(model =>
                             model.ProviderProfileId.Equals(provider.ProviderProfileId, StringComparison.OrdinalIgnoreCase)))
                {
                    var model = legacy.Models.FirstOrDefault(existing =>
                        existing.Id.Equals(definition.ModelId, StringComparison.OrdinalIgnoreCase));

                    if (model == null)
                    {
                        model = new ModelProfile { Id = definition.ModelId };
                        legacy.Models.Add(model);
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
