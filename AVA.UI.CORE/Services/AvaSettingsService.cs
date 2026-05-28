using System.Text.Json;
using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Models.Settings;

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

        #endregion

        #region Persistence Paths

        private readonly string SettingsFolder;
        private readonly string SettingsFile;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        #endregion

        #region Constructor

        public AvaSettingsService(string? settingsFilePath = null)
        {
            if (string.IsNullOrWhiteSpace(settingsFilePath))
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AVA");
                SettingsFolder = root;
                SettingsFile = Path.Combine(root, "settings.json");
            }
            else
            {
                SettingsFile = settingsFilePath;
                SettingsFolder = Path.GetDirectoryName(SettingsFile)
                    ?? throw new ArgumentException("Could not determine directory from the provided settings file path.", nameof(settingsFilePath));
            }
        }

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
                        SettingsArchitectureMigration.Normalize(AppSettings);
                    }
                }
                else
                {
                    AppSettings = new AppSettings();
                    SettingsArchitectureMigration.Normalize(AppSettings);
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
                SettingsArchitectureMigration.Normalize(AppSettings);

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

        #endregion

        #region Internals

        private void EnsureSettingsDirectory()
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
