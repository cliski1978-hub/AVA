using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNoteQueryService
    {
        Task<VaultWorkflowNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowNote>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
        Task<List<VaultWorkflowNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowId, string noteId, CancellationToken ct = default);
        Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    }
}
