namespace AVA.Vault.Core.Interfaces
{
    /// <summary>
    /// ID generation abstraction for Vault Core entities.
    /// Allows future transition to AVA.Identity, IdentityStamp, or distributed
    /// identity systems without changing Vault Core storage or service logic.
    /// Temporary implementation uses GUIDs internally.
    /// </summary>
    public interface IVaultIdService
    {
        string NewId();
        string NewNoteId();
        string NewProjectId();
        string NewSessionId();
    }
}
