using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Provides Canvas document persistence behaviour.
    /// </summary>
    public sealed class CanvasDocumentPersistenceService : ICanvasDocumentPersistenceService
    {
        /// <inheritdoc />
        public Task<CanvasDocumentSaveResult> SaveDocumentAsync(
            CanvasDocument document,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(CanvasDocumentSaveResult.Stubbed(
                "Save prepared; filesystem document storage is not implemented yet."));
        }

        /// <inheritdoc />
        public Task<CanvasDocumentSaveResult> SaveDocumentAsAsync(
            CanvasDocument document,
            string path,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("A save path is required.");

            var normalizedPath = path.Trim();
            var directory = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            // Serialize block content to plain text for file output.
            var content = string.Join(Environment.NewLine + Environment.NewLine,
                document.OrderedBlocks.Select(GetBlockText).Where(t => !string.IsNullOrWhiteSpace(t)));

            File.WriteAllText(normalizedPath, content);
            return Task.FromResult(CanvasDocumentSaveResult.Success(normalizedPath));
        }

        private static string GetBlockText(CanvasBlock block) => block switch
        {
            CanvasTextBlock    t  => t.Content  ?? string.Empty,
            CanvasHeadingBlock h  => h.Content  ?? string.Empty,
            CanvasCodeBlock    c  => c.Content  ?? string.Empty,
            CanvasChatBlock    ch => ch.Content ?? string.Empty,
            _ => string.Empty
        };
    }
}
