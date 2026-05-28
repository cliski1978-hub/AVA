using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Models.UI;
using AVA.UI.State;

namespace AVA.UI.Features.Canvas.ViewModels;

/// <summary>
/// ViewModel for BroadcastCard.razor.
/// Owns draft input, sending state, tab selection, expand tracking, and broadcast send.
/// Per-instance — one created per BroadcastCard component.
/// </summary>
public class BroadcastCardVM : IDisposable
{
    private readonly AppState _appState;
    private readonly CardState _card;
    private readonly IAvaIdService _ids;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Local UI state ────────────────────────────────────────────────────────
    public string Input { get; set; } = string.Empty;
    public bool IsSending { get; private set; }
    public string? ActiveTab { get; private set; }
    public HashSet<string> ExpandedIds { get; } = new();
    public bool ShowModelPicker { get; set; }

    // ── Computed ──────────────────────────────────────────────────────────────
    public List<Message> Responses => _appState.GetBroadcastConversation(_card.ModelProfileIds);

    public IEnumerable<string> SelectedModelIds =>
        _card.ModelProfileIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    public int CharacterCount => Input.Length;
    public int CharacterLimit => _appState.GetCharacterLimitForModels(SelectedModelIds);
    public bool HasCharacterLimit => CharacterLimit > 0;
    public bool IsOverCharacterLimit => HasCharacterLimit && CharacterCount > CharacterLimit;

    public string ResolveModelLabel(string modelId) =>
        Responses.FirstOrDefault(r => r.ModelId == modelId)?.ModelLabel
        ?? _appState.GetModelName(modelId);

    public BroadcastCardVM(AppState appState, CardState card, IAvaIdService ids)
    {
        _appState = appState;
        _card     = card;
        _ids      = ids;
        EnsureValidLayout();
    }

    // ── Tab / expand ──────────────────────────────────────────────────────────
    public void SelectTab(string modelId) { ActiveTab = modelId; Notify(); }

    public void ToggleExpand(string id)
    {
        if (!ExpandedIds.Remove(id)) ExpandedIds.Add(id);
        Notify();
    }

    // ── Send ──────────────────────────────────────────────────────────────────
    // TODO: Replace with BroadcastPromptAction
    public async Task SendBroadcastAsync()
    {
        if (string.IsNullOrWhiteSpace(Input) || IsSending || IsOverCharacterLimit) return;
        var modelIds = SelectedModelIds.ToList();
        if (modelIds.Count == 0) return;

        var prompt = Input.Trim();
        if (_appState.TryGetCharacterLimitWarning(prompt, modelIds, out var warning))
        {
            foreach (var modelId in modelIds)
                _appState.AppendAssistantConversationMessage(modelId, warning, _appState.GetModelName(modelId), true);
            Notify();
            return;
        }

        var turnId = _ids.NewTurnId();
        Input = string.Empty;
        IsSending = true;
        Notify();

        try
        {
            var ctx = _appState.CaptureChatContext();

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

            ActiveTab ??= modelIds.FirstOrDefault();
            _appState.SaveCanvasState();
        }
        catch (Exception ex)
        {
            foreach (var modelId in modelIds)
                _appState.AppendAssistantConversationMessage(modelId, ex.Message, _appState.GetModelName(modelId), true, turnId);
        }
        finally
        {
            IsSending = false;
            Notify();
        }
    }

    public async Task HandleGlobalBroadcast(string prompt)
    {
        Input = prompt;
        await SendBroadcastAsync();
    }

    // ── Card operations ───────────────────────────────────────────────────────
    public void ToggleModel(string modelId)
    {
        if (_card.ModelProfileIds.Contains(modelId)) _card.ModelProfileIds.Remove(modelId);
        else _card.ModelProfileIds.Add(modelId);
        ActiveTab ??= _card.ModelProfileIds.FirstOrDefault();
        _appState.SaveCanvasState();
        Notify();
    }

    public void SetLayout(string layout)
    {
        _card.ResponseLayout = layout;
        _appState.SaveCanvasState();
        Notify();
    }

    public void ToggleMinimise()
    {
        _card.IsMinimised = !_card.IsMinimised;
        _appState.NotifyStateChanged();
    }

    public void Close() => _appState.RemoveCard(_card.CardId);

    private void EnsureValidLayout()
    {
        if (string.IsNullOrWhiteSpace(_card.ResponseLayout))
            _card.ResponseLayout = "Stacked";
    }

    public void Dispose() { }
}
