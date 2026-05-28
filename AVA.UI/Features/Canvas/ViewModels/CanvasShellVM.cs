using AVA.UI.Features.Canvas.Actions;
using AVA.UI.Features.Canvas.State;

namespace AVA.UI.Features.Canvas.ViewModels;

/// <summary>
/// ViewModel for CanvasShell.razor.
/// Owns canvas shell UI state: tabs, inspector, Ask AVA panel, tools FAB, export.
/// Pipeline: CanvasShell.razor → CanvasShellVM → Canvas Actions → CanvasRuntimeState
/// </summary>
public class CanvasShellVM : IDisposable
{
    private readonly CanvasRuntimeState _state;
    private readonly SaveCanvasAction _save;
    private readonly OpenCanvasAction _open;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public CanvasShellVM(
        CanvasRuntimeState state,
        SaveCanvasAction save,
        OpenCanvasAction open)
    {
        _state = state;
        _save = save;
        _open = open;
    }

    // TODO: IsInspectorOpen, IsAskAvaOpen, IsToolsMenuOpen
    // TODO: ActiveDocumentId
    // TODO: OpenDocuments list
    // TODO: SaveAsync(), ExportMarkdownAsync(), ExportPdfAsync()
    // TODO: DuplicateDocumentAsync()
    // TODO: CloseDocumentAsync()

    public void Dispose() { }
}
