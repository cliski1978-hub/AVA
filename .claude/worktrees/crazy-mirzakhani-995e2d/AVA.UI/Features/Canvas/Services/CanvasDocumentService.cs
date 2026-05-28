using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Manages the active Canvas document and in-memory block updates for Sprint 2.
    /// </summary>
    public sealed class CanvasDocumentService : ICanvasDocumentService
    {
        private readonly Dictionary<string, CanvasDocument> _documents =
            new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public IReadOnlyList<CanvasDocument> OpenDocuments =>
            _documents.Values
                .OrderBy(document => document.UpdatedAt)
                .ThenBy(document => document.CreatedAt)
                .ToList();

        /// <inheritdoc />
        public CanvasDocument? ActiveDocument { get; private set; }

        /// <inheritdoc />
        public event Action? ActiveDocumentChanged;

        /// <inheritdoc />
        public Task<CanvasDocument> CreateDocumentAsync(string title)
        {
            var document = new CanvasDocument
            {
                Title = string.IsNullOrWhiteSpace(title) ? "Untitled Canvas" : title.Trim()
            };

            _documents[document.Id] = document;
            ActiveDocument = document;
            NotifyChanged();

            return Task.FromResult(document);
        }

        /// <inheritdoc />
        public Task SetActiveDocumentAsync(CanvasDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            _documents[document.Id] = document;
            ActiveDocument = document;
            NotifyChanged();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CloseDocumentAsync(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId))
            {
                return Task.CompletedTask;
            }

            _documents.Remove(documentId.Trim());

            if (ActiveDocument != null
                && ActiveDocument.Id.Equals(documentId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ActiveDocument = OpenDocuments.LastOrDefault();
            }

            NotifyChanged();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<CanvasDocument> DuplicateDocumentAsync(string documentId)
        {
            if (!_documents.TryGetValue(documentId, out var sourceDocument))
            {
                throw new InvalidOperationException($"Canvas document '{documentId}' was not found.");
            }

            var copy = CloneDocument(sourceDocument, $"{sourceDocument.Title} Copy");
            _documents[copy.Id] = copy;
            ActiveDocument = copy;
            NotifyChanged();

            return Task.FromResult(copy);
        }

        /// <inheritdoc />
        public Task AddBlockAsync(CanvasBlock block)
        {
            ArgumentNullException.ThrowIfNull(block);

            var document = RequireActiveDocument();
            block.SortOrder = ResolveSortOrder(document, block.SortOrder);
            block.UpdatedAt = DateTime.UtcNow;
            document.Blocks.Add(block);
            document.UpdatedAt = DateTime.UtcNow;
            NormalizeSortOrder(document);
            NotifyChanged();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RemoveBlockAsync(string blockId)
        {
            var document = RequireActiveDocument();
            var block = FindBlock(document, blockId);
            if (block == null)
            {
                return Task.CompletedTask;
            }

            document.Blocks.Remove(block);
            document.UpdatedAt = DateTime.UtcNow;
            NormalizeSortOrder(document);
            NotifyChanged();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpdateTextBlockAsync(string blockId, string text)
        {
            var document = RequireActiveDocument();
            var block = FindBlock(document, blockId)
                ?? throw new InvalidOperationException($"Canvas block '{blockId}' was not found.");

            switch (block)
            {
                case CanvasTextBlock textBlock:
                    textBlock.Content = text ?? string.Empty;
                    break;
                case CanvasHeadingBlock headingBlock:
                    headingBlock.Content = text ?? string.Empty;
                    break;
                case CanvasCodeBlock codeBlock:
                    codeBlock.Content = text ?? string.Empty;
                    break;
                case CanvasChatBlock chatBlock:
                    chatBlock.Content = text ?? string.Empty;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Canvas block '{blockId}' does not support text content updates.");
            }

            block.UpdatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
            NotifyChanged();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task MoveBlockAsync(string blockId, int newSortOrder)
        {
            var document = RequireActiveDocument();
            var block = FindBlock(document, blockId)
                ?? throw new InvalidOperationException($"Canvas block '{blockId}' was not found.");

            block.SortOrder = Math.Max(0, newSortOrder);
            block.UpdatedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
            NormalizeSortOrder(document);
            NotifyChanged();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task TransformBlockAsync(string blockId, CanvasBlockType targetType, string? text = null)
        {
            var document = RequireActiveDocument();
            var block = FindBlock(document, blockId)
                ?? throw new InvalidOperationException($"Canvas block '{blockId}' was not found.");

            if (block.BlockType == targetType && text == null)
            {
                return Task.CompletedTask;
            }

            var replacement = CreateBlock(targetType, text ?? GetBlockText(block));
            replacement.Id = block.Id;
            replacement.SortOrder = block.SortOrder;
            replacement.CreatedAt = block.CreatedAt;
            replacement.UpdatedAt = DateTime.UtcNow;

            var blockIndex = document.Blocks.FindIndex(existing =>
                existing.Id.Equals(block.Id, StringComparison.OrdinalIgnoreCase));
            document.Blocks[blockIndex] = replacement;
            document.UpdatedAt = DateTime.UtcNow;
            NotifyChanged();

            return Task.CompletedTask;
        }

        private CanvasDocument RequireActiveDocument()
        {
            return ActiveDocument
                ?? throw new InvalidOperationException("No active Canvas document is loaded.");
        }

        private static CanvasBlock? FindBlock(CanvasDocument document, string blockId)
        {
            if (string.IsNullOrWhiteSpace(blockId))
            {
                return null;
            }

            return document.Blocks.FirstOrDefault(block =>
                block.Id.Equals(blockId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static int ResolveSortOrder(CanvasDocument document, int requestedSortOrder)
        {
            if (requestedSortOrder > 0 || document.Blocks.Count == 0)
            {
                return requestedSortOrder > 0
                    ? requestedSortOrder
                    : 0;
            }

            return document.Blocks.Max(block => block.SortOrder) + 1;
        }

        private static void NormalizeSortOrder(CanvasDocument document)
        {
            var ordered = document.Blocks
                .OrderBy(block => block.SortOrder)
                .ThenBy(block => block.CreatedAt)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].SortOrder = i;
            }
        }

        private static string GetBlockText(CanvasBlock block)
        {
            return block switch
            {
                CanvasTextBlock textBlock => textBlock.Content,
                CanvasHeadingBlock headingBlock => headingBlock.Content,
                CanvasCodeBlock codeBlock => codeBlock.Content,
                CanvasChatBlock chatBlock => chatBlock.Content,
                _ => string.Empty
            };
        }

        private static CanvasBlock CreateBlock(CanvasBlockType targetType, string text)
        {
            return targetType switch
            {
                CanvasBlockType.Heading => new CanvasHeadingBlock
                {
                    Content = text,
                    Level = 1
                },
                CanvasBlockType.Code => new CanvasCodeBlock
                {
                    Content = text,
                    Language = "text"
                },
                CanvasBlockType.Chat => new CanvasChatBlock
                {
                    Content = text,
                    Role = "AVA"
                },
                _ => new CanvasTextBlock
                {
                    Content = text
                }
            };
        }

        private static CanvasDocument CloneDocument(CanvasDocument sourceDocument, string title)
        {
            var copy = new CanvasDocument
            {
                Title = title
            };

            copy.Blocks = sourceDocument.OrderedBlocks
                .Select(CloneBlock)
                .ToList();
            copy.UpdatedAt = DateTime.UtcNow;
            return copy;
        }

        private static CanvasBlock CloneBlock(CanvasBlock block)
        {
            return block switch
            {
                CanvasHeadingBlock headingBlock => new CanvasHeadingBlock
                {
                    Content = headingBlock.Content,
                    Level = headingBlock.Level,
                    SortOrder = headingBlock.SortOrder
                },
                CanvasCodeBlock codeBlock => new CanvasCodeBlock
                {
                    Content = codeBlock.Content,
                    Language = codeBlock.Language,
                    SortOrder = codeBlock.SortOrder
                },
                CanvasChatBlock chatBlock => new CanvasChatBlock
                {
                    Content = chatBlock.Content,
                    Role = chatBlock.Role,
                    SortOrder = chatBlock.SortOrder
                },
                CanvasTextBlock textBlock => new CanvasTextBlock
                {
                    Content = textBlock.Content,
                    SortOrder = textBlock.SortOrder
                },
                _ => throw new InvalidOperationException($"Unsupported canvas block type '{block.BlockType}'.")
            };
        }

        private void NotifyChanged()
        {
            ActiveDocumentChanged?.Invoke();
        }
    }
}
