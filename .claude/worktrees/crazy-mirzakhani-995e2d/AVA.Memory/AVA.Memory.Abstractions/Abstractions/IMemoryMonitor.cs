using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Abstractions
{
    /// <summary>
    /// Provides live statistics and controls for monitoring AVA's memory system.
    /// Used by background services and dashboards.
    /// </summary>
    public interface IMemoryMonitor
    {
        /// <summary>
        /// Retrieves the latest snapshot of working and persistent memory statistics.
        /// </summary>
        Task<MemoryStatsDto> GetStatsAsync(CancellationToken ct);

        /// <summary>
        /// Enables real-time memory stats streaming.
        /// </summary>
        void EnableStreaming();

        /// <summary>
        /// Disables real-time memory stats streaming.
        /// </summary>
        void DisableStreaming();

        /// <summary>
        /// Whether streaming is currently enabled.
        /// </summary>
        bool IsStreaming { get; }
    }
}
