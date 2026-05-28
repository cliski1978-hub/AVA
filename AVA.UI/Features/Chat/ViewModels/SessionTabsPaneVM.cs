using AVA.UI.Features.Chat.State;

namespace AVA.UI.Features.Chat.ViewModels;

/// <summary>
/// ViewModel for SessionTabsPane.razor.
/// Owns open session tab management and broadcast group UI.
/// Pipeline: SessionTabsPane.razor → SessionTabsPaneVM → ChatConversationState + SessionManager → Notify
/// </summary>
public class SessionTabsPaneVM : IDisposable
{
    private readonly ChatConversationState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public SessionTabsPaneVM(ChatConversationState state)
    {
        _state = state;
    }

    // TODO: OpenSessions list
    // TODO: ActiveSessionId
    // TODO: SelectSessionAsync(string sessionId)
    // TODO: CloseSessionAsync(string sessionId)
    // TODO: IsBroadcastMode, ToggleBroadcast()

    public void Dispose() { }
}
