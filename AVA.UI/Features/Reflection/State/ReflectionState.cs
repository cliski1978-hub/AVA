using System.Collections.ObjectModel;

namespace AVA.UI.Features.Reflection.State;

/// <summary>
/// Runtime state for the Reflection and Memory log features.
/// Owns memory events, reflections, and contradiction tracking.
/// </summary>
public class ReflectionState
{
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── State ─────────────────────────────────────────────────────────────────
    public ObservableCollection<string> MemoryEvents { get; } = new();
    public ObservableCollection<string> Reflections { get; } = new();
    public string LatestInsight { get; private set; } = string.Empty;
    public bool HasContradiction { get; private set; }

    // ── Mutations ─────────────────────────────────────────────────────────────
    public void AppendMemory(string message)
    {
        MemoryEvents.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        Notify();
    }

    public void ClearMemory()
    {
        MemoryEvents.Clear();
        AppendMemory("Memory log cleared.");
    }

    public void AppendReflection(string insight, bool isContradiction = false, string score = "")
    {
        LatestInsight = insight;
        HasContradiction = isContradiction;

        var tag = isContradiction ? "CONTRADICTION" : "ALIGNMENT";
        Reflections.Add($"[{DateTime.Now:HH:mm:ss}] {tag}: {insight}");

        if (!string.IsNullOrWhiteSpace(score))
            Reflections.Add($"Score: {score}");

        Notify();
    }
}
