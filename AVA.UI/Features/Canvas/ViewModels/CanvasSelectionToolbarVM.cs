using AVA.UI.Features.Canvas.State;

namespace AVA.UI.Features.Canvas.ViewModels;

/// <summary>
/// ViewModel for CanvasSelectionToolbar.razor.
/// Owns formatting command dispatch for selected block text.
/// Pipeline: CanvasSelectionToolbar.razor → CanvasSelectionToolbarVM → ICanvasInteractionService
/// </summary>
public class CanvasSelectionToolbarVM : IDisposable
{
    private readonly CanvasRuntimeState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public CanvasSelectionToolbarVM(CanvasRuntimeState state)
    {
        _state = state;
    }

    // TODO: IsVisible
    // TODO: Position (X, Y)
    // TODO: ApplyBold(), ApplyItalic(), ApplyCode()
    // TODO: Dismiss()

    public void Dispose() { }
}
