// ─────────────────────────────────────────────────────────────────────────────
//  Class    : VaultWorkspaceFileService
//  Namespace: AVA.UI.CORE.Services
//  Purpose  : Persists Vault/Project/Session hierarchy to vaults.json.
//             Separate from settings.json so app preferences and workspace
//             state have independent lifecycles.
//             Acts as offline mirror/backup of the AvaVault SQL database.
// ─────────────────────────────────────────────────────────────────────────────

using System.Text.Json;
using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Services
{
    public class VaultWorkspaceFileService
    {
        private readonly AvaSettingsService _settings;

        public VaultWorkspaceFileService(AvaSettingsService settings)
        {
            _settings = settings;
        }

        #region State

        public List<VaultState> Vaults { get; private set; } = new();

        #endregion

        #region Paths

        private string VaultsFolder => _settings.AppSettings.VaultsFolderPath;

        private string VaultsFile => Path.Combine(VaultsFolder, "vaults.json");

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        #endregion

        #region Load / Save

        public void Load()
        {
            LoadAsync().GetAwaiter().GetResult();
        }

        public async Task LoadAsync()
        {
            try
            {
                EnsureDirectory();

                if (File.Exists(VaultsFile))
                {
                    var json   = await File.ReadAllTextAsync(VaultsFile).ConfigureAwait(false);
                    var loaded = JsonSerializer.Deserialize<VaultWorkspaceWrapper>(json, SerializerOptions);
                    Vaults = loaded?.Vaults ?? new List<VaultState>();
                }
                else
                {
                    Vaults = new List<VaultState>();
                    await SaveAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVA] Failed to load vaults.json: {ex.Message}");
                Vaults = new List<VaultState>();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                EnsureDirectory();

                var wrapper = new VaultWorkspaceWrapper
                {
                    SchemaVersion = "1.0",
                    LastSaved     = DateTime.UtcNow,
                    Vaults        = Vaults
                };

                var json = JsonSerializer.Serialize(wrapper, SerializerOptions);
                await File.WriteAllTextAsync(VaultsFile, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVA] Failed to save vaults.json: {ex.Message}");
            }
        }

        public async Task SaveVaultsAsync(List<VaultState> vaults)
        {
            Vaults = vaults ?? new List<VaultState>();
            await SaveAsync().ConfigureAwait(false);
        }

        public async Task<List<VaultState>> LoadVaultsAsync()
        {
            await LoadAsync().ConfigureAwait(false);
            return Vaults;
        }

        public async Task SaveSessionAsync(string vaultId, string? projectId, SessionState session)
        {
            ArgumentNullException.ThrowIfNull(session);

            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return;

            if (string.IsNullOrWhiteSpace(projectId))
            {
                var existing = vault.Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
                if (existing != null) vault.Sessions.Remove(existing);
                vault.Sessions.Add(session);
                await SaveAsync().ConfigureAwait(false);
                return;
            }

            var project = vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null) return;

            var existingSession = project.Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
            if (existingSession != null) project.Sessions.Remove(existingSession);
            project.Sessions.Add(session);

            await SaveAsync().ConfigureAwait(false);
        }

        #endregion

        #region Internals

        private void EnsureDirectory()
        {
            if (!Directory.Exists(VaultsFolder))
                Directory.CreateDirectory(VaultsFolder);
        }

        #endregion

        #region Wrapper

        private class VaultWorkspaceWrapper
        {
            public string SchemaVersion { get; set; } = "1.0";
            public DateTime LastSaved { get; set; }
            public List<VaultState> Vaults { get; set; } = new();
        }

        #endregion
    }
}
