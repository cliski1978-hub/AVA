using AVA.UI.CORE.Models.UI;
using AVA.UI.Features.Chat.State;
using AVA.UI.State;

namespace AVA.UI.Features.Chat.ViewModels;

/// <summary>
/// ViewModel for OutputPane.razor.
/// Owns message display, title, waiting state, and clear dispatch.
/// Routes through AppState transitionally for session-dependent methods.
/// Registered as singleton — no per-instance state.
/// </summary>
public class OutputPaneVM : IDisposable
{
    private readonly AppState _appState;
    private readonly ChatConversationState _chatState;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public OutputPaneVM(AppState appState, ChatConversationState chatState)
    {
        _appState = appState;
        _chatState = chatState;

        // Propagate state changes to any component subscribed to this VM
        _appState.OnChange += Notify;
        _chatState.OnChange += Notify;
    }

    // ── Display properties ────────────────────────────────────────────────────
    public IReadOnlyList<Message> GetMessages() => _appState.GetMainChatMessages();
    public string GetTitle() => _appState.GetMainChatTitle();
    public bool IsWaiting => _appState.IsMainChatWaiting;
    public string? GetActiveSessionId() => _chatState.ActiveSessionId;

    // ── Mutations ─────────────────────────────────────────────────────────────
    public void Clear() => _appState.ClearMainChat();

    public void Dispose()
    {
        _appState.OnChange -= Notify;
        _chatState.OnChange -= Notify;
    }
}
