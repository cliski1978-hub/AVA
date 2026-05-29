using System.Text.Json;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AVA.Vault.Core.Services.Secrets
{
    /// <summary>
    /// SQL-backed encrypted Vault secret store.
    /// </summary>
    public class DbVaultSecretStore : IVaultSecretStore
    {
        private const string EncryptionProviderName = "AspNetCoreDataProtection:v1";
        private const string ProtectorPurpose = "AVA.Vault.Secrets.v1";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly IDbContextFactory<VaultDbContext> _dbFactory;
        private readonly IDataProtector _protector;
        private readonly ILogger<DbVaultSecretStore> _logger;

        /// <summary>
        /// Initializes a new SQL-backed Vault secret store.
        /// </summary>
        public DbVaultSecretStore(
            IDbContextFactory<VaultDbContext> dbFactory,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<DbVaultSecretStore> logger)
        {
            _dbFactory = dbFactory;
            _protector = dataProtectionProvider.CreateProtector(ProtectorPurpose);
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> SaveSecretAsync(
            string secretRef,
            string secretName,
            string secretType,
            string secretValue,
            Dictionary<string, string>? metadata = null,
            CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(secretRef);
            ArgumentException.ThrowIfNullOrWhiteSpace(secretName);
            ArgumentException.ThrowIfNullOrWhiteSpace(secretType);
            ArgumentException.ThrowIfNullOrWhiteSpace(secretValue);

            await using var db = _dbFactory.CreateDbContext();
            var now = DateTime.UtcNow;
            var encryptedValue = _protector.Protect(secretValue);
            var metadataJson = SerializeMetadata(metadata);

            var existing = await db.AvaSecrets
                .FirstOrDefaultAsync(secret => secret.SecretRef == secretRef, ct)
                .ConfigureAwait(false);

            if (existing == null)
            {
                db.AvaSecrets.Add(new AvaSecret
                {
                    SecretId = Guid.NewGuid().ToString(),
                    SecretRef = secretRef,
                    SecretName = secretName,
                    SecretType = secretType,
                    EncryptedValue = encryptedValue,
                    EncryptionProvider = EncryptionProviderName,
                    MetadataJson = metadataJson,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
            else
            {
                existing.SecretName = secretName;
                existing.SecretType = secretType;
                existing.EncryptedValue = encryptedValue;
                existing.EncryptionProvider = EncryptionProviderName;
                existing.MetadataJson = metadataJson;
                existing.IsActive = true;
                existing.UpdatedAt = now;
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return secretRef;
        }

        /// <inheritdoc />
        public async Task<string?> GetSecretAsync(string secretRef, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(secretRef))
            {
                return null;
            }

            await using var db = _dbFactory.CreateDbContext();
            var secret = await db.AvaSecrets
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.SecretRef == secretRef && item.IsActive, ct)
                .ConfigureAwait(false);

            if (secret == null)
            {
                return null;
            }

            try
            {
                return _protector.Unprotect(secret.EncryptedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt Vault secret {SecretRef}", secretRef);
                return null;
            }
        }

        private static string? SerializeMetadata(Dictionary<string, string>? metadata)
        {
            if (metadata == null || metadata.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(metadata, JsonOptions);
        }
    }
}
