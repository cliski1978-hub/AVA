using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models.UI;
using AVA.UI.State;

namespace AVA.UI.Features.Canvas.ViewModels;

/// <summary>
/// ViewModel for GlobalPromptBar.razor.
/// Owns draft text, sending state, and broadcast send dispatch.
/// Per-instance — one created per GlobalPromptBar component.
/// </summary>
public class GlobalPromptBarVM : IDisposable
{
    private readonly AppState _appState;
    private readonly IAvaIdService _ids;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Local UI state ────────────────────────────────────────────────────────
    public string Text { get; set; } = string.Empty;
    public bool IsSending { get; private set; }

    public GlobalPromptBarVM(AppState appState, IAvaIdService ids)
    {
        _appState = appState;
        _ids      = ids;
    }

    // ── Computed ──────────────────────────────────────────────────────────────
    public string GetTargetInfo()
    {
        var count = GetParticipatingCards().Count;
        return $"{count} card{(count == 1 ? "" : "s")}";
    }

    public int CharacterCount => Text.Length;
    public int CharacterLimit => _appState.GetCharacterLimitForModels(GetTargetModelIds());
    public bool HasCharacterLimit => CharacterLimit > 0;
    public bool IsOverCharacterLimit => HasCharacterLimit && CharacterCount > CharacterLimit;

    // ── Send ──────────────────────────────────────────────────────────────────
    public async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(Text) || IsSending || IsOverCharacterLimit) return;

        var prompt   = Text.Trim();
        var cards    = GetParticipatingCards();
        var modelIds = GetTargetModelIds(cards);

        if (modelIds.Count == 0) return;
        if (_appState.TryGetCharacterLimitWarning(prompt, modelIds, out var warning))
        {
            foreach (var modelId in modelIds)
                _appState.AppendAssistantConversationMessage(modelId, warning, _appState.GetModelName(modelId), true);
            Notify();
            return;
        }

        Text      = string.Empty;
        IsSending = true;
        Notify();

        try
        {
            var ctx    = _appState.CaptureChatContext();
            var turnId = _ids.NewTurnId();
            foreach (var card in cards) card.IsMinimised = false;

            foreach (var modelId in modelIds)
                _appState.AppendUserConversationMessage(modelId, prompt, _appState.GetModelName(modelId), turnId);

            var results = await _appState.Sessions.BroadcastAsync(prompt, modelIds);

            for (var i = 0; i < results.Count; i++)
            {
                var modelId = i < modelIds.Count ? modelIds[i] : null;
                if (string.IsNullOrWhiteSpace(modelId)) continue;
                var content = AppState.ExtractContent(results[i]);
                _appState.AppendAssistantConversationMessage(
                    modelId,
                    string.IsNullOrEmpty(content) ? "(empty response)" : content,
                    _appState.GetModelName(modelId),
                    !results[i].Success,
                    turnId,
                    AppState.BuildResponseMetadata(results[i]),
                    sessionContext: ctx);
            }

            _appState.SaveCanvasState();
        }
        finally
        {
            IsSending = false;
            Notify();
        }
    }

    private List<CardState> GetParticipatingCards() =>
        _appState.CurrentSession?.Canvas.Cards
            .Where(c => c.ParticipatesInGlobal)
            .ToList() ?? new();

    private List<string> GetTargetModelIds()
        => GetTargetModelIds(GetParticipatingCards());

    private static List<string> GetTargetModelIds(IEnumerable<CardState> cards)
        => cards
            .SelectMany(c => c.ModelProfileIds)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public void Dispose() { }
}
