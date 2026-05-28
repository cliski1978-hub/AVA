namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Estimates token counts for content.
    /// Deterministic approximation only — not model-exact.
    /// Sprint 3.5 Block 2 provides the character-based implementation.
    /// Future sprints may plug in tiktoken or provider SDK.
    /// </summary>
    public interface ITokenEstimator
    {
        int EstimateTokens(string? text);
    }
}
