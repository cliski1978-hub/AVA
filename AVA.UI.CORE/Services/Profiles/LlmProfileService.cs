using System.Text.Json;
using AVA.UI.CORE.Models.Settings;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Persistence;

namespace AVA.UI.CORE.Services.Profiles
{
    public class LlmProfileService
    {
        private readonly IProfilePersistenceProvider _persistence;
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };
        private static readonly char[] _separator = [';'];

        public LlmProfileService(IProfilePersistenceProvider persistence)
        {
            _persistence = persistence;
        }

        public async Task<List<ProviderProfile>> GetAllProviderProfilesAsync(CancellationToken ct = default)
        {
            var entities = await _persistence.GetAllProviderProfilesAsync(ct);
            return entities.Select(MapToProviderProfile).ToList();
        }

        public async Task<List<ProviderProfile>> GetActiveProviderProfilesAsync(CancellationToken ct = default)
        {
            var entities = await _persistence.GetActiveProviderProfilesAsync(ct);
            return entities.Select(MapToProviderProfile).ToList();
        }

        public async Task<ProviderProfile?> GetDefaultProviderProfileAsync(CancellationToken ct = default)
        {
            var entity = await _persistence.GetDefaultProviderProfileAsync(ct);
            return entity != null ? MapToProviderProfile(entity) : null;
        }

        public async Task<List<AVA.UI.CORE.Models.Settings.ModelDefinition>> GetModelsByProfileIdAsync(string profileId, CancellationToken ct = default)
        {
            var entities = await _persistence.GetModelsByProviderProfileIdAsync(profileId, ct);
            return entities.Select(MapToModelDefinition).ToList();
        }

        public async Task<ProviderProfile> SaveProviderProfileAsync(ProviderProfile profile, CancellationToken ct = default)
        {
            var entity = MapFromProviderProfile(profile);
            var saved = await _persistence.SaveProviderProfileAsync(entity, ct);
            return MapToProviderProfile(saved);
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
            return entities.Select(MapToLlmProfile).ToList();
        }

        public async Task<LLMProfile?> GetDefaultLlmProfileAsync(CancellationToken ct = default)
        {
            var entity = await _persistence.GetDefaultProviderProfileAsync(ct);
            return entity != null ? MapToLlmProfile(entity) : null;
        }

        #region Mapping: ProviderProfile

        private static ProviderProfile MapToProviderProfile(AvaProviderProfile entity)
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
                ApiKey = string.Empty,
                Secret = string.Empty,
                CustomHeadersAsText = entity.CustomHeadersAsText ?? string.Empty,
                TimeoutSeconds = entity.TimeoutSeconds,
                RetryCount = entity.RetryCount,
                SupportsStreaming = entity.SupportsStreaming,
                Metadata = DeserializeMetadata(entity.MetadataJson)
            };
        }

        private static AvaProviderProfile MapFromProviderProfile(ProviderProfile ui)
        {
            return new AvaProviderProfile
            {
                ProviderProfileId = string.IsNullOrWhiteSpace(ui.ProviderProfileId)
                    ? Guid.NewGuid().ToString()
                    : ui.ProviderProfileId,
                Name = ui.Name,
                ProviderType = ui.ProviderType,
                CustomProviderType = string.IsNullOrWhiteSpace(ui.CustomProviderType) ? null : ui.CustomProviderType,
                TransportType = ui.TransportType,
                CustomTransportType = string.IsNullOrWhiteSpace(ui.CustomTransportType) ? null : ui.CustomTransportType,
                BaseUrl = string.IsNullOrWhiteSpace(ui.Endpoint) ? null : ui.Endpoint,
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
                ModelDefinitionId = Guid.NewGuid().ToString(),
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

        #region Mapping: LLMProfile (legacy)

        private static LLMProfile MapToLlmProfile(AvaProviderProfile entity)
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
                ApiKey = string.Empty,
                Secret = string.Empty,
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

        #endregion
    }
}
