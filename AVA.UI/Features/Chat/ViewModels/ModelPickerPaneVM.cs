using AVA.UI.Features.Chat.State;

namespace AVA.UI.Features.Chat.ViewModels;

/// <summary>
/// ViewModel for ModelPickerPane.razor.
/// Owns LLM profile/model selection and broadcast toggle behaviour.
/// Pipeline: ModelPickerPane.razor → ModelPickerPaneVM → ChatConversationState + SessionManager → Notify
/// </summary>
public class ModelPickerPaneVM : IDisposable
{
    private readonly ChatConversationState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public ModelPickerPaneVM(ChatConversationState state)
    {
        _state = state;
    }

    // TODO: SelectedProfile, SelectedModel
    // TODO: IsBroadcastMode
    // TODO: SelectProfileAsync(string profileId)
    // TODO: SelectModelAsync(string modelId)
    // TODO: ToggleBroadcast()

    public void Dispose() { }
}
