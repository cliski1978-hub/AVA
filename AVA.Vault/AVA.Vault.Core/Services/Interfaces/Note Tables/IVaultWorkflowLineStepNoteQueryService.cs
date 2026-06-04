using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineStepNoteQueryService
    {
        Task<VaultWorkflowLineStepNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStepNote>> GetByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStepNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowLineStepId, string noteId, CancellationToken ct = default);
        Task<int> CountByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default);
    }
}
