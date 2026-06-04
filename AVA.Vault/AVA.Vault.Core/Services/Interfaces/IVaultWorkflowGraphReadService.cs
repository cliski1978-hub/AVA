using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Workflows;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowGraphReadService
    {
        Task<VaultWorkflowGraphDto?> GetWorkflowGraphAsync(string workflowId, CancellationToken ct = default);
        Task<VaultWorkflowGraphNodeDto?> GetNodeGraphContextAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowGraphLineDto>> GetOutgoingNodeLinksAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowGraphLineDto>> GetIncomingNodeLinksAsync(string workflowNodeId, CancellationToken ct = default);
    }
}
