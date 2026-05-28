using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Builds a PromptContextPackage from a completed ContextSelectionResult.
    /// Does NOT format provider-specific HTTP/API request payloads.
    /// </summary>
    public interface IPromptAssemblyService
    {
        PromptContextPackage Assemble(
            string sessionId,
            string modelId,
            string currentPrompt,
            ContextSelectionResult selectionResult,
            ModelContextSettings modelSettings);
    }
}
