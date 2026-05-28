using AVA.UI.Features.Canvas.State;

namespace AVA.UI.Features.Canvas.ViewModels;

/// <summary>
/// ViewModel for CanvasContextMenu.razor.
/// Owns context menu visibility, position, and block-level command dispatch.
/// Pipeline: CanvasContextMenu.razor → CanvasContextMenuVM → ICanvasInteractionService
/// </summary>
public class CanvasContextMenuVM : IDisposable
{
    private readonly CanvasRuntimeState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public CanvasContextMenuVM(CanvasRuntimeState state)
    {
        _state = state;
    }

    // TODO: IsVisible, Position (X, Y), TargetBlockId
    // TODO: Cut(), Copy(), Paste(), Delete()
    // TODO: Dismiss()

    public void Dispose() { }
}
