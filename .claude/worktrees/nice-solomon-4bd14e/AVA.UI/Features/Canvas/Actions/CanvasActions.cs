using System;
using System.Linq;
using AVA.UI.Features.Canvas.Actions.Models;
using AVA.UI.Features.Canvas.Models;
using AVA.UI.Features.Canvas.Services;

namespace AVA.UI.Features.Canvas.Actions
{
    public sealed class CanvasActions
    {
        private static readonly CanvasRegistry Registry = new();
        private static ICanvasDocumentService DocumentService = new CanvasDocumentService();
        private static ICanvasDocumentPersistenceService PersistenceService = new CanvasDocumentPersistenceService();

        public static void ConfigureDocumentService(ICanvasDocumentService documentService)
        {
            DocumentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        public static void ConfigurePersistenceService(ICanvasDocumentPersistenceService persistenceService)
        {
            PersistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        }

        // ── Document lifecycle ────────────────────────────────────────────────

        public CanvasActionResult CreateDocument(string title = "")
        {
            var document = DocumentService.CreateDocumentAsync(title).GetAwaiter().GetResult();
            Registry.RegisterDocument(document);
            return DocumentSuccess("Canvas document created.", document);
        }

        public CanvasActionResult OpenDocument(string path)
        {
            // File-load not yet wired to document service; create with title from path.
            var title    = System.IO.Path.GetFileNameWithoutExtension(path?.Trim() ?? "Untitled Canvas");
            var document = DocumentService.CreateDocumentAsync(title).GetAwaiter().GetResult();
            Registry.RegisterDocument(document);
            Registry.TrackDocumentPath(document.Id, path?.Trim() ?? string.Empty);
            return DocumentSuccess("Canvas document opened.", document);
        }

        public CanvasActionResult FocusDocument(string documentId)
        {
            var document = RequireRegisteredDocument(documentId);
            DocumentService.SetActiveDocumentAsync(document).GetAwaiter().GetResult();
            Registry.SetActiveDocument(document);
            return DocumentSuccess($"Canvas document '{document.Id}' focused.", document);
        }

        public CanvasActionResult ListDocuments()
        {
            return new CanvasActionResult
            {
                Success    = true,
                Message    = "Open Canvas documents returned.",
                DocumentId = Registry.ActiveDocumentId,
                Documents  = Registry.ListOpenDocuments()
            };
        }

        public CanvasActionResult CloseDocument(string documentId)
        {
            var document = RequireRegisteredDocument(documentId);
            DocumentService.CloseDocumentAsync(documentId).GetAwaiter().GetResult();
            var closed = Registry.UnregisterDocument(documentId);
            return new CanvasActionResult
            {
                Success    = closed,
                Message    = closed
                    ? $"Canvas document '{documentId}' closed."
                    : $"Canvas document '{documentId}' was closed in the document service.",
                DocumentId = document.Id
            };
        }

        public CanvasSaveResult SaveDocument(string documentId)
        {
            var document   = RequireRegisteredDocument(documentId);
            var saveResult = PersistenceService.SaveDocumentAsync(document).GetAwaiter().GetResult();
            return ToCanvasSaveResult(document, saveResult);
        }

        public CanvasSaveResult SaveDocumentAs(string documentId, string path)
        {
            var document   = RequireRegisteredDocument(documentId);
            var saveResult = PersistenceService.SaveDocumentAsAsync(document, path).GetAwaiter().GetResult();
            Registry.TrackDocumentPath(document.Id, saveResult.SavedPath ?? path);
            return ToCanvasSaveResult(document, saveResult);
        }

        public CanvasActionResult RenameDocument(string documentId, string title)
        {
            var document = RequireRegisteredDocument(documentId);
            document.Title    = string.IsNullOrWhiteSpace(title) ? "Untitled Canvas" : title.Trim();
            document.UpdatedAt = DateTime.UtcNow;
            Registry.SetActiveDocument(document);
            return DocumentSuccess("Canvas document renamed.", document);
        }

        public CanvasDocumentState GetDocumentState(string documentId)
            => Registry.ToDocumentState(RequireRegisteredDocument(documentId));

        // ── Content ───────────────────────────────────────────────────────────

        public CanvasContentResult GetContent(string documentId)
        {
            var document = RequireRegisteredDocument(documentId);
            return new CanvasContentResult
            {
                Success    = true,
                Message    = "Canvas document content returned.",
                DocumentId = document.Id,
                Content    = GetDocumentText(document)
            };
        }

        public CanvasContentResult SetContent(string documentId, string content)
        {
            var document = RequireRegisteredDocument(documentId);
            // Clear existing blocks and replace with a single text block.
            foreach (var block in document.OrderedBlocks.ToList())
                DocumentService.RemoveBlockAsync(block.Id).GetAwaiter().GetResult();
            DocumentService.AddBlockAsync(new CanvasTextBlock { Content = content ?? string.Empty })
                .GetAwaiter().GetResult();
            document.UpdatedAt = DateTime.UtcNow;
            Registry.MarkDocumentDirty(document.Id);
            Registry.NormalizeSelection(document);
            return ContentSuccess("Canvas document content set.", document.Id, GetDocumentText(document));
        }

        public CanvasContentResult AppendContent(string documentId, string content)
        {
            var document = RequireRegisteredDocument(documentId);
            DocumentService.AddBlockAsync(new CanvasTextBlock { Content = content ?? string.Empty })
                .GetAwaiter().GetResult();
            document.UpdatedAt = DateTime.UtcNow;
            Registry.MarkDocumentDirty(document.Id);
            return ContentSuccess("Canvas document content appended.", document.Id, GetDocumentText(document));
        }

        public CanvasContentResult InsertContent(string documentId, int index, string content)
        {
            var document  = RequireRegisteredDocument(documentId);
            var newBlock  = new CanvasTextBlock { Content = content ?? string.Empty, SortOrder = index };
            DocumentService.AddBlockAsync(newBlock).GetAwaiter().GetResult();
            document.UpdatedAt = DateTime.UtcNow;
            Registry.MarkDocumentDirty(document.Id);
            return ContentSuccess("Canvas document content inserted.", document.Id, GetDocumentText(document));
        }

        public CanvasContentResult ClearContent(string documentId)
        {
            var document = RequireRegisteredDocument(documentId);
            foreach (var block in document.OrderedBlocks.ToList())
                DocumentService.RemoveBlockAsync(block.Id).GetAwaiter().GetResult();
            document.UpdatedAt = DateTime.UtcNow;
            Registry.MarkDocumentDirty(document.Id);
            Registry.ClearSelection(document.Id);
            return ContentSuccess("Canvas document content cleared.", document.Id, string.Empty);
        }

        // ── Selection ─────────────────────────────────────────────────────────

        public CanvasSelectionState GetSelection(string documentId)
            => Registry.GetSelection(documentId);

        public CanvasSelectionState SetSelection(
            string documentId, int startIndex, int endIndex,
            string activeBlockId = "", string activeBlockType = "")
            => Registry.SetSelection(documentId, startIndex, endIndex, activeBlockId, activeBlockType);

        public CanvasSelectionState ClearSelection(string documentId)
            => Registry.ClearSelection(documentId);

        public CanvasContextResult GetSelectionContext(string documentId, int beforeChars = 500, int afterChars = 500)
            => Registry.GetSelectionContext(documentId, beforeChars, afterChars);

        public CanvasContextResult GetDocumentContext(string documentId, int maxChars = 4000)
            => Registry.GetDocumentContext(documentId, maxChars);

        public CanvasContextResult GetBlockContext(string documentId, string blockId)
            => Registry.GetBlockContext(documentId, blockId);

        // ── Editing ───────────────────────────────────────────────────────────

        public CanvasEditResult ReplaceSelection(string documentId, string replacement)
        {
            var selection = Registry.GetSelection(documentId);
            var document  = RequireRegisteredDocument(documentId);
            var content   = GetDocumentText(document);
            var s = Math.Clamp(selection.StartIndex, 0, content.Length);
            var e = Math.Clamp(selection.EndIndex,   s, content.Length);
            var updated   = string.Concat(content.AsSpan(0, s), replacement ?? string.Empty, content.AsSpan(e));
            ApplyTextToDocument(document, updated);
            var newEnd    = s + (replacement?.Length ?? 0);
            var newSel    = Registry.SetSelection(documentId, newEnd, newEnd);
            return EditSuccess(document, "Canvas selection replaced.", newSel);
        }

        public CanvasEditResult InsertAtCursor(string documentId, string content)
        {
            var selection = Registry.GetSelection(documentId);
            var document  = RequireRegisteredDocument(documentId);
            var text      = GetDocumentText(document);
            var cursor    = Math.Clamp(selection.CursorPosition, 0, text.Length);
            var inserted  = content ?? string.Empty;
            var updated   = text.Insert(cursor, inserted);
            ApplyTextToDocument(document, updated);
            var newSel    = Registry.SetSelection(documentId, cursor + inserted.Length, cursor + inserted.Length);
            return EditSuccess(document, "Canvas content inserted at cursor.", newSel);
        }

        public CanvasEditResult DeleteSelection(string documentId)
        {
            var selection = Registry.GetSelection(documentId);
            var document  = RequireRegisteredDocument(documentId);
            var content   = GetDocumentText(document);
            var s = Math.Clamp(selection.StartIndex, 0, content.Length);
            var e = Math.Clamp(selection.EndIndex,   s, content.Length);
            var updated   = string.Concat(content.AsSpan(0, s), content.AsSpan(e));
            ApplyTextToDocument(document, updated);
            var newSel    = Registry.SetSelection(documentId, s, s);
            return EditSuccess(document, "Canvas selection deleted.", newSel);
        }

        public CanvasEditResult WrapSelection(string documentId, string prefix, string suffix)
        {
            var selection = Registry.GetSelection(documentId);
            var document  = RequireRegisteredDocument(documentId);
            var content   = GetDocumentText(document);
            var s = Math.Clamp(selection.StartIndex, 0, content.Length);
            var e = Math.Clamp(selection.EndIndex,   s, content.Length);
            var wrapped   = $"{prefix ?? string.Empty}{content.Substring(s, e - s)}{suffix ?? string.Empty}";
            var updated   = string.Concat(content.AsSpan(0, s), wrapped, content.AsSpan(e));
            ApplyTextToDocument(document, updated);
            var newSel    = Registry.SetSelection(documentId, s, s + wrapped.Length, selection.ActiveBlockId, selection.ActiveBlockType);
            return EditSuccess(document, "Canvas selection wrapped.", newSel);
        }

        public CanvasEditResult ModifySelection(string documentId, string instruction)
        {
            var selection = Registry.GetSelection(documentId);
            var document  = RequireRegisteredDocument(documentId);
            var modified  = ApplyInstruction(selection.SelectedText, instruction, out var matched);
            if (!matched)
                return EditResult(false, "No deterministic Canvas selection modifier matched the instruction.", document, selection);
            return ReplaceSelection(documentId, modified);
        }

        public CanvasEditResult ModifyDocument(string documentId, string instruction)
        {
            var document = RequireRegisteredDocument(documentId);
            var content  = GetDocumentText(document);
            var modified = ApplyInstruction(content, instruction, out var matched);
            if (!matched)
                return EditResult(false, "No deterministic Canvas document modifier matched the instruction.", document, Registry.GetSelection(documentId));
            ApplyTextToDocument(document, modified);
            var newSel = Registry.SetSelection(documentId, modified.Length, modified.Length);
            return EditSuccess(document, "Canvas document modified.", newSel);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string GetDocumentText(CanvasDocument document)
        {
            return string.Join(Environment.NewLine,
                document.OrderedBlocks.Select(GetBlockText).Where(t => t.Length > 0));
        }

        private static string GetBlockText(CanvasBlock block) => block switch
        {
            CanvasTextBlock    t  => t.Content  ?? string.Empty,
            CanvasHeadingBlock h  => h.Content  ?? string.Empty,
            CanvasCodeBlock    c  => c.Content  ?? string.Empty,
            CanvasChatBlock    ch => ch.Content ?? string.Empty,
            _ => string.Empty
        };

        private void ApplyTextToDocument(CanvasDocument document, string text)
        {
            foreach (var block in document.OrderedBlocks.ToList())
                DocumentService.RemoveBlockAsync(block.Id).GetAwaiter().GetResult();
            DocumentService.AddBlockAsync(new CanvasTextBlock { Content = text }).GetAwaiter().GetResult();
            document.UpdatedAt = DateTime.UtcNow;
            Registry.MarkDocumentDirty(document.Id);
        }

        private static CanvasDocument RequireRegisteredDocument(string documentId)
        {
            // First try the live document service, fall back to registry.
            var fromService = DocumentService.OpenDocuments
                .FirstOrDefault(d => d.Id.Equals(documentId?.Trim() ?? "", StringComparison.OrdinalIgnoreCase));
            if (fromService is not null) return fromService;

            throw new InvalidOperationException($"Canvas document '{documentId}' was not found.");
        }

        private static CanvasActionResult DocumentSuccess(string message, CanvasDocument document)
        {
            return new CanvasActionResult
            {
                Success    = true,
                Message    = message,
                DocumentId = document.Id,
                Documents  = { Registry.ToDocumentRef(document) }
            };
        }

        private static CanvasContentResult ContentSuccess(string message, string documentId, string content)
        {
            return new CanvasContentResult
            {
                Success    = true,
                Message    = message,
                DocumentId = documentId,
                Content    = content ?? string.Empty
            };
        }

        private static CanvasSaveResult ToCanvasSaveResult(CanvasDocument document, CanvasDocumentSaveResult saveResult)
        {
            var savedAt = DateTime.UtcNow;
            if (saveResult.IsSuccess) Registry.MarkDocumentSaved(document.Id, saveResult.SavedPath, savedAt);
            return new CanvasSaveResult
            {
                Success    = saveResult.IsSuccess,
                Message    = saveResult.Message,
                DocumentId = document.Id,
                Path       = saveResult.SavedPath ?? string.Empty,
                SavedAt    = saveResult.IsSuccess ? savedAt : null
            };
        }

        private static CanvasEditResult EditSuccess(CanvasDocument document, string message, CanvasSelectionState selection)
            => EditResult(true, message, document, selection);

        private static CanvasEditResult EditResult(bool success, string message, CanvasDocument document, CanvasSelectionState selection)
        {
            return new CanvasEditResult
            {
                Success    = success,
                Message    = message,
                DocumentId = document.Id,
                Content    = GetDocumentText(document),
                Selection  = selection
            };
        }

        private static string ApplyInstruction(string content, string instruction, out bool matched)
        {
            var norm = instruction?.Trim() ?? string.Empty;
            matched = true;
            if (norm.Contains("uppercase", StringComparison.OrdinalIgnoreCase)) return (content ?? string.Empty).ToUpperInvariant();
            if (norm.Contains("lowercase", StringComparison.OrdinalIgnoreCase)) return (content ?? string.Empty).ToLowerInvariant();
            if (norm.Contains("trim",      StringComparison.OrdinalIgnoreCase)) return (content ?? string.Empty).Trim();
            if (norm.Contains("bullet",    StringComparison.OrdinalIgnoreCase))
                return string.Join(Environment.NewLine,
                    (content ?? string.Empty)
                        .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => $"- {l.Trim()}"));
            matched = false;
            return content ?? string.Empty;
        }
    }
}
