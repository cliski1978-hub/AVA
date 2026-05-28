using System;
using System.Linq;
using AVA.Identity.Core.Services;
using AVA.Vault.Core.Data;
using AVA.Vault.Core.Data.Entities;

namespace AVA.Vault.Core.Identity
{
    /// <summary>
    /// Seeds the persistent module identity on first run only.
    /// </summary>
    public class ModuleIdentitySeeder
    {
        private readonly VaultDbContext _db;
        private readonly AvaIdGenerator _idGenerator;

        public ModuleIdentitySeeder(VaultDbContext db, AvaIdGenerator idGenerator)
        {
            _db = db;
            _idGenerator = idGenerator;
        }

        public void EnsureSeeded()
        {
            if (_db.ModuleIdentity.Any())
                return;

            var moduleId = _idGenerator.CreateModuleId();

            _db.ModuleIdentity.Add(new ModuleIdentity
            {
                ModuleAvaId = moduleId,
                RegisteredAtUtc = DateTime.UtcNow
            });

            _db.SaveChanges();
        }
    }
}
