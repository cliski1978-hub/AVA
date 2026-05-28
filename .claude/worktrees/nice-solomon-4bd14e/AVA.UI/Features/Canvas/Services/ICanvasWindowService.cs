using System.Threading;
using System.Threading.Tasks;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Defines the operations used to prepare and materialize popped-out Canvas windows.
    /// </summary>
    public interface ICanvasWindowService
    {
        /// <summary>
        /// Gets the tool name used for Canvas window-open requests.
        /// </summary>
        string ToolName { get; }

        /// <summary>
        /// Builds a window-open request from an existing Canvas document.
        /// </summary>
        /// <param name="document">The source document.</param>
        /// <returns>A populated open-window request.</returns>
        CanvasWindowOpenRequest CreateRequest(CanvasDocument document);

        /// <summary>
        /// Prepares the route descriptor needed to launch a standalone Canvas window.
        /// </summary>
        /// <param name="request">The request to prepare.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A descriptor containing the route and encoded payload.</returns>
        Task<CanvasWindowDescriptor> PrepareWindowAsync(
            CanvasWindowOpenRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to parse a serialized window-open request.
        /// </summary>
        /// <param name="encodedRequest">The encoded request payload.</param>
        /// <returns>The parsed request if successful; otherwise <see langword="null"/>.</returns>
        CanvasWindowOpenRequest? TryParseRequest(string? encodedRequest);

        /// <summary>
        /// Creates a Canvas document from a tool-window request.
        /// </summary>
        /// <param name="request">The request payload.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The created document.</returns>
        Task<CanvasDocument> OpenFromToolAsync(
            CanvasWindowOpenRequest request,
            CancellationToken cancellationToken = default);
    }
}
