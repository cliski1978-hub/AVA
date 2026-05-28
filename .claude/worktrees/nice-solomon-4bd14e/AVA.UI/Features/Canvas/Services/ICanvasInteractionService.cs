using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.UI.CORE.Models;
using AVA.UI.CORE.Models.UI;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Defines the operations that translate AVA output into structured Canvas documents and blocks.
    /// </summary>
    public interface ICanvasInteractionService
    {
        /// <summary>
        /// Creates a new Canvas document from a raw AVA response string.
        /// </summary>
        /// <param name="title">The title for the new Canvas document.</param>
        /// <param name="responseText">The response text to convert.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The created Canvas document.</returns>
        Task<CanvasDocument> CreateDocumentFromResponseAsync(
            string title,
            string responseText,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new Canvas document from a chat message.
        /// </summary>
        /// <param name="title">The title for the new Canvas document.</param>
        /// <param name="message">The message to convert.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The created Canvas document.</returns>
        Task<CanvasDocument> CreateDocumentFromMessageAsync(
            string title,
            Message message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new Canvas document from an output segment.
        /// </summary>
        /// <param name="title">The title for the new Canvas document.</param>
        /// <param name="segment">The output segment to convert.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The created Canvas document.</returns>
        Task<CanvasDocument> CreateDocumentFromOutputSegmentAsync(
            string title,
            OutputSegment segment,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Appends a raw AVA response string to the currently active Canvas document.
        /// </summary>
        /// <param name="responseText">The response text to append.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The active document after the append operation.</returns>
        Task<CanvasDocument> AppendResponseToActiveDocumentAsync(
            string responseText,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Appends a chat message to the currently active Canvas document.
        /// </summary>
        /// <param name="message">The message to append.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The active document after the append operation.</returns>
        Task<CanvasDocument> AppendMessageToActiveDocumentAsync(
            Message message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Appends an output segment to the currently active Canvas document.
        /// </summary>
        /// <param name="segment">The output segment to append.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The active document after the append operation.</returns>
        Task<CanvasDocument> AppendOutputSegmentToActiveDocumentAsync(
            OutputSegment segment,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Parses raw response text into simple Canvas blocks.
        /// </summary>
        /// <param name="responseText">The response text to parse.</param>
        /// <returns>The parsed blocks.</returns>
        IReadOnlyList<CanvasBlock> ParseResponseToBlocks(string responseText);
    }
}
