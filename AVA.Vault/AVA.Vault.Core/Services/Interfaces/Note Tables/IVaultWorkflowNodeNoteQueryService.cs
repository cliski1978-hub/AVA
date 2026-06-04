using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNodeNoteQueryService
    {
        Task<VaultWorkflowNodeNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowNodeNote>> GetByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowNodeNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowNodeId, string noteId, CancellationToken ct = default);
        Task<int> CountByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default);
    }
}
