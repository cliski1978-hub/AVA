using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using AVA.Identity.Core.Data;
using AVA.Identity.Core.Models;
using AVA.Vault.Core.Identity;
using AVA.Vault.Core.Identity.Bootstrap;
using AVA.Identity.Core.Registry;

namespace AVA.Vault.Core.Identity.Resolution
{
    /// <summary>
    /// Resolves identities from the local IdentityDbContext.
    /// Used when Vault is running in Embedded or LocalDatabase identity mode.
    /// </summary>
    public sealed class LocalIdentityResolver : IIdentityResolver
    {
        private readonly IdentityDbContext _db;
        private readonly IdentityBootstrap _bootstrap;

        public LocalIdentityResolver(
            IdentityDbContext db,
            IdentityBootstrap bootstrap)
        {
            _db = db;
            _bootstrap = bootstrap;
        }

        // ---------------------------------------------------------------------
        // ResolveIdentityAsync
        // ---------------------------------------------------------------------
        /// <summary>
        /// Resolves an AvaId by querying the local IdentityDbContext.
        /// Returns null if the identity does not exist.
        /// </summary>
        public async Task<AvaIdentity?> ResolveIdentityAsync(string avaId)
        {
            if (string.IsNullOrWhiteSpace(avaId))
                return null;

            return await _db.AvaIdentities
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AvaId == avaId);
        }

        // ---------------------------------------------------------------------
        // ValidateIdentityAsync
        // ---------------------------------------------------------------------
        /// <summary>
        /// Validates an incoming identity stamp by looking up the identity
        /// and checking that the identity is active.
        /// </summary>
        public async Task<bool> ValidateIdentityAsync(IdentityStamp stamp)
        {
            if (stamp == null || string.IsNullOrWhiteSpace(stamp.PrimaryAvaId))
                return false;

            var identity = await ResolveIdentityAsync(stamp.PrimaryAvaId);
            if (identity == null)
                return false;

            return identity.Status == IdentityStatus.Active;
        }

        // ---------------------------------------------------------------------
        // GetCurrentIdentityStampAsync
        // ---------------------------------------------------------------------
        /// <summary>
        /// Returns the identity stamp for this Vault instance.
        /// This comes from the IdentityBootstrap, which seeds and retrieves
        /// the local module/node identity.
        /// </summary>
        public async Task<IdentityStamp> GetCurrentIdentityStampAsync()
        {
            var localIdentity = await _bootstrap.GetOrCreateLocalModuleIdentityAsync();

            return new IdentityStamp
            {
                PrimaryAvaId = localIdentity.AvaId,
                PrimaryDisplayName = localIdentity.DisplayName,
                PrimaryType = localIdentity.Type,
                DnaHash = localIdentity.DnaHash,
                TimestampUtc = DateTime.UtcNow
            };
        }
    }
}
