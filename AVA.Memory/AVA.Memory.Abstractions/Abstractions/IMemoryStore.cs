using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Abstractions
{
    /// <summary>
    /// Contract for a memory persistence provider.
    /// Works with DTO models and is portable across .NET Standard and .NET 8.
    /// </summary>
    public interface IMemoryStore
    {
        /// <summary>
        /// Inserts or updates a memory record and returns its ID.
        /// </summary>
        Task<string> UpsertAsync(MemoryRecordDto record, CancellationToken ct);

        /// <summary>
        /// Retrieves a record by ID.
        /// </summary>
        Task<MemoryRecordDto?> GetAsync(string id, CancellationToken ct);

        /// <summary>
        /// Deletes a record by ID and returns success flag.
        /// </summary>
        Task<bool> DeleteAsync(string id, CancellationToken ct);

        /// <summary>
        /// Returns a paginated list of records and the total count.
        /// </summary>
        Task<(IReadOnlyList<MemoryRecordDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct);
    }
}
