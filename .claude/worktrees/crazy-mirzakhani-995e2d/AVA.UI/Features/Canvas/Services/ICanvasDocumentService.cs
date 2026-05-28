using System;
using System.Threading.Tasks;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Defines the document-state operations used by the Canvas feature.
    /// </summary>
    public interface ICanvasDocumentService
    {
        /// <summary>
        /// Gets the currently open canvas documents.
        /// </summary>
        IReadOnlyList<CanvasDocument> OpenDocuments { get; }

        /// <summary>
        /// Gets the currently active canvas document.
        /// </summary>
        CanvasDocument? ActiveDocument { get; }

        /// <summary>
        /// Raised whenever the active document reference or its contents change.
        /// </summary>
        event Action? ActiveDocumentChanged;

        /// <summary>
        /// Creates a new canvas document and makes it the active document.
        /// </summary>
        /// <param name="title">The title for the new document.</param>
        /// <returns>The created document.</returns>
        Task<CanvasDocument> CreateDocumentAsync(string title);

        /// <summary>
        /// Sets the active canvas document.
        /// </summary>
        /// <param name="document">The document to activate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SetActiveDocumentAsync(CanvasDocument document);

        /// <summary>
        /// Closes an open canvas document by identifier.
        /// </summary>
        /// <param name="documentId">The identifier of the document to close.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task CloseDocumentAsync(string documentId);

        /// <summary>
        /// Creates a copy of an existing canvas document and activates it.
        /// </summary>
        /// <param name="documentId">The identifier of the document to copy.</param>
        /// <returns>The duplicated document.</returns>
        Task<CanvasDocument> DuplicateDocumentAsync(string documentId);

        /// <summary>
        /// Adds a block to the active document.
        /// </summary>
        /// <param name="block">The block to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AddBlockAsync(CanvasBlock block);

        /// <summary>
        /// Removes a block from the active document by identifier.
        /// </summary>
        /// <param name="blockId">The identifier of the block to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveBlockAsync(string blockId);

        /// <summary>
        /// Updates the content of a text-oriented block in the active document.
        /// </summary>
        /// <param name="blockId">The identifier of the block to update.</param>
        /// <param name="text">The new block text.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateTextBlockAsync(string blockId, string text);

        /// <summary>
        /// Moves a block to a new sort position within the active document.
        /// </summary>
        /// <param name="blockId">The identifier of the block to move.</param>
        /// <param name="newSortOrder">The new sort order to apply.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MoveBlockAsync(string blockId, int newSortOrder);

        /// <summary>
        /// Converts an existing block into a different block type while preserving its position.
        /// </summary>
        /// <param name="blockId">The identifier of the block to transform.</param>
        /// <param name="targetType">The destination block type.</param>
        /// <param name="text">Optional replacement text for the new block content.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task TransformBlockAsync(string blockId, CanvasBlockType targetType, string? text = null);
    }
}
