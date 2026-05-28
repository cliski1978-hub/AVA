namespace AVA.Memory.Core.Abstractions
{
    using AVA.Memory.Core.WorkingMemory;
    using System.Collections.Generic;

    public interface IWorkingMemoryStore
    {
        void Add(WorkingSetItem item);

        IEnumerable<WorkingSetItem> GetRecent(int n);

        IEnumerable<WorkingSetItem> GetContextWindow(int maxItems);

        void PurgeExpired();
    }
}
