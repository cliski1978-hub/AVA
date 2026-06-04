using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Models.Query;

namespace AVA.Vault.Core.Services.Interfaces
{
    /// <summary>
    /// Provides a set of high-performance, parameterized SQL operations for reading Vault data.
    /// Used internally by services and adapters for bulk or time-sensitive retrieval.
    /// </summary>
    public interface IVaultSqlQueryHelper
    {
        Task<List<VaultNote>> GetRecentNotesAsync(string vaultId, DateTime since, int limit = 500, CancellationToken ct = default);
        Task<List<VaultTag>> GetTagsByVaultAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultNoteRelation>> GetRelationsForNoteAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultProject>> GetProjectsAsync(CancellationToken ct = default);
        Task<List<VaultGraph>> GetGraphSegmentAsync(string rootId, CancellationToken ct = default);

        /// <summary>
        /// Executes a predefined named query from registry (if available) with parameters.
        /// </summary>
        Task<VaultQueryResult> ExecuteNamedQueryAsync(string name, Dictionary<string, object>? parameters = null, CancellationToken ct = default);
    }
}
