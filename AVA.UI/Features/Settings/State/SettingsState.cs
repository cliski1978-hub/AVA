namespace AVA.UI.Features.Settings.State;

/// <summary>
/// Runtime state for the Settings feature.
/// Owns connection status display state.
/// ConnectAsync / TestEndpointAsync live in Actions (Step 6) — too many cross-feature dependencies.
/// </summary>
public class SettingsState
{
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── State ─────────────────────────────────────────────────────────────────
    public bool IsConnected { get; private set; }
    public string ConnectionType { get; private set; } = string.Empty;
    public string ConnectionStatus { get; private set; } = "Not connected";
    public string ConnectionDetails { get; private set; } = string.Empty;

    // ── Mutations ─────────────────────────────────────────────────────────────
    public void UpdateStatus(string type, string status, string details = "")
    {
        ConnectionType = type;
        ConnectionStatus = status;
        ConnectionDetails = details;
        Notify();
    }

    public void SetConnected(bool connected)
    {
        IsConnected = connected;
        Notify();
    }
}
