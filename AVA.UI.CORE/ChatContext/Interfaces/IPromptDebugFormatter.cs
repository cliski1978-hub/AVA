using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Builds inspectable debug payloads from assembled prompt context packages.
    /// </summary>
    public interface IPromptDebugFormatter
    {
        /// <summary>
        /// Builds a deterministic prompt debug package.
        /// </summary>
        PromptDebugPackage BuildDebugPackage(
            PromptContextPackage package,
            ModelContextSettings modelSettings);
    }
}
