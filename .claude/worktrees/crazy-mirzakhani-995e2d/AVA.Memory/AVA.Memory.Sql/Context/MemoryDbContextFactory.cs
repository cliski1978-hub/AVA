using AVA.Memory.Sql.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AVA.Memory.Sql.Context
{
    /// <summary>
    /// Design-time factory so EF Core tools can create MemoryDbContext
    /// when running migrations from Package Manager Console or CLI.
    /// </summary>
    public class MemoryDbContextFactory : IDesignTimeDbContextFactory<MemoryDbContext>
    {
        public MemoryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MemoryDbContext>();

            // 👇 Use SqlServer by default; you can change this or read from config if needed.
            optionsBuilder.UseSqlServer(
                "Server=4D-C76\\SQLEXPRESS;Database=AvaMemory;Trusted_Connection=True;TrustServerCertificate=True;");

            return new MemoryDbContext(optionsBuilder.Options);
        }
    }
}
