namespace AVA.UI.Features.Chat.Actions;

/// <summary>
/// Broadcasts a prompt to all sessions in the broadcast group.
/// Replaces: AppState.BroadcastPromptAsync() + inline logic in BroadcastCard, GlobalPromptBar
/// Pipeline: InputPaneVM/GlobalPromptBarVM → BroadcastPromptAction → ChatConversationState + SessionManager → Notify
/// </summary>
public class BroadcastPromptAction
{
    // TODO: Inject ChatConversationState, SessionManager
    // TODO: ExecuteAsync(string prompt, CancellationToken ct = default)
}
