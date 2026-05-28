using AVA.UI.Features.Chat.State;

namespace AVA.UI.Features.Chat.ViewModels;

/// <summary>
/// ViewModel for PromptPreviewPane.razor.
/// Owns prompt preview display and token estimate.
/// Pipeline: PromptPreviewPane.razor → PromptPreviewPaneVM → ChatConversationState → Notify
/// </summary>
public class PromptPreviewPaneVM : IDisposable
{
    private readonly ChatConversationState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public PromptPreviewPaneVM(ChatConversationState state)
    {
        _state = state;
    }

    // TODO: PreviewText
    // TODO: TokenEstimate
    // TODO: IsVisible
    // TODO: Close()

    public void Dispose() { }
}
