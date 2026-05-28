using AVA.UI.CORE.Models.UI;
using AVA.UI.State;

namespace AVA.UI.Features.Canvas.ViewModels;

/// <summary>
/// ViewModel for SingleModelCard.razor.
/// Owns draft input, typing state, send dispatch, model selection, and global broadcast handling.
/// Per-instance — one created per SingleModelCard component.
/// </summary>
public class SingleModelCardVM : IDisposable
{
    private readonly AppState _appState;
    private readonly CardState _card;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Local UI state ────────────────────────────────────────────────────────
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Reads from shared AppState so the waiting indicator survives VM re-creation
    /// when the user navigates away and returns while a model is still responding.
    /// </summary>
    public bool IsTyping
    {
        get => CurrentModelId != null && _appState.IsModelTyping(CurrentModelId);
        private set { if (CurrentModelId != null) _appState.SetModelTyping(CurrentModelId, value); }
    }

    public bool ShowSettings { get; set; }
    public bool ShowModelPicker { get; set; }

    // ── Computed ──────────────────────────────────────────────────────────────
    public string? CurrentModelId => _card.ModelProfileIds.Count > 0 ? _card.ModelProfileIds[0] : null;
    public string CurrentModelLabel => CurrentModelId != null ? _appState.GetModelName(CurrentModelId) : "No model";
    public IReadOnlyList<Message> DisplayMessages =>
        CurrentModelId != null ? _appState.GetConversation(CurrentModelId) : Array.Empty<Message>();
    public int CharacterCount => Input.Length;
    public int CharacterLimit => _appState.GetModelCharacterLimit(CurrentModelId);
    public bool HasCharacterLimit => CharacterLimit > 0;
    public bool IsOverCharacterLimit => HasCharacterLimit && CharacterCount > CharacterLimit;

    public SingleModelCardVM(AppState appState, CardState card)
    {
        _appState = appState;
        _card = card;
    }

    // ── Send ──────────────────────────────────────────────────────────────────
    // TODO: Replace with SendPromptAction
    public async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(Input) || IsTyping || IsOverCharacterLimit) return;
        var prompt = Input.Trim();
        if (CurrentModelId != null &&
            _appState.TryGetCharacterLimitWarning(prompt, new[] { CurrentModelId }, out var warning))
        {
            _appState.AppendAssistantConversationMessage(CurrentModelId, warning, CurrentModelLabel, true);
            Notify();
            return;
        }

        Input = string.Empty;
        await SendPromptAsync(prompt);
    }

    public async Task HandleGlobalBroadcast(string prompt)
    {
        await SendPromptAsync(prompt);
    }

    private async Task SendPromptAsync(string prompt)
    {
        if (IsTyping) return;
        IsTyping = true;
        Notify();

        try
        {
            if (CurrentModelId == null) return;

            var ctx = _appState.CaptureChatContext();
            _appState.AppendUserConversationMessage(CurrentModelId, prompt, CurrentModelLabel);
            var response = await _appState.Sessions.SendToModelAsync(prompt, CurrentModelId);
            var content = AppState.ExtractContent(response);
            _appState.AppendAssistantConversationMessage(
                CurrentModelId,
                string.IsNullOrEmpty(content) ? "(empty response)" : content,
                CurrentModelLabel,
                !response.Success,
                responseMetadata: AppState.BuildResponseMetadata(response),
                sessionContext: ctx);
        }
        catch (Exception ex)
        {
            if (CurrentModelId != null)
                _appState.AppendAssistantConversationMessage(CurrentModelId, ex.Message, CurrentModelLabel, true);
        }
        finally
        {
            IsTyping = false;
            Notify();
        }
    }

    // ── Card operations ───────────────────────────────────────────────────────
    public void SelectModel(string modelId)
    {
        if (_card.ModelProfileIds.Count == 0) _card.ModelProfileIds.Add(modelId);
        else _card.ModelProfileIds[0] = modelId;
        ShowModelPicker = false;
        _appState.SaveCanvasState();
        Notify();
    }

    public void ToggleMinimise()
    {
        _card.IsMinimised = !_card.IsMinimised;
        _appState.NotifyStateChanged();
    }

    public void Close() => _appState.RemoveCard(_card.CardId);

    public void Dispose() { }
}
