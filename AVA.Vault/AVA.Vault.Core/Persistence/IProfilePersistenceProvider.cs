using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Persistence
{
    public interface IProfilePersistenceProvider
    {
        Task<List<AvaProviderProfile>> GetAllProviderProfilesAsync(CancellationToken ct = default);
        Task<List<AvaProviderProfile>> GetActiveProviderProfilesAsync(CancellationToken ct = default);
        Task<AvaProviderProfile?> GetDefaultProviderProfileAsync(CancellationToken ct = default);
        Task<AvaProviderProfile?> GetProviderProfileByIdAsync(string id, CancellationToken ct = default);
        Task<AvaProviderProfile> SaveProviderProfileAsync(AvaProviderProfile profile, CancellationToken ct = default);
        Task DeleteProviderProfileAsync(string id, CancellationToken ct = default);

        Task<List<AvaModelDefinition>> GetModelsByProviderProfileIdAsync(string profileId, CancellationToken ct = default);
        Task<AvaModelDefinition?> GetModelDefinitionByIdAsync(string id, CancellationToken ct = default);
        Task<AvaModelDefinition> SaveModelDefinitionAsync(AvaModelDefinition model, CancellationToken ct = default);
        Task DeleteModelDefinitionAsync(string id, CancellationToken ct = default);
    }
}
