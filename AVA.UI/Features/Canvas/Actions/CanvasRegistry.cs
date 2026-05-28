using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AVA.UI.Features.Canvas.Actions.Models;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Actions
{
    /// <summary>
    /// Tracks Canvas tool runtime metadata that is not owned by the Canvas document service.
    /// </summary>
    public sealed class CanvasRegistry
    {
        private readonly ConcurrentDictionary<string, CanvasDocument> _documents =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, string> _documentPaths =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, DateTime> _documentSavedAt =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, bool> _documentDirty =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, CanvasSelectionState> _documentSelections =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Gets the active Canvas document identifier.</summary>
        public string ActiveDocumentId { get; private set; } = string.Empty;

        public CanvasDocument RegisterDocument(CanvasDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            _documents[document.Id] = document;
            _documentDirty.TryAdd(document.Id, false);
            _documentSelections.TryAdd(document.Id, CreateSelection(document, 0, 0));
            ActiveDocumentId = document.Id;
            return document;
        }

        public bool UnregisterDocument(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId)) return false;
            var id = documentId.Trim();
            var removed = _documents.TryRemove(id, out _);
            _documentPaths.TryRemove(id, out _);
            _documentSavedAt.TryRemove(id, out _);
            _documentDirty.TryRemove(id, out _);
            _documentSelections.TryRemove(id, out _);
            if (ActiveDocumentId.Equals(id, StringComparison.OrdinalIgnoreCase))
                ActiveDocumentId = _documents.Keys.FirstOrDefault() ?? string.Empty;
            return removed;
        }

        public List<CanvasDocumentRef> ListOpenDocuments()
        {
            return _documents.Values
                .OrderByDescending(d => ActiveDocumentId.Equals(d.Id, StringComparison.OrdinalIgnoreCase))
                .ThenBy(d => d.CreatedAt)
                .Select(ToDocumentRef)
                .ToList();
        }

        public void SetActiveDocument(CanvasDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            _documents[document.Id] = document;
            ActiveDocumentId = document.Id;
        }

        public CanvasSelectionState GetSelection(string documentId)
        {
            var document = RequireDocument(documentId);
            return CloneSelection(ResolveSelection(document), "Canvas selection returned.");
        }

        public CanvasSelectionState SetSelection(
            string documentId, int startIndex, int endIndex,
            string activeBlockId = "", string activeBlockType = "")
        {
            var document = RequireDocument(documentId);
            var selection = CreateSelection(document, startIndex, endIndex, activeBlockId, activeBlockType, "Canvas selection set.");
            _documentSelections[document.Id] = selection;
            return CloneSelection(selection);
        }

        public CanvasSelectionState ClearSelection(string documentId)
        {
            var document = RequireDocument(documentId);
            var previous = ResolveSelection(document);
            var cursor = Math.Clamp(previous.CursorPosition, 0, (GetDocumentText(document)).Length);
            var selection = CreateSelection(document, cursor, cursor, message: "Canvas selection cleared.");
            _documentSelections[document.Id] = selection;
            return CloneSelection(selection);
        }

        public CanvasContextResult GetSelectionContext(string documentId, int beforeChars = 500, int afterChars = 500)
        {
            var document = RequireDocument(documentId);
            var content = GetDocumentText(document);
            var selection = ResolveSelection(document);
            var anchorStart = selection.StartIndex == selection.EndIndex ? selection.CursorPosition : selection.StartIndex;
            var anchorEnd   = selection.StartIndex == selection.EndIndex ? selection.CursorPosition : selection.EndIndex;
            var start = Math.Clamp(anchorStart - Math.Max(0, beforeChars), 0, content.Length);
            var end   = Math.Clamp(anchorEnd   + Math.Max(0, afterChars),  0, content.Length);
            return CreateContextResult(document, start, end, content, "Canvas selection context returned.", CloneSelection(selection));
        }

        public CanvasContextResult GetDocumentContext(string documentId, int maxChars = 4000)
        {
            var document = RequireDocument(documentId);
            var content  = GetDocumentText(document);
            var selection = ResolveSelection(document);
            var safeMax  = Math.Max(1, maxChars);
            if (content.Length <= safeMax)
                return CreateContextResult(document, 0, content.Length, content, "Canvas document context returned.", CloneSelection(selection));
            var half   = safeMax / 2;
            var cursor = Math.Clamp(selection.CursorPosition, 0, content.Length);
            var start  = Math.Clamp(cursor - half, 0, Math.Max(0, content.Length - safeMax));
            var end    = Math.Clamp(start + safeMax, 0, content.Length);
            return CreateContextResult(document, start, end, content, "Canvas document context returned.", CloneSelection(selection));
        }

        public CanvasContextResult GetBlockContext(string documentId, string blockId)
        {
            var document = RequireDocument(documentId);
            var block = document.Blocks.FirstOrDefault(b => b.Id.Equals(blockId, StringComparison.OrdinalIgnoreCase));
            if (block == null) throw new InvalidOperationException($"Canvas block '{blockId}' was not found.");
            var blockContent = GetBlockContent(block);
            return new CanvasContextResult
            {
                Success = true, Message = "Canvas block context returned.",
                DocumentId = document.Id, BlockId = block.Id,
                StartIndex = 0, EndIndex = blockContent.Length,
                Context = blockContent,
                Selection = CloneSelection(ResolveSelection(document)),
                Metadata = new Dictionary<string, string>
                {
                    ["blockType"]  = block.BlockType.ToString(),
                    ["sortOrder"]  = block.SortOrder.ToString(),
                    ["createdAt"]  = block.CreatedAt.ToString("O"),
                    ["updatedAt"]  = block.UpdatedAt.ToString("O")
                }
            };
        }

        public CanvasDocumentRef ToDocumentRef(CanvasDocument document)
        {
            return new CanvasDocumentRef
            {
                DocumentId = document.Id,
                Title      = document.Title,
                Path       = _documentPaths.TryGetValue(document.Id, out var path) ? path : string.Empty,
                CreatedAt  = document.CreatedAt,
                UpdatedAt  = document.UpdatedAt,
                SavedAt    = _documentSavedAt.TryGetValue(document.Id, out var savedAt) ? savedAt : null,
                IsActive   = ActiveDocumentId.Equals(document.Id, StringComparison.OrdinalIgnoreCase),
                IsDirty    = _documentDirty.TryGetValue(document.Id, out var dirty) && dirty
            };
        }

        public void TrackDocumentPath(string documentId, string? path)
        {
            if (!string.IsNullOrWhiteSpace(documentId) && !string.IsNullOrWhiteSpace(path))
                _documentPaths[documentId.Trim()] = path.Trim();
        }

        public void MarkDocumentDirty(string documentId)
        {
            if (!string.IsNullOrWhiteSpace(documentId))
                _documentDirty[documentId.Trim()] = true;
        }

        public void MarkDocumentSaved(string documentId, string? path, DateTime savedAt)
        {
            if (string.IsNullOrWhiteSpace(documentId)) return;
            var id = documentId.Trim();
            if (!string.IsNullOrWhiteSpace(path)) _documentPaths[id] = path.Trim();
            _documentSavedAt[id] = savedAt;
            _documentDirty[id]   = false;
        }

        public CanvasDocumentState ToDocumentState(CanvasDocument document)
        {
            return new CanvasDocumentState
            {
                Success    = true, Message = "Canvas document state returned.",
                DocumentId = document.Id, Title = document.Title,
                Content    = GetDocumentText(document),
                Path       = _documentPaths.TryGetValue(document.Id, out var path) ? path : string.Empty,
                CreatedAt  = document.CreatedAt, UpdatedAt = document.UpdatedAt,
                SavedAt    = _documentSavedAt.TryGetValue(document.Id, out var savedAt) ? savedAt : null,
                IsDirty    = _documentDirty.TryGetValue(document.Id, out var dirty) && dirty,
                Document   = document,
                Metadata   = new Dictionary<string, string>
                {
                    ["blockCount"]    = document.Blocks.Count.ToString(),
                    ["contentLength"] = GetDocumentText(document).Length.ToString()
                }
            };
        }

        public CanvasSelectionState NormalizeSelection(CanvasDocument document)
            => NormalizeSelection(document, ResolveSelection(document));

        private CanvasDocument RequireDocument(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId) ||
                !_documents.TryGetValue(documentId.Trim(), out var document))
                throw new InvalidOperationException($"Canvas document '{documentId}' was not found.");
            return document;
        }

        private CanvasSelectionState ResolveSelection(CanvasDocument document)
        {
            if (_documentSelections.TryGetValue(document.Id, out var selection))
                return NormalizeSelection(document, selection);
            var contentLength = (GetDocumentText(document)).Length;
            selection = CreateSelection(document, contentLength, contentLength);
            _documentSelections[document.Id] = selection;
            return selection;
        }

        private CanvasSelectionState NormalizeSelection(CanvasDocument document, CanvasSelectionState selection)
        {
            var normalized = CreateSelection(document, selection.StartIndex, selection.EndIndex,
                selection.ActiveBlockId, selection.ActiveBlockType, selection.Message);
            _documentSelections[document.Id] = normalized;
            return normalized;
        }

        private CanvasSelectionState CreateSelection(
            CanvasDocument document, int startIndex, int endIndex,
            string activeBlockId = "", string activeBlockType = "",
            string message = "Canvas selection returned.")
        {
            var content = GetDocumentText(document);
            var start = Math.Clamp(startIndex, 0, content.Length);
            var end   = Math.Clamp(endIndex,   0, content.Length);
            if (start > end) (start, end) = (end, start);
            return new CanvasSelectionState
            {
                Success = true, Message = message,
                DocumentId = document.Id,
                StartIndex = start, EndIndex = end,
                CursorPosition = end,
                SelectedText   = content.Substring(start, end - start),
                ActiveBlockId  = activeBlockId  ?? string.Empty,
                ActiveBlockType = activeBlockType ?? string.Empty,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private CanvasContextResult CreateContextResult(
            CanvasDocument document, int start, int end,
            string content, string message, CanvasSelectionState selection)
        {
            return new CanvasContextResult
            {
                Success = true, Message = message,
                DocumentId = document.Id,
                StartIndex = start, EndIndex = end,
                Context    = content.Substring(start, end - start),
                Selection  = selection,
                Metadata   = new Dictionary<string, string>
                {
                    ["documentTitle"] = document.Title,
                    ["contentLength"] = content.Length.ToString(),
                    ["isTruncated"]   = (start > 0 || end < content.Length).ToString()
                }
            };
        }

        internal static string GetDocumentText(CanvasDocument document)
        {
            return string.Join(Environment.NewLine,
                document.OrderedBlocks.Select(GetBlockContent).Where(t => t.Length > 0));
        }

        private static string GetBlockContent(CanvasBlock block) => block switch
        {
            CanvasTextBlock    t  => t.Content  ?? string.Empty,
            CanvasHeadingBlock h  => h.Content  ?? string.Empty,
            CanvasCodeBlock    c  => c.Content  ?? string.Empty,
            CanvasChatBlock    ch => ch.Content ?? string.Empty,
            _ => string.Empty
        };

        private static CanvasSelectionState CloneSelection(CanvasSelectionState s, string? message = null)
        {
            return new CanvasSelectionState
            {
                Success = s.Success, Message = message ?? s.Message,
                DocumentId = s.DocumentId,
                StartIndex = s.StartIndex, EndIndex = s.EndIndex,
                CursorPosition = s.CursorPosition, SelectedText = s.SelectedText,
                ActiveBlockId = s.ActiveBlockId, ActiveBlockType = s.ActiveBlockType,
                UpdatedAt = s.UpdatedAt
            };
        }
    }
}
