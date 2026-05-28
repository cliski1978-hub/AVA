namespace AVA.UI.Features.Chat.Actions;

/// <summary>
/// Sends a prompt to the active session model.
/// Replaces: AppState.SendPromptAsync() + inline logic in InputPane, SingleModelCard
/// Pipeline: InputPaneVM → SendPromptAction → ChatConversationState + SessionManager → Notify
/// </summary>
public class SendPromptAction
{
    // TODO: Inject ChatConversationState, SessionManager
    // TODO: ExecuteAsync(string prompt, string modelId, CancellationToken ct = default)
}
