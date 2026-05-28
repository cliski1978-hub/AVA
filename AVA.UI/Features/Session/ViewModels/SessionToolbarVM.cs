using AVA.UI.Features.Session.Actions;
using AVA.UI.Features.Session.State;
using AVA.UI.Features.Canvas.Actions;
using AVA.UI.Features.Project.Actions;

namespace AVA.UI.Features.Session.ViewModels;

/// <summary>
/// ViewModel for SessionToolbar.razor.
/// Owns toolbar-level session actions: attach model, cycle layout, add card, add file.
/// Pipeline: SessionToolbar.razor → SessionToolbarVM → Session/Canvas/Project Actions → Notify
/// </summary>
public class SessionToolbarVM : IDisposable
{
    private readonly SessionUiState _state;
    private readonly AttachModelAction _attachModel;
    private readonly CycleLayoutAction _cycleLayout;
    private readonly CreateCardAction _createCard;
    private readonly AddFileRefAction _addFileRef;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public SessionToolbarVM(
        SessionUiState state,
        AttachModelAction attachModel,
        CycleLayoutAction cycleLayout,
        CreateCardAction createCard,
        AddFileRefAction addFileRef)
    {
        _state = state;
        _attachModel = attachModel;
        _cycleLayout = cycleLayout;
        _createCard = createCard;
        _addFileRef = addFileRef;
    }

    // TODO: AttachNextModelAsync()
    // TODO: CycleLayoutAsync()
    // TODO: AddCardAsync()
    // TODO: AddFileAsync()
    // TODO: ToggleKnowledgeBaseAsync()
    // TODO: Breadcrumb display properties

    public void Dispose() { }
}
