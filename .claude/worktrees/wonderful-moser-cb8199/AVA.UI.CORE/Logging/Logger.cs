using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVA.UI.CORE.Logging
{
    public static class Logger
    {
        public static void LogEvent(string message)
        {
            // In production, could log to file or system; for now just debug.
            System.Diagnostics.Debug.WriteLine($"[AVA] {message}");
        }
    }
}
