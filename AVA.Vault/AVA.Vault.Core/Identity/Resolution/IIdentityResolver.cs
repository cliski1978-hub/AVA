using System.Threading.Tasks;
using AVA.Identity.Core.Models;
using AVA.Identity.Core.Registry;

namespace AVA.Vault.Core.Identity
{
    /// <summary>
    /// Resolves, validates, and retrieves identity information for Vault.
    /// Implementations:
    ///   - LocalIdentityResolver (IdentityDbContext-backed)
    ///   - RemoteIdentityResolver (remote HTTP/UPS-backed)
    /// </summary>
    public interface IIdentityResolver
    {
        /// <summary>
        /// Resolve an AvaId into a full AvaIdentity record.
        /// Returns null if not found.
        /// </summary>
        Task<AvaIdentity?> ResolveIdentityAsync(string avaId);

        /// <summary>
        /// Validate an identity stamp (fast check).
        /// Returns true if identity is valid and active.
        /// </summary>
        Task<bool> ValidateIdentityAsync(IdentityStamp stamp);

        /// <summary>
        /// Returns the identity of this Vault node/module.
        /// Used for identity stamping on outbound writes.
        /// </summary>
        Task<IdentityStamp> GetCurrentIdentityStampAsync();
    }
}
