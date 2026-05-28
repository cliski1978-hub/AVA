using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using AVA.Identity.Core.Models;
using AVA.Identity.Core.Registry;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Identity.Resolution
{
    /// <summary>
    /// Remote implementation of IIdentityResolver.
    /// Communicates with a remote Identity Service to:
    ///   - Fetch the current identity stamp for this Vault
    ///   - Resolve arbitrary identities by AvaId
    ///   - Validate a provided identity stamp
    /// </summary>
    public class RemoteIdentityResolver : IIdentityResolver
    {
        private readonly HttpClient _http;
        private readonly VaultInstanceConfig _config;
        private readonly ILogger<RemoteIdentityResolver> _logger;

        public RemoteIdentityResolver(
            HttpClient http,
            VaultInstanceConfig config,
            ILogger<RemoteIdentityResolver> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        // ---------------------------------------------------------------------
        // 1. Get current identity stamp for this Vault instance
        // ---------------------------------------------------------------------
        public async Task<IdentityStamp> GetCurrentIdentityStampAsync()
        {
            string url = BuildUrl("/api/identity/stamp");

            _logger.LogInformation("RemoteIdentityResolver requesting current identity stamp from {Url}", url);

            var stamp = await SafeGetAsync<IdentityStamp>(url);

            LogStamp("Received current identity stamp", stamp);

            return stamp;
        }

        // ---------------------------------------------------------------------
        // 2. Resolve identity by AvaId (returns AvaIdentity?)
        // ---------------------------------------------------------------------
        public async Task<AvaIdentity?> ResolveIdentityAsync(string avaId)
        {
            if (string.IsNullOrWhiteSpace(avaId))
                throw new ArgumentException("AvaId cannot be null or empty.", nameof(avaId));

            string url = BuildUrl($"/api/identity/resolve/{avaId}");

            _logger.LogInformation("Resolving identity for {AvaId} via {Url}", avaId, url);

            // Expect the remote identity service to return an AvaIdentity JSON payload.
            var identity = await SafeGetAsync<AvaIdentity?>(url);

            if (identity == null)
            {
                _logger.LogWarning("Remote identity service returned null for AvaId {AvaId}", avaId);
                return null;
            }

            _logger.LogInformation(
                "Resolved identity for {AvaId}: DisplayName={DisplayName}, Type={Type}",
                identity.AvaId,
                identity.DisplayName,
                identity.Type
            );

            return identity;
        }

        // ---------------------------------------------------------------------
        // 3. Validate a provided identity stamp (returns bool)
        // ---------------------------------------------------------------------
        public async Task<bool> ValidateIdentityAsync(IdentityStamp stamp)
        {
            if (stamp == null)
                throw new ArgumentNullException(nameof(stamp));

            string url = BuildUrl("/api/identity/validate");

            _logger.LogInformation(
                "Validating identity stamp for PrimaryAvaId={PrimaryAvaId}",
                stamp.PrimaryAvaId ?? "(null)"
            );

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsJsonAsync(url, stamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error calling remote identity validation endpoint at {Url}", url);
                throw;
            }

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Remote identity validation failed for {PrimaryAvaId}. StatusCode={StatusCode}, Body={Body}",
                    stamp.PrimaryAvaId,
                    (int)response.StatusCode,
                    body
                );
                return false;
            }

            // Optionally treat body as bool, but simplest pattern:
            _logger.LogInformation(
                "Remote identity validation succeeded for {PrimaryAvaId}",
                stamp.PrimaryAvaId
            );

            return true;
        }

        // ---------------------------------------------------------------------
        // Internal helpers
        // ---------------------------------------------------------------------
        private string BuildUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(_config.IdentityConnectionString))
                throw new InvalidOperationException(
                    "RemoteIdentityResolver requires IdentityConnectionString in VaultInstanceConfig.");

            return _config.IdentityConnectionString.TrimEnd('/') + path;
        }

        private async Task<T> SafeGetAsync<T>(string url)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<T>(url);
                if (result == null)
                    throw new InvalidOperationException($"Remote identity returned null for URL: {url}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Remote identity GET request failed at {Url}", url);
                throw;
            }
        }

        private void LogStamp(string prefix, IdentityStamp stamp)
        {
            _logger.LogInformation(
                "{Prefix}: PrimaryAvaId={PrimaryAvaId}, PrimaryType={PrimaryType}, NodeId={NodeId}, ModuleId={ModuleId}, Timestamp={TimestampUtc}",
                prefix,
                stamp.PrimaryAvaId,
                stamp.PrimaryType,
                stamp.NodeId,
                stamp.ModuleId,
                stamp.TimestampUtc
            );
        }
    }
}
