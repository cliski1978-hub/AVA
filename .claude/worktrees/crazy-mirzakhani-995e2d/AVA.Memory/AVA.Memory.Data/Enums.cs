using System;
using System.Collections.Generic;
using System.Text;

namespace AVA.Memory.Data
{
    /// <summary>
    /// Defines importance and persistence rules for working memory items.
    /// </summary>
    public enum WorkingSetPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
}
