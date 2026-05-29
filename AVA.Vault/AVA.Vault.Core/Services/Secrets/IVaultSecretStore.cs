namespace AVA.Vault.Core.Services.Secrets
{
    /// <summary>
    /// Stores and retrieves encrypted Vault secrets by stable reference.
    /// </summary>
    public interface IVaultSecretStore
    {
        /// <summary>
        /// Creates or updates an encrypted secret and returns its stable reference.
        /// </summary>
        Task<string> SaveSecretAsync(
            string secretRef,
            string secretName,
            string secretType,
            string secretValue,
            Dictionary<string, string>? metadata = null,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves and decrypts a secret value by reference.
        /// </summary>
        Task<string?> GetSecretAsync(string secretRef, CancellationToken ct = default);
    }
}
