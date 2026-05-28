using AVA.Memory.Sql.Config;
using System;

namespace AVA.Memory.Sql.Configuration
{
    public static class SqlMemoryConfigurator
    {
        private static readonly SqlMemoryOptions _options = new();

        public static void Configure(Action<SqlMemoryOptions> configure)
        {
            configure(_options);
        }

        public static SqlMemoryOptions GetOptions()
        {
            return _options;
        }
    }
}
