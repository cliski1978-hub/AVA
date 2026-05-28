using System.Threading;
using System.Threading.Tasks;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Defines persistence operations for Canvas documents.
    /// </summary>
    public interface ICanvasDocumentPersistenceService
    {
        /// <summary>Saves a Canvas document to durable storage.</summary>
        Task<CanvasDocumentSaveResult> SaveDocumentAsync(
            CanvasDocument document,
            CancellationToken cancellationToken = default);

        /// <summary>Saves a Canvas document to a specific path.</summary>
        Task<CanvasDocumentSaveResult> SaveDocumentAsAsync(
            CanvasDocument document,
            string path,
            CancellationToken cancellationToken = default);
    }
}
