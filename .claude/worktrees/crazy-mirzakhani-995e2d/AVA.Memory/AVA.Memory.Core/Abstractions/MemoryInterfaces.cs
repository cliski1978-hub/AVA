namespace AVA.Memory.Core.Abstractions
{
    using AVA.Memory.Abstractions.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    

    public interface IVectorIndexStore
    {
        Task AddAsync(MemoryRecordDto record);
        Task<IReadOnlyList<QueryHit>> QueryAsync(QueryRequest request);
    }

   
}
