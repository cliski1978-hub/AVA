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

            optionsBuilder.UseSqlServer("Server=WITMNCND3112P9Q;Database=AvaVault;User ID=MECRAPPS;Password=!Qazxsw2;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True;");

            return new VaultDbContext(optionsBuilder.Options);
        }
    }
}
