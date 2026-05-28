using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AVA.UI.CORE.Models;
using AVA.UI.CORE.Models.UI;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Converts AVA chat output into structured Canvas documents using simple block parsing rules.
    /// </summary>
    public sealed class CanvasInteractionService : ICanvasInteractionService
    {
        private readonly ICanvasDocumentService _canvasDocumentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CanvasInteractionService"/> class.
        /// </summary>
        /// <param name="canvasDocumentService">The Canvas document state service.</param>
        public CanvasInteractionService(ICanvasDocumentService canvasDocumentService)
        {
            _canvasDocumentService = canvasDocumentService;
        }

        /// <inheritdoc />
        public async Task<CanvasDocument> CreateDocumentFromResponseAsync(
            string title,
            string responseText,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = await _canvasDocumentService.CreateDocumentAsync(title);
            await AppendBlocksAsync(ParseResponseToBlocks(responseText), cancellationToken);
            return document;
        }

        /// <inheritdoc />
        public Task<CanvasDocument> CreateDocumentFromMessageAsync(
            string title,
            Message message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            return CreateDocumentFromResponseAsync(title, message.Content, cancellationToken);
        }

        /// <inheritdoc />
        public Task<CanvasDocument> CreateDocumentFromOutputSegmentAsync(
            string title,
            OutputSegment segment,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(segment);
            return CreateDocumentFromResponseAsync(title, segment.Value, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<CanvasDocument> AppendResponseToActiveDocumentAsync(
            string responseText,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = _canvasDocumentService.ActiveDocument
                ?? await _canvasDocumentService.CreateDocumentAsync("AVA Response Canvas");

            await AppendBlocksAsync(ParseResponseToBlocks(responseText), cancellationToken);
            return document;
        }

        /// <inheritdoc />
        public Task<CanvasDocument> AppendMessageToActiveDocumentAsync(
            Message message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            return AppendResponseToActiveDocumentAsync(message.Content, cancellationToken);
        }

        /// <inheritdoc />
        public Task<CanvasDocument> AppendOutputSegmentToActiveDocumentAsync(
            OutputSegment segment,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(segment);
            return AppendResponseToActiveDocumentAsync(segment.Value, cancellationToken);
        }

        /// <inheritdoc />
        public IReadOnlyList<CanvasBlock> ParseResponseToBlocks(string responseText)
        {
            var normalizedText = NormalizeLineEndings(responseText);
            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                return Array.Empty<CanvasBlock>();
            }

            var lines = normalizedText.Split('\n');
            var blocks = new List<CanvasBlock>();
            var paragraphBuilder = new StringBuilder();
            var codeBuilder = new StringBuilder();
            var inCodeFence = false;
            var codeLanguage = "text";
            var sortOrder = 0;

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd('\r');
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("```", StringComparison.Ordinal))
                {
                    FlushParagraph(blocks, paragraphBuilder, ref sortOrder);

                    if (inCodeFence)
                    {
                        blocks.Add(new CanvasCodeBlock
                        {
                            Content = TrimTrailingNewLine(codeBuilder.ToString()),
                            Language = string.IsNullOrWhiteSpace(codeLanguage) ? "text" : codeLanguage,
                            SortOrder = sortOrder++
                        });

                        codeBuilder.Clear();
                        codeLanguage = "text";
                        inCodeFence = false;
                    }
                    else
                    {
                        codeLanguage = trimmedLine.Length > 3
                            ? trimmedLine[3..].Trim()
                            : "text";
                        inCodeFence = true;
                    }

                    continue;
                }

                if (inCodeFence)
                {
                    codeBuilder.AppendLine(line);
                    continue;
                }

                if (trimmedLine.StartsWith("# ", StringComparison.Ordinal))
                {
                    FlushParagraph(blocks, paragraphBuilder, ref sortOrder);
                    blocks.Add(new CanvasHeadingBlock
                    {
                        Content = trimmedLine[2..].Trim(),
                        Level = 1,
                        SortOrder = sortOrder++
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    FlushParagraph(blocks, paragraphBuilder, ref sortOrder);
                    continue;
                }

                if (paragraphBuilder.Length > 0)
                {
                    paragraphBuilder.AppendLine();
                }

                paragraphBuilder.Append(trimmedLine);
            }

            if (inCodeFence)
            {
                blocks.Add(new CanvasCodeBlock
                {
                    Content = TrimTrailingNewLine(codeBuilder.ToString()),
                    Language = string.IsNullOrWhiteSpace(codeLanguage) ? "text" : codeLanguage,
                    SortOrder = sortOrder++
                });
            }

            FlushParagraph(blocks, paragraphBuilder, ref sortOrder);
            return blocks;
        }

        private async Task AppendBlocksAsync(
            IEnumerable<CanvasBlock> blocks,
            CancellationToken cancellationToken)
        {
            foreach (var block in blocks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _canvasDocumentService.AddBlockAsync(block);
            }
        }

        private static void FlushParagraph(
            ICollection<CanvasBlock> blocks,
            StringBuilder paragraphBuilder,
            ref int sortOrder)
        {
            if (paragraphBuilder.Length == 0)
            {
                return;
            }

            blocks.Add(new CanvasTextBlock
            {
                Content = paragraphBuilder.ToString().Trim(),
                SortOrder = sortOrder++
            });

            paragraphBuilder.Clear();
        }

        private static string NormalizeLineEndings(string value)
            => (value ?? string.Empty).Replace("\r\n", "\n", StringComparison.Ordinal);

        private static string TrimTrailingNewLine(string value)
            => value.TrimEnd('\r', '\n');
    }
}
