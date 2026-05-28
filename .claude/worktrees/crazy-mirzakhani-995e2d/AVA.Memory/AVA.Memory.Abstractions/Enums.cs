using System;
using System.Collections.Generic;
using System.Text;

namespace AVA.Memory.Abstractions
{
    [Flags]
    public enum StorageTargets
    {
        None = 0,
        Sql = 1 << 0,
        Vector = 1 << 1,
        // future: File = 1 << 2,
        All = Sql | Vector
    }
}
