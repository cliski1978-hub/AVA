using AVA.UI.Features.ChatContext.ViewModels;
using AVA.UI.State;

namespace AVA.UI.Features.ChatContext.ViewModels;

/// <summary>
/// Coordinates the right-side Prompt Context rail.
/// Hosts PromptContextPaneVM without moving ChatContext logic into shell components.
/// </summary>
public class RightContextNavVM : IDisposable
{
    private readonly AppState _appState;
    private readonly PromptContextPaneVM _promptContextPaneVM;
    private string _lastSessionId = string.Empty;
    private string _lastModelId = string.Empty;
    private string _lastPrompt = string.Empty;

    /// <summary>
    /// Raised when the right context rail state changes.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Gets the stable panel identifier.
    /// </summary>
    public string PanelId => "prompt-context";

    /// <summary>
    /// Gets the right rail title.
    /// </summary>
    public string Title => "Prompt Context";

    /// <summary>
    /// Gets whether the right rail is open.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Gets the expanded right rail width in pixels.
    /// </summary>
    public int WidthPx { get; private set; } = 760;

    /// <summary>
    /// Gets the current placeholder message when the rail cannot build context.
    /// </summary>
    public string? PlaceholderMessage { get; private set; }

    /// <summary>
    /// Initializes the right context rail view model.
    /// </summary>
    public RightContextNavVM(AppState appState, PromptContextPaneVM promptContextPaneVM)
    {
        _appState = appState;
        _promptContextPaneVM = promptContextPaneVM;
        _promptContextPaneVM.OnChange += HandlePromptContextPaneChanged;
    }

    /// <summary>
    /// Opens the rail and binds it to the active session and default session model.
    /// </summary>
    public async Task OpenAsync(string? currentPrompt = null)
    {
        IsOpen = true;
        await BindPromptContextAsync(currentPrompt ?? _lastPrompt);
        Notify();
    }

    /// <summary>
    /// Toggles the rail open or closed.
    /// </summary>
    public async Task ToggleAsync(string? currentPrompt = null)
    {
        if (IsOpen)
            Close();
        else
            await OpenAsync(currentPrompt);
    }

    /// <summary>
    /// Closes the rail fully without clearing Prompt Context pane state.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
        Notify();
    }

    /// <summary>
    /// Refreshes the rail when active session, model, or prompt changes.
    /// </summary>
    public async Task RefreshAsync(string? currentPrompt = null)
    {
        if (!IsOpen)
            return;

        await BindPromptContextAsync(currentPrompt ?? _lastPrompt);
        Notify();
    }

    private async Task BindPromptContextAsync(string? currentPrompt)
    {
        var session = _appState.ActiveWorkspaceSession;
        if (session == null)
        {
            PlaceholderMessage = "No active session selected.";
            return;
        }

        var modelId = session.DefaultModelId
            ?? session.AttachedModelIds.FirstOrDefault()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(modelId))
        {
            PlaceholderMessage = "No model selected for the active session.";
            return;
        }

        var sessionId = session.SessionId ?? string.Empty;
        var prompt = currentPrompt ?? string.Empty;
        PlaceholderMessage = null;

        if (string.Equals(_lastSessionId, sessionId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_lastModelId, modelId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_lastPrompt, prompt, StringComparison.Ordinal))
        {
            return;
        }

        _lastSessionId = sessionId;
        _lastModelId = modelId;
        _lastPrompt = prompt;
        await _promptContextPaneVM.OpenAsync(sessionId, modelId, prompt);
    }

    private void Notify() => OnChange?.Invoke();

    private void HandlePromptContextPaneChanged()
    {
        if (IsOpen && !_promptContextPaneVM.IsVisible && string.IsNullOrWhiteSpace(PlaceholderMessage))
        {
            IsOpen = false;
            Notify();
        }
    }

    /// <summary>
    /// Releases resources held by the right context rail view model.
    /// </summary>
    public void Dispose()
    {
        _promptContextPaneVM.OnChange -= HandlePromptContextPaneChanged;
    }
}
