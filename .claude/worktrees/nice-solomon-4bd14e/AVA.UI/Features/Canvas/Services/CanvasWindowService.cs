using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.UI.Features.Canvas.Models;
using AVA.UI.Features.Canvas.Tools;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Prepares Canvas pop-out windows and materializes tool-driven Canvas documents.
    /// </summary>
    public sealed class CanvasWindowService : ICanvasWindowService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly ICanvasDocumentService _canvasDocumentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CanvasWindowService"/> class.
        /// </summary>
        /// <param name="canvasDocumentService">The Canvas document state service.</param>
        public CanvasWindowService(ICanvasDocumentService canvasDocumentService)
        {
            _canvasDocumentService = canvasDocumentService;
        }

        /// <inheritdoc />
        public string ToolName => CanvasToolNames.CanvasWindowOpen;

        /// <inheritdoc />
        public CanvasWindowOpenRequest CreateRequest(CanvasDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);

            return new CanvasWindowOpenRequest
            {
                DocumentId = document.Id,
                Title = string.IsNullOrWhiteSpace(document.Title) ? "Untitled Canvas" : document.Title,
                InitialBlocks = document.OrderedBlocks
                    .Select(ToSeed)
                    .ToList(),
                Meta =
                {
                    ["source"] = "canvas",
                    ["documentId"] = document.Id
                },
                Formatting =
                {
                    ["layout"] = "document"
                }
            };
        }

        /// <inheritdoc />
        public Task<CanvasWindowDescriptor> PrepareWindowAsync(
            CanvasWindowOpenRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var encodedRequest = EncodeRequest(request);
            return Task.FromResult(new CanvasWindowDescriptor
            {
                EncodedRequest = encodedRequest,
                Url = $"/canvas?mode=window&open={Uri.EscapeDataString(encodedRequest)}",
                Title = string.IsNullOrWhiteSpace(request.Title) ? "Canvas" : request.Title
            });
        }

        /// <inheritdoc />
        public CanvasWindowOpenRequest? TryParseRequest(string? encodedRequest)
        {
            if (string.IsNullOrWhiteSpace(encodedRequest))
            {
                return null;
            }

            try
            {
                var rawValue = Uri.UnescapeDataString(encodedRequest);
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(rawValue));
                return JsonSerializer.Deserialize<CanvasWindowOpenRequest>(json, SerializerOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<CanvasDocument> OpenFromToolAsync(
            CanvasWindowOpenRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var document = await _canvasDocumentService.CreateDocumentAsync(request.Title);

            var seedBlocks = request.InitialBlocks.Any()
                ? request.InitialBlocks.AsEnumerable()
                : BuildFallbackBlocks(request).AsEnumerable();

            foreach (var seed in seedBlocks.OrderBy(block => block.SortOrder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _canvasDocumentService.AddBlockAsync(ToBlock(seed));
            }

            return document;
        }

        private static string EncodeRequest(CanvasWindowOpenRequest request)
        {
            var json = JsonSerializer.Serialize(request, SerializerOptions);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        private static CanvasWindowBlockSeed ToSeed(CanvasBlock block)
            => block switch
            {
                CanvasHeadingBlock headingBlock => new CanvasWindowBlockSeed
                {
                    Type = CanvasBlockType.Heading,
                    Content = headingBlock.Content,
                    SortOrder = headingBlock.SortOrder,
                    Level = headingBlock.Level
                },
                CanvasCodeBlock codeBlock => new CanvasWindowBlockSeed
                {
                    Type = CanvasBlockType.Code,
                    Content = codeBlock.Content,
                    SortOrder = codeBlock.SortOrder,
                    Language = codeBlock.Language
                },
                CanvasChatBlock chatBlock => new CanvasWindowBlockSeed
                {
                    Type = CanvasBlockType.Chat,
                    Content = chatBlock.Content,
                    SortOrder = chatBlock.SortOrder,
                    Role = chatBlock.Role
                },
                CanvasTextBlock textBlock => new CanvasWindowBlockSeed
                {
                    Type = CanvasBlockType.Text,
                    Content = textBlock.Content,
                    SortOrder = textBlock.SortOrder
                },
                _ => new CanvasWindowBlockSeed
                {
                    Type = block.BlockType,
                    SortOrder = block.SortOrder
                }
            };

        private static CanvasBlock ToBlock(CanvasWindowBlockSeed seed)
            => seed.Type switch
            {
                CanvasBlockType.Heading => new CanvasHeadingBlock
                {
                    Content = seed.Content,
                    Level = seed.Level.GetValueOrDefault(1),
                    SortOrder = seed.SortOrder
                },
                CanvasBlockType.Code => new CanvasCodeBlock
                {
                    Content = seed.Content,
                    Language = string.IsNullOrWhiteSpace(seed.Language) ? "text" : seed.Language,
                    SortOrder = seed.SortOrder
                },
                CanvasBlockType.Chat => new CanvasChatBlock
                {
                    Content = seed.Content,
                    Role = string.IsNullOrWhiteSpace(seed.Role) ? "assistant" : seed.Role,
                    SortOrder = seed.SortOrder
                },
                _ => new CanvasTextBlock
                {
                    Content = seed.Content,
                    SortOrder = seed.SortOrder
                }
            };

        private static CanvasWindowBlockSeed[] BuildFallbackBlocks(CanvasWindowOpenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return new[]
                {
                    new CanvasWindowBlockSeed
                    {
                        Type = CanvasBlockType.Text,
                        Content = "New Canvas window opened from tool call.",
                        SortOrder = 0
                    }
                };
            }

            var blockType = request.Formatting.TryGetValue("blockType", out var blockTypeText)
                && Enum.TryParse<CanvasBlockType>(blockTypeText, true, out var parsedType)
                    ? parsedType
                    : CanvasBlockType.Text;

            return new[]
            {
                new CanvasWindowBlockSeed
                {
                    Type = blockType,
                    Content = request.Content,
                    SortOrder = 0,
                    Language = request.Formatting.TryGetValue("language", out var language) ? language : null,
                    Role = request.Formatting.TryGetValue("role", out var role) ? role : null,
                    Level = request.Formatting.TryGetValue("level", out var levelValue)
                        && int.TryParse(levelValue, out var level)
                            ? level
                            : null
                }
            };
        }
    }
}
