using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNoteDetailsReadService
    {
        Task<VaultNoteDetailsDto?> GetNoteDetailsAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteMetadataDto>> GetNoteMetadataAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteTagDto>> GetNoteTagsAsync(string noteId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetNoteFilesAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteRelationDto>> GetNoteRelationsAsync(string noteId, CancellationToken ct = default);
    }
}
