using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using AVA.UI.CORE.Models.Settings;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Persistence;
using AVA.Vault.Core.Services.Secrets;

namespace AVA.UI.CORE.Services.Profiles
{
    public class LlmProfileService
    {
        private const string SecretRefPrefix = "ava-secret://";

        private readonly IProfilePersistenceProvider _persistence;
        private readonly IVaultSecretStore _secretStore;
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
        private static readonly char[] _separator = [';'];

        public LlmProfileService(IProfilePersistenceProvider persistence, IVaultSecretStore secretStore)
        {
            _persistence = persistence;
            _secretStore = secretStore;
        }

        public async Task<List<ProviderProfile>> GetAllProviderProfilesAsync(CancellationToken ct = default)
        {
            var entities = await _persistence.GetAllProviderProfilesAsync(ct);
            var profiles = new List<ProviderProfile>();
            foreach (var entity in entities)
            {
                profiles.Add(await MapToProviderProfileAsync(entity, ct).ConfigureAwait(false));
            }
            return profiles;
        }

        public async Task<List<ProviderProfile>> GetActiveProviderProfilesAsync(CancellationToken ct = default)
        {
            var entities = await _persistence.GetActiveProviderProfilesAsync(ct);
            var profiles = new List<ProviderProfile>();
            foreach (var entity in entities)
            {
                profiles.Add(await MapToProviderProfileAsync(entity, ct).ConfigureAwait(false));
            }
            return profiles;
        }

        public async Task<ProviderProfile?> GetDefaultProviderProfileAsync(CancellationToken ct = default)
        {
            var entity = await _persistence.GetDefaultProviderProfileAsync(ct);
            return entity != null ? await MapToProviderProfileAsync(entity, ct).ConfigureAwait(false) : null;
        }

        public async Task<List<AVA.UI.CORE.Models.Settings.ModelDefinition>> GetModelsByProfileIdAsync(string profileId, CancellationToken ct = default)
        {
            var entities = await _persistence.GetModelsByProviderProfileIdAsync(profileId, ct);
            return entities.Select(MapToModelDefinition).ToList();
        }

        public async Task<ProviderProfile> SaveProviderProfileAsync(ProviderProfile profile, CancellationToken ct = default)
        {
            var entity = await MapFromProviderProfileAsync(profile, ct).ConfigureAwait(false);
            var saved = await _persistence.SaveProviderProfileAsync(entity, ct);
            return await MapToProviderProfileAsync(saved, ct).ConfigureAwait(false);
        }

        public async Task DeleteProviderProfileAsync(string id, CancellationToken ct = default)
        {
            await _persistence.DeleteProviderProfileAsync(id, ct);
        }

        public async Task<AVA.UI.CORE.Models.Settings.ModelDefinition> SaveModelDefinitionAsync(
            AVA.UI.CORE.Models.Settings.ModelDefinition model, CancellationToken ct = default)
        {
            var entity = MapFromModelDefinition(model);
            var saved = await _persistence.SaveModelDefinitionAsync(entity, ct);
            return MapToModelDefinition(saved);
        }

        public async Task DeleteModelDefinitionAsync(string id, CancellationToken ct = default)
        {
            await _persistence.DeleteModelDefinitionAsync(id, ct);
        }

        public async Task<List<LLMProfile>> GetAllLlmProfilesAsync(CancellationToken ct = default)
        {
            var entities = await _persistence.GetAllProviderProfilesAsync(ct);
            var profiles = new List<LLMProfile>();
            foreach (var entity in entities)
            {
                profiles.Add(await MapToLlmProfileAsync(entity, ct).ConfigureAwait(false));
            }
            return profiles;
        }

        public async Task<LLMProfile?> GetDefaultLlmProfileAsync(CancellationToken ct = default)
        {
            var entity = await _persistence.GetDefaultProviderProfileAsync(ct);
            return entity != null ? await MapToLlmProfileAsync(entity, ct).ConfigureAwait(false) : null;
        }

        #region Mapping: ProviderProfile

        private async Task<ProviderProfile> MapToProviderProfileAsync(AvaProviderProfile entity, CancellationToken ct)
        {
            return new ProviderProfile
            {
                ProviderProfileId = entity.ProviderProfileId,
                Name = entity.Name,
                ProviderType = entity.ProviderType,
                CustomProviderType = entity.CustomProviderType ?? string.Empty,
                TransportType = entity.TransportType,
                CustomTransportType = entity.CustomTransportType ?? string.Empty,
                Endpoint = entity.BaseUrl ?? string.Empty,
                ApiKey = await ResolveSecretValueAsync(entity.ApiKeySecretRef, ct).ConfigureAwait(false),
                Secret = await ResolveSecretValueAsync(entity.SecondarySecretRef, ct).ConfigureAwait(false),
                CustomHeadersAsText = entity.CustomHeadersAsText ?? string.Empty,
                TimeoutSeconds = entity.TimeoutSeconds,
                RetryCount = entity.RetryCount,
                SupportsStreaming = entity.SupportsStreaming,
                Metadata = DeserializeMetadata(entity.MetadataJson)
            };
        }

        private async Task<AvaProviderProfile> MapFromProviderProfileAsync(ProviderProfile ui, CancellationToken ct)
        {
            var providerProfileId = string.IsNullOrWhiteSpace(ui.ProviderProfileId)
                ? Guid.NewGuid().ToString()
                : ui.ProviderProfileId;

            return new AvaProviderProfile
            {
                ProviderProfileId = providerProfileId,
                Name = ui.Name,
                ProviderType = ui.ProviderType,
                CustomProviderType = string.IsNullOrWhiteSpace(ui.CustomProviderType) ? null : ui.CustomProviderType,
                TransportType = ui.TransportType,
                CustomTransportType = string.IsNullOrWhiteSpace(ui.CustomTransportType) ? null : ui.CustomTransportType,
                BaseUrl = string.IsNullOrWhiteSpace(ui.Endpoint) ? null : ui.Endpoint,
                ApiKeySecretRef = await SaveProviderSecretIfPresentAsync(
                    providerProfileId,
                    "api-key",
                    "ProviderApiKey",
                    "API key",
                    ui.ApiKey,
                    ct).ConfigureAwait(false),
                SecondarySecretRef = await SaveProviderSecretIfPresentAsync(
                    providerProfileId,
                    "secondary",
                    "ProviderSecondarySecret",
                    "secondary secret",
                    ui.Secret,
                    ct).ConfigureAwait(false),
                CustomHeadersAsText = string.IsNullOrWhiteSpace(ui.CustomHeadersAsText) ? null : ui.CustomHeadersAsText,
                TimeoutSeconds = ui.TimeoutSeconds,
                RetryCount = ui.RetryCount,
                SupportsStreaming = ui.SupportsStreaming,
                MetadataJson = SerializeMetadata(ui.Metadata),
                IsEnabled = true,
                IsActive = true
            };
        }

        #endregion

        #region Mapping: ModelDefinition

        private static AVA.UI.CORE.Models.Settings.ModelDefinition MapToModelDefinition(AvaModelDefinition entity)
        {
            return new AVA.UI.CORE.Models.Settings.ModelDefinition
            {
                ModelId = entity.ModelId,
                ProviderProfileId = entity.ProviderProfileId,
                DisplayName = entity.DisplayName,
                ModelType = entity.ModelType,
                IsDiscovered = entity.IsDiscovered,
                ContextWindow = entity.ContextWindowTokens ?? 8192,
                MaxOutputTokens = entity.MaxOutputTokens ?? 1024,
                MaxInputCharacters = entity.MaxInputCharacters ?? 0,
                DefaultTemperature = entity.DefaultTemperature,
                SupportsToolCalls = entity.SupportsTools,
                SupportsVision = entity.SupportsVision,
                SupportsReasoning = entity.SupportsReasoning,
                SupportsProviderMemory = entity.SupportsProviderMemory,
                SystemPrompt = entity.SystemPrompt,
                Metadata = DeserializeMetadata(entity.MetadataJson)
            };
        }

        private static AvaModelDefinition MapFromModelDefinition(AVA.UI.CORE.Models.Settings.ModelDefinition ui)
        {
            return new AvaModelDefinition
            {
                ModelDefinitionId = BuildModelDefinitionId(ui.ProviderProfileId, ui.ModelId),
                ModelId = ui.ModelId,
                ProviderProfileId = ui.ProviderProfileId,
                DisplayName = ui.DisplayName,
                ModelType = ui.ModelType,
                IsDiscovered = ui.IsDiscovered,
                ContextWindowTokens = ui.ContextWindow > 0 ? ui.ContextWindow : null,
                MaxOutputTokens = ui.MaxOutputTokens > 0 ? ui.MaxOutputTokens : null,
                MaxInputCharacters = ui.MaxInputCharacters > 0 ? ui.MaxInputCharacters : null,
                DefaultTemperature = ui.DefaultTemperature,
                SupportsTools = ui.SupportsToolCalls,
                SupportsVision = ui.SupportsVision,
                SupportsReasoning = ui.SupportsReasoning,
                SupportsProviderMemory = ui.SupportsProviderMemory,
                SystemPrompt = ui.SystemPrompt,
                MetadataJson = SerializeMetadata(ui.Metadata),
                IsActive = true,
                IsEnabled = true
            };
        }

        #endregion

        #region Mapping: LLMProfile runtime

        private async Task<LLMProfile> MapToLlmProfileAsync(AvaProviderProfile entity, CancellationToken ct)
        {
            return new LLMProfile
            {
                Name = entity.Name,
                ProfileId = entity.ProviderProfileId,
                ModelType = entity.ProviderType,
                CustomModelType = entity.CustomProviderType ?? string.Empty,
                EndpointType = entity.TransportType,
                CustomEndpointType = entity.CustomTransportType ?? string.Empty,
                Endpoint = entity.BaseUrl ?? string.Empty,
                ApiKey = await ResolveSecretValueAsync(entity.ApiKeySecretRef, ct).ConfigureAwait(false),
                Secret = await ResolveSecretValueAsync(entity.SecondarySecretRef, ct).ConfigureAwait(false),
                CustomHeadersAsText = entity.CustomHeadersAsText ?? string.Empty,
                Temperature = entity.Temperature ?? 0.7,
                MaxTokens = entity.MaxTokens ?? 512,
                IsDefault = entity.IsDefault,
                IsActive = entity.IsActive,
                Models = (entity.ModelDefinitions ?? new List<AvaModelDefinition>())
                    .Select(MapToModelProfile)
                    .ToList()
            };
        }

        private static ModelProfile MapToModelProfile(AvaModelDefinition entity)
        {
            return new ModelProfile
            {
                Id = entity.ModelId,
                Label = entity.DisplayName,
                Type = entity.ModelType,
                IsActive = entity.IsActive,
                IsDiscovered = entity.IsDiscovered,
                SystemPrompt = entity.SystemPrompt,
                Temperature = entity.DefaultTemperature,
                MaxTokens = entity.MaxOutputTokens,
                Metadata = DeserializeMetadata(entity.MetadataJson)
            };
        }

        #endregion

        #region Helpers

        private static Dictionary<string, string> DeserializeMetadata(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, string>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json, _jsonOpts)
                    ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private static string? SerializeMetadata(Dictionary<string, string>? metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return null;

            try
            {
                return JsonSerializer.Serialize(metadata, _jsonOpts);
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> SaveProviderSecretIfPresentAsync(
            string providerProfileId,
            string secretSlot,
            string secretType,
            string secretDescription,
            string? secretValue,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(secretValue))
            {
                return null;
            }

            if (IsSecretRef(secretValue))
            {
                return secretValue;
            }

            var secretRef = BuildProviderSecretRef(providerProfileId, secretSlot);
            var secretName = $"{providerProfileId} {secretDescription}";
            return await _secretStore.SaveSecretAsync(
                secretRef,
                secretName,
                secretType,
                secretValue,
                new Dictionary<string, string>
                {
                    ["ProviderProfileId"] = providerProfileId,
                    ["SecretSlot"] = secretSlot
                },
                ct).ConfigureAwait(false);
        }

        private async Task<string> ResolveSecretValueAsync(string? secretRefOrPlainValue, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(secretRefOrPlainValue))
            {
                return string.Empty;
            }

            if (!IsSecretRef(secretRefOrPlainValue))
            {
                return secretRefOrPlainValue;
            }

            var decryptedValue = await _secretStore.GetSecretAsync(secretRefOrPlainValue, ct)
    .ConfigureAwait(false);

            return decryptedValue ?? string.Empty;
        }

        private static string BuildProviderSecretRef(string providerProfileId, string secretSlot)
            => $"{SecretRefPrefix}provider/{providerProfileId}/{secretSlot}";

        private static bool IsSecretRef(string value)
            => value.StartsWith(SecretRefPrefix, StringComparison.OrdinalIgnoreCase);

        private static string BuildModelDefinitionId(string providerProfileId, string modelId)
        {
            var key = $"{providerProfileId}:{modelId}".Trim();
            if (key.Length <= 120 && key.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' or ':' or '.'))
            {
                return key;
            }

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"model-{hash}";
        }

        #endregion
    }
}
