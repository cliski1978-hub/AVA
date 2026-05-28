using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVA.UI.CORE.Interfaces
{

    public interface IMemoryTracer
    {
        void Append(string message);
        void Clear();
        void Enable();
        void Disable();
    }
}
