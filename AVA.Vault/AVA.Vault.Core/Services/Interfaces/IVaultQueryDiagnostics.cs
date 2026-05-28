using System;
using System.Threading;
using System.Threading.Tasks;

namespace AVA.Vault.Core.Services.Interfaces
{
    /// <summary>
    /// Provides query-level performance tracing and diagnostic utilities.
    /// Used by all query services to log timings and detect performance regressions.
    /// </summary>
    public interface IVaultQueryDiagnostics
    {
        /// <summary>
        /// Executes a timed query operation and logs duration, row count, and label.
        /// </summary>
        Task<T> TraceAsync<T>(string label, Func<Task<T>> operation, CancellationToken ct = default);

        /// <summary>
        /// Records a manual diagnostic event, e.g., slow query warning.
        /// </summary>
        void Record(string label, string message, long elapsedMs);

        /// <summary>
        /// Optional: sets a threshold (in ms) above which queries are logged as warnings.
        /// </summary>
        int WarningThresholdMs { get; set; }
    }
}
