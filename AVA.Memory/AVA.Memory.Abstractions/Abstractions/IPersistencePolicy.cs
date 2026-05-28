using AVA.Memory.Abstractions.Models;
using AVA.Memory.Abstractions.Contracts;


namespace AVA.Memory.Abstractions
{
    public interface IPersistencePolicy
    {
        // Decide storage targets for a given record + request (when no explicit override)
        StorageTargets DecideTargets(MemoryRecordDto record, UpsertMemoryRequest req, MemoryPersistenceOptions opts);
    }
}
