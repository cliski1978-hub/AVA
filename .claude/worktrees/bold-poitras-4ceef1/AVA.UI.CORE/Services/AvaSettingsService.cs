// ─────────────────────────────────────────────────────────────────────────────
//  Class    : AvaSettingsService
//  Namespace: AVA.UI.CORE.Services
//  Purpose  : Loads and saves AVA application settings to disk as JSON.
//             Persists to the OS local app data folder and now supports
//             vault, project, and session state persistence.
// ─────────────────────────────────────────────────────────────────────────────

using System.Text.Json;
using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Models.Settings;
using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Services
{
    public class AvaSettingsService : IAvaSettingsService
    {
        #region Properties

        public string SelectedModel { get; set; } = "gpt-4";
        public bool UseMockCore { get; set; } = true;
        public bool EnableDebugLogging { get; set; } = true;
        public bool AutoScrollOutput { get; set; } = true;
        public bool AllowFileDrop { get; set; } = true;

        public AppSettings AppSettings { get; private set; } = new AppSettings();
        public AppSettings Settings => AppSettings;

        public List<VaultState> Vaults
        {
            get => AppSettings.Vaults;
            set => AppSettings.Vaults = value ?? new List<VaultState>();
        }

        #endregion

        #region Persistence Paths

        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AVA");

        private static readonly string SettingsFile = Path.Combine(
            SettingsFolder,
            "settings.json");

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        #endregion

        #region Public Methods

        public void LoadSettings()
        {
            LoadAsync().GetAwaiter().GetResult();
        }

        public void SaveSettings()
        {
            SaveAsync().GetAwaiter().GetResult();
        }

        public async Task LoadAsync()
        {
            try
            {
                EnsureSettingsDirectory();

                if (File.Exists(SettingsFile))
                {
                    var json = await File.ReadAllTextAsync(SettingsFile).ConfigureAwait(false);
                    var loaded = JsonSerializer.Deserialize<PersistedSettingsWrapper>(json, SerializerOptions);

                    if (loaded != null)
                    {
                        SelectedModel = loaded.SelectedModel ?? SelectedModel;
                        UseMockCore = loaded.UseMockCore;
                        EnableDebugLogging = loaded.EnableDebugLogging;
                        AutoScrollOutput = loaded.AutoScrollOutput;
                        AllowFileDrop = loaded.AllowFileDrop;
                        AppSettings = loaded.AppSettings ?? new AppSettings();
                    }
                }
                else
                {
                    AppSettings = new AppSettings();
                    await SaveAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVA] Failed to load settings: {ex.Message}");
                AppSettings = new AppSettings();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                EnsureSettingsDirectory();

                var wrapper = new PersistedSettingsWrapper
                {
                    SelectedModel = SelectedModel,
                    UseMockCore = UseMockCore,
                    EnableDebugLogging = EnableDebugLogging,
                    AutoScrollOutput = AutoScrollOutput,
                    AllowFileDrop = AllowFileDrop,
                    AppSettings = AppSettings
                };

                var json = JsonSerializer.Serialize(wrapper, SerializerOptions);
                await File.WriteAllTextAsync(SettingsFile, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVA] Failed to save settings: {ex.Message}");
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
            return AppSettings.Vaults ?? new List<VaultState>();
        }

        public async Task SaveSessionAsync(string vaultId, string? projectId, SessionState session)
        {
            ArgumentNullException.ThrowIfNull(session);

            var vault = AppSettings.Vaults?.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                var existingVaultSession = vault.Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
                if (existingVaultSession != null)
                {
                    vault.Sessions.Remove(existingVaultSession);
                }

                vault.Sessions.Add(session);
                await SaveAsync().ConfigureAwait(false);
                return;
            }

            var project = vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null)
            {
                return;
            }

            var existing = project.Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
            if (existing != null)
            {
                project.Sessions.Remove(existing);
            }

            project.Sessions.Add(session);
            await SaveAsync().ConfigureAwait(false);
        }

        public async Task<SessionState?> LoadSessionAsync(string vaultId, string? projectId, string sessionId)
        {
            await LoadAsync().ConfigureAwait(false);

            var vault = AppSettings.Vaults?.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                return vault.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            }

            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            return project?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }

        #endregion

        #region Internals

        private static void EnsureSettingsDirectory()
        {
            if (!Directory.Exists(SettingsFolder))
            {
                Directory.CreateDirectory(SettingsFolder);
            }
        }

        #endregion

        #region Wrapper

        private class PersistedSettingsWrapper
        {
            public string? SelectedModel { get; set; }
            public bool UseMockCore { get; set; }
            public bool EnableDebugLogging { get; set; }
            public bool AutoScrollOutput { get; set; }
            public bool AllowFileDrop { get; set; }
            public AppSettings? AppSettings { get; set; }
        }

        #endregion
    }
}
