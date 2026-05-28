using System.Collections.Generic;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Interfaces
{
    /// <summary>
    /// Minimal agent bridge adapter interface until promoted into CliskiCore.DbAPI.
    /// </summary>
    public interface IAgentBridgeAdapter
    {
        Task<string> StoreAgentArtifactAsync(string agentId, string type, string content, Dictionary<string, string>? metadata = null);
        Task<List<VaultNote>> QueryAgentArtifactsAsync(string agentId, string? type = null, string? tag = null);
        Task<string> LinkAgentArtifactsAsync(string sourceNoteId, string targetNoteId, string relationType);
        Task<bool> DeleteAgentArtifactAsync(string noteId);
    }
}
