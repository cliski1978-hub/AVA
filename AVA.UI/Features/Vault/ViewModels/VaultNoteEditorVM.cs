using Microsoft.Extensions.Logging;

using AVA.UI.Errors;
using AVA.UI.Runtime;
using AVA.UI.State;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Services.Data;

namespace AVA.UI.Features.Vault.ViewModels;

/// <summary>
/// ViewModel for the note editing surface.
/// Owns: active note, draft title/content, available tags, assigned tags, related notes.
/// All write operations check the CfkApiResponse returned by the Vault service layer.
/// If Succeeded is false the UserMessage surfaces in the error ribbon automatically.
/// Singleton — one editor at a time. Call LoadNoteAsync to switch notes.
/// </summary>
public class VaultNoteEditorVM : IDisposable
{
    private readonly IAvaRuntimeContext _ctx;
    private readonly AppState _appState;
    private readonly ILogger<VaultNoteEditorVM> _logger;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Active note ───────────────────────────────────────────────────────────
    public VaultNote? ActiveNote { get; private set; }
    public bool HasNote => ActiveNote != null;

    // ── Draft state ───────────────────────────────────────────────────────────
    public string DraftTitle   { get; set; } = string.Empty;
    public string DraftContent { get; set; } = string.Empty;
    public bool IsDirty => HasNote &&
        (DraftTitle   != (ActiveNote!.Title   ?? string.Empty) ||
         DraftContent != (ActiveNote!.Content ?? string.Empty));

    // ── Tag state ─────────────────────────────────────────────────────────────
    public List<VaultTag> AvailableTags { get; private set; } = new();
    public List<VaultTag> AssignedTags  => ActiveNote?.VaultNoteVaultTags?.Select(jt => jt.Tag).ToList() ?? new();

    // ── Related notes ─────────────────────────────────────────────────────────
    public List<RelatedNoteResult> RelatedNotes { get; private set; } = new();

    // ── Loading flags ─────────────────────────────────────────────────────────
    public bool IsLoading { get; private set; }
    public bool IsSaving  { get; private set; }

    public VaultNoteEditorVM(
        IAvaRuntimeContext ctx,
        AppState appState,
        ILogger<VaultNoteEditorVM> logger)
    {
        _ctx      = ctx;
        _appState = appState;
        _logger   = logger;
    }

    // ── Load ──────────────────────────────────────────────────────────────────

    public async Task LoadNoteAsync(string vaultId, string noteId)
    {
        const string source = nameof(LoadNoteAsync);
        _ctx.Errors.ClearSource(source);
        IsLoading = true;
        Notify();

        try
        {
            var storageMode  = _appState.ActiveVault?.StorageMode ?? "Database";
            ActiveNote       = await _ctx.Vault.GetNoteAsync(vaultId, noteId, storageMode);
            DraftTitle       = ActiveNote?.Title   ?? string.Empty;
            DraftContent     = ActiveNote?.Content ?? string.Empty;
            AvailableTags    = (await _ctx.Vault.ListTagsAsync(vaultId, storageMode)).ToList();
            RelatedNotes     = (await _ctx.Vault.GetRelatedNotesAsync(vaultId, noteId, storageMode)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadNoteAsync failed [{NoteId}]", noteId);
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    public async Task SaveAsync()
    {
        if (ActiveNote == null || !IsDirty) return;

        const string source = nameof(SaveAsync);
        _ctx.Errors.ClearSource(source);
        IsSaving = true;
        Notify();

        try
        {
            var storageMode = _appState.ActiveVault?.StorageMode ?? "Database";
            var response    = await _ctx.Vault.UpdateNoteAsync(
                ActiveNote.VaultID,
                ActiveNote.ID,
                DraftTitle.Trim(),
                DraftContent,
                storageMode);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            // Reload to confirm saved state and updated timestamps
            ActiveNote = await _ctx.Vault.GetNoteAsync(ActiveNote.VaultID, ActiveNote.ID, storageMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveAsync failed [{NoteId}]", ActiveNote?.ID);
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
        finally
        {
            IsSaving = false;
            Notify();
        }
    }

    public void DiscardChanges()
    {
        DraftTitle   = ActiveNote?.Title   ?? string.Empty;
        DraftContent = ActiveNote?.Content ?? string.Empty;
        Notify();
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    public async Task AssignTagAsync(string tagId)
    {
        if (ActiveNote == null) return;

        const string source = nameof(AssignTagAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var storageMode = _appState.ActiveVault?.StorageMode ?? "Database";
            var response    = await _ctx.Vault.AssignTagToNoteAsync(ActiveNote.VaultID, ActiveNote.ID, tagId, storageMode);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            ActiveNote = await _ctx.Vault.GetNoteAsync(ActiveNote.VaultID, ActiveNote.ID, storageMode);
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AssignTagAsync failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    public async Task RemoveTagAsync(string tagId)
    {
        if (ActiveNote == null) return;

        const string source = nameof(RemoveTagAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var storageMode = _appState.ActiveVault?.StorageMode ?? "Database";
            var response    = await _ctx.Vault.RemoveTagFromNoteAsync(ActiveNote.VaultID, ActiveNote.ID, tagId, storageMode);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            ActiveNote = await _ctx.Vault.GetNoteAsync(ActiveNote.VaultID, ActiveNote.ID, storageMode);
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveTagAsync failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    public async Task DeleteNoteAsync()
    {
        if (ActiveNote == null) return;

        const string source = nameof(DeleteNoteAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var storageMode = _appState.ActiveVault?.StorageMode ?? "Database";
            var response    = await _ctx.Vault.DeleteNoteAsync(ActiveNote.VaultID, ActiveNote.ID, storageMode);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteNoteAsync failed [{NoteId}]", ActiveNote?.ID);
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    // ── Create tag ────────────────────────────────────────────────────────────

    public async Task CreateTagAsync(string tagName)
    {
        if (ActiveNote == null || string.IsNullOrWhiteSpace(tagName)) return;

        const string source = nameof(CreateTagAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var storageMode = _appState.ActiveVault?.StorageMode ?? "Database";
            var createResp  = await _ctx.Vault.CreateTagAsync(ActiveNote.VaultID, tagName.Trim(), storageMode);

            if (!createResp.Succeeded)
            {
                _ctx.Errors.AddModelErrors(createResp, source, "Vault");
                return;
            }

            var assignResp = await _ctx.Vault.AssignTagToNoteAsync(
                ActiveNote.VaultID, ActiveNote.ID, createResp.Tag!.ID, storageMode);

            if (!assignResp.Succeeded)
            {
                _ctx.Errors.AddModelErrors(assignResp, source, "Vault");
                return;
            }

            ActiveNote     = await _ctx.Vault.GetNoteAsync(ActiveNote.VaultID, ActiveNote.ID, storageMode);
            AvailableTags  = (await _ctx.Vault.ListTagsAsync(ActiveNote!.VaultID, storageMode)).ToList();
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateTagAsync failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    // ── Create link ───────────────────────────────────────────────────────────

    public async Task CreateLinkAsync(string targetNoteId, string relationType)
    {
        if (ActiveNote == null || string.IsNullOrWhiteSpace(targetNoteId)) return;

        const string source = nameof(CreateLinkAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var storageMode = _appState.ActiveVault?.StorageMode ?? "Database";
            var response    = await _ctx.Vault.CreateLinkAsync(
                ActiveNote.VaultID, ActiveNote.ID, targetNoteId, relationType, storageMode);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            RelatedNotes = (await _ctx.Vault.GetRelatedNotesAsync(
                ActiveNote.VaultID, ActiveNote.ID, storageMode)).ToList();
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateLinkAsync failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    // ── Delete link ───────────────────────────────────────────────────────────

    public async Task DeleteLinkAsync(string linkId)
    {
        if (ActiveNote == null) return;

        const string source = nameof(DeleteLinkAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var storageMode = _appState.ActiveVault?.StorageMode ?? "Database";
            var response    = await _ctx.Vault.DeleteLinkAsync(ActiveNote.VaultID, linkId, storageMode);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            RelatedNotes = (await _ctx.Vault.GetRelatedNotesAsync(
                ActiveNote.VaultID, ActiveNote.ID, storageMode)).ToList();
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteLinkAsync failed [{LinkId}]", linkId);
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    // ── Clear ─────────────────────────────────────────────────────────────────

    public void Clear()
    {
        ActiveNote    = null;
        DraftTitle    = string.Empty;
        DraftContent  = string.Empty;
        AvailableTags = new();
        RelatedNotes  = new();
        Notify();
    }

    public void Dispose() { }
}
