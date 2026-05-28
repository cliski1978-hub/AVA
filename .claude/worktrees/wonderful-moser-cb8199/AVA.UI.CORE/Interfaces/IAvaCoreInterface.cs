using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVA.UI.CORE.Interfaces
{
    public interface IAvaCoreInterface
    {
        Task<string> ProcessInputAsync(string prompt);
    }
}
