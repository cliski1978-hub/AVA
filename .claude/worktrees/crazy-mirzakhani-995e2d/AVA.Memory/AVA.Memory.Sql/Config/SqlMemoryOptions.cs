namespace AVA.Memory.Sql.Config
{
    public class SqlMemoryOptions
    {
        public string? ConnectionString { get; set; }
        public bool UseSqlite { get; set; } = false;

        // Easy check for API/WPF bootstrap
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
    }
}
