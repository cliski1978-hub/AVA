using AVA.UI.Features.Chat.State;
using AVA.UI.State;

namespace AVA.UI.Features.Chat.ViewModels;

/// <summary>
/// ViewModel for InputPane.razor.
/// Owns prompt draft text, sending state, preview toggle, and send dispatch.
/// Routes through AppState transitionally — SendPromptAction replaces in future step.
/// One instance per InputPane component instance.
/// </summary>
public class InputPaneVM : IDisposable
{
    private readonly AppState _appState;
    private readonly ChatConversationState _chatState;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Local UI state ────────────────────────────────────────────────────────
    public string PromptText { get; set; } = string.Empty;

    /// <summary>
    /// Reads from shared AppState so the sending indicator survives VM re-creation
    /// when the user navigates away and returns while a response is in flight.
    /// </summary>
    public bool IsSending => _appState.IsChatSending;

    public int CharacterCount => PromptText.Length;

    public int CharacterLimit => _appState.GetActivePromptCharacterLimit();

    public bool HasCharacterLimit => CharacterLimit > 0;

    public bool IsOverCharacterLimit => HasCharacterLimit && CharacterCount > CharacterLimit;

    public bool CanSend => _appState.IsConnected
                        && !string.IsNullOrWhiteSpace(PromptText)
                        && !IsSending
                        && !IsOverCharacterLimit;

    public InputPaneVM(AppState appState, ChatConversationState chatState)
    {
        _appState = appState;
        _chatState = chatState;
    }

    // ── Mutations ─────────────────────────────────────────────────────────────
    // TODO: Replace SendPromptAsync call with SendPromptAction

    public async Task SubmitAsync()
    {
        var prompt = PromptText.Trim();
        if (string.IsNullOrWhiteSpace(prompt)) return;
        if (_appState.TryGetCharacterLimitWarning(prompt, _appState.GetActiveChatTargetModelIds(), out var warning))
        {
            _appState.AppendOutputSystem(warning);
            Notify();
            return;
        }

        PromptText = string.Empty;
        Notify();

        try
        {
            await _appState.SendPromptAsync(prompt);
        }
        catch (Exception ex)
        {
            _appState.AppendOutputSystem($"⚠️ Error: {ex.Message}");
        }
        finally
        {
            Notify();
        }
    }

    public void ShowPreview()
    {
        _chatState.PromptPreviewText = PromptText.Trim();
        _chatState.PromptTokenEstimate = PromptText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        _chatState.ShowPromptPreview = true;
        Notify();
    }

    public void Dispose() { }
}
