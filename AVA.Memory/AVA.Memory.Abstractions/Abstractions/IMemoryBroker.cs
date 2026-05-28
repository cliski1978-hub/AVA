using AVA.Memory.Abstractions.Models;
using AVA.Memory.Abstractions.Contracts;

namespace AVA.Memory.Abstractions
{
    /// <summary>
    /// Defines high-level memory orchestration operations across stores, associations, and working memory.
    /// </summary>
    public interface IMemoryBroker
    {
        // Core record operations
        Task<string> UpsertAsync(UpsertMemoryRequest request, CancellationToken ct);
        Task<MemoryRecordDto?> GetAsync(string id, bool bumpAccess, CancellationToken ct);
        Task<bool> DeleteAsync(string id, CancellationToken ct);
        Task<(IReadOnlyList<MemoryRecordDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct);

        // Semantic vector queries
        Task<IReadOnlyList<(MemoryRecordDto Record, float Score)>> SemanticQueryAsync(QueryMemoryRequest request, CancellationToken ct);

        // Association management
        Task<string> UpsertEdgeAsync(AssociationEdgeDto edge, CancellationToken ct);
        Task<AssociationEdgeDto?> GetEdgeAsync(string id, CancellationToken ct);
        Task<bool> DeleteEdgeAsync(string id, CancellationToken ct);
        Task<IReadOnlyList<AssociationEdgeDto>> ListEdgesAsync(int skip, int take, CancellationToken ct);

        // Working memory control
        Task<IReadOnlyList<MemoryRecordDto>> GetWorkingAsync(CancellationToken ct);
        Task FlushWorkingAsync(CancellationToken ct);
    }
}
