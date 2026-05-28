using AVA.Vault.Core.Interfaces;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Temporary GUID-based implementation of IVaultIdService.
    /// All ID generation in Vault Core routes through this service.
    /// Future implementations can integrate with AVA.Identity without
    /// changing any Vault Core storage or service logic.
    /// </summary>
    public class VaultIdService : IVaultIdService
    {
        public string NewId()        => Guid.NewGuid().ToString();
        public string NewNoteId()    => Guid.NewGuid().ToString();
        public string NewProjectId() => Guid.NewGuid().ToString();
        public string NewSessionId() => Guid.NewGuid().ToString();
    }
}
