// Data/Entities/VaultDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AVA.Vault.Core.Data.Entities
{
    public class VaultDbContextFactory : IDesignTimeDbContextFactory<VaultDbContext>
    {
        public VaultDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<VaultDbContext>();

            optionsBuilder.UseSqlServer("Server=4D-C76\\SQLEXPRESS;Database=AvaVault;Integrated Security=True;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;");

            return new VaultDbContext(optionsBuilder.Options);
        }
    }
}
