using System.Collections.ObjectModel;
using AVA.UI.Features.Reflection.State;

namespace AVA.UI.Features.Reflection.ViewModels;

/// <summary>
/// ViewModel for ReflectionPane.razor.
/// Owns reflection display, contradiction flagging, and clear dispatch.
/// Singleton — wraps ReflectionState, no per-instance state.
/// </summary>
public class ReflectionPaneVM : IDisposable
{
    private readonly ReflectionState _state;

    public event Action? OnChange;

    public ReflectionPaneVM(ReflectionState state)
    {
        _state = state;
        _state.OnChange += () => OnChange?.Invoke();
    }

    // ── Display properties ────────────────────────────────────────────────────
    public ObservableCollection<string> Reflections => _state.Reflections;
    public string LatestInsight => _state.LatestInsight;
    public bool HasContradiction => _state.HasContradiction;

    // ── Mutations ─────────────────────────────────────────────────────────────
    public void Clear() => _state.Reflections.Clear();

    public void Dispose() { }
}
