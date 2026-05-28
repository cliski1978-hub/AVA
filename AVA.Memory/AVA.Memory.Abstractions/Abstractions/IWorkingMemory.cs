using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Abstractions
{
    public interface IWorkingMemory
    {
        Task<IReadOnlyList<MemoryRecordDto>> GetItemsAsync(CancellationToken ct);
        Task AddOrRefreshAsync(MemoryRecordDto record, TimeSpan ttl, CancellationToken ct);
        Task FlushAsync(CancellationToken ct);

        // remove a single record by ID
        Task RemoveAsync(string id, CancellationToken ct);
    }
}
