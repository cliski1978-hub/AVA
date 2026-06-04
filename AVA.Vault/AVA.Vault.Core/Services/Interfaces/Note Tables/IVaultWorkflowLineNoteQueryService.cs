using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineNoteQueryService
    {
        Task<VaultWorkflowLineNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowLineNote>> GetByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowLineId, string noteId, CancellationToken ct = default);
        Task<int> CountByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default);
    }
}
