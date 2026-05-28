using AVA.UI.CORE.Models.Settings;
using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Interfaces
{
    public interface IAvaSettingsService
    {
        string SelectedModel { get; set; }
        bool UseMockCore { get; set; }
        bool EnableDebugLogging { get; set; }
        bool AutoScrollOutput { get; set; }
        bool AllowFileDrop { get; set; }
        AppSettings AppSettings { get; }
        List<VaultState> Vaults { get; set; }

        void LoadSettings();
        void SaveSettings();
        Task LoadAsync();
        Task SaveAsync();
        Task SaveVaultsAsync(List<VaultState> vaults);
        Task<List<VaultState>> LoadVaultsAsync();
        Task SaveSessionAsync(string vaultId, string? projectId, SessionState session);
        Task<SessionState?> LoadSessionAsync(string vaultId, string? projectId, string sessionId);
    }
}
