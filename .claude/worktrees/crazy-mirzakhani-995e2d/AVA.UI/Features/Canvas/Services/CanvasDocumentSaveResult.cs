namespace AVA.UI.Features.Canvas.Services
{
    /// <summary>
    /// Represents the outcome of a Canvas document save operation.
    /// </summary>
    public sealed class CanvasDocumentSaveResult
    {
        public bool IsSuccess { get; init; }
        public bool IsStubbed { get; init; }
        public string Message { get; init; } = string.Empty;
        public string? SavedPath { get; init; }

        public static CanvasDocumentSaveResult Success(string savedPath) => new()
        {
            IsSuccess = true,
            Message   = "Document saved.",
            SavedPath = savedPath
        };

        public static CanvasDocumentSaveResult Stubbed(string message) => new()
        {
            IsSuccess = true,
            IsStubbed = true,
            Message   = message
        };

        public static CanvasDocumentSaveResult Failure(string message) => new()
        {
            IsSuccess = false,
            Message   = message
        };
    }
}
