using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Centralized deterministic retrieval of model prompt construction policy settings.
    /// Never returns null — safe defaults are always provided.
    /// No RM behavior — resolver is deterministic configuration lookup only.
    /// </summary>
    public interface IModelContextPolicyResolver
    {
        ModelContextSettings Resolve(string modelId);
    }
}
