using System.Collections.Generic;

namespace AVA.UI.CORE.Interfaces
{
    public interface IPluginManager
    {
        IEnumerable<string> GetActivePlugins();
        void RefreshPluginStates();
    }
}