using AVA.UI.Features.Chat.State;

namespace AVA.UI.Features.Chat.ViewModels;

/// <summary>
/// ViewModel for ChatLayout.razor.
/// Owns top-level chat layout coordination and panel visibility.
/// Pipeline: ChatLayout.razor → ChatLayoutVM → ChatConversationState → Notify
/// </summary>
public class ChatLayoutVM : IDisposable
{
    private readonly ChatConversationState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public ChatLayoutVM(ChatConversationState state)
    {
        _state = state;
    }

    // TODO: Panel visibility coordination
    // TODO: GetMainChatTitle()
    // TODO: IsMainChatWaiting

    public void Dispose() { }
}
