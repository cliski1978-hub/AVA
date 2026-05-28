using AVA.UI.CORE.Models.Settings;

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

        void LoadSettings();
        void SaveSettings();
        Task LoadAsync();
        Task SaveAsync();
    }
}
