using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Models.Query;

namespace AVA.Vault.Core.Services.Interfaces
{
    /// <summary>
    /// Defines a contract for executing validated SQL queries directly against the Vault database.
    /// This adapter is responsible for enforcing safety checks, parameter binding, and returning structured results.
    /// </summary>
    public interface IVaultQueryAdapter
    {
        /// <summary>
        /// Executes a SQL query request safely and returns a standardized result.
        /// </summary>
        Task<VaultQueryResult> ExecuteAsync(VaultQueryRequest request, CancellationToken ct = default);

        /// <summary>
        /// Validates whether a given SQL string is permissible under Vault query rules.
        /// </summary>
        Task<bool> ValidateQueryAsync(string query, CancellationToken ct = default);

        /// <summary>
        /// Executes a parameterized SQL query and returns results as dictionaries (untyped).
        /// </summary>
        Task<List<Dictionary<string, object>>> ExecuteRawAsync(string sql, Dictionary<string, object>? parameters = null, CancellationToken ct = default);
    }
}
