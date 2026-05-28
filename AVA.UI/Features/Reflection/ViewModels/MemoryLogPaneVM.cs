using System.Collections.ObjectModel;
using AVA.UI.Features.Reflection.State;

namespace AVA.UI.Features.Reflection.ViewModels;

/// <summary>
/// ViewModel for MemoryLogPane.razor.
/// Owns memory event log display and clear dispatch.
/// Singleton — wraps ReflectionState, no per-instance state.
/// </summary>
public class MemoryLogPaneVM : IDisposable
{
    private readonly ReflectionState _state;

    public event Action? OnChange;

    public MemoryLogPaneVM(ReflectionState state)
    {
        _state = state;
        _state.OnChange += () => OnChange?.Invoke();
    }

    // ── Display properties ────────────────────────────────────────────────────
    public ObservableCollection<string> MemoryEvents => _state.MemoryEvents;

    // ── Mutations ─────────────────────────────────────────────────────────────
    public void Clear() => _state.ClearMemory();

    public void Dispose() { }
}
