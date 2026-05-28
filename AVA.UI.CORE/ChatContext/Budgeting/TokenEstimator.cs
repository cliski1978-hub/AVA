using AVA.UI.CORE.ChatContext.Interfaces;

namespace AVA.UI.CORE.ChatContext.Budgeting
{
    /// <summary>
    /// Lightweight deterministic token estimator.
    /// Uses ceiling(characterCount / 4.0) — not provider-exact.
    /// Future sprints may replace with tiktoken or provider SDK.
    /// </summary>
    public class TokenEstimator : ITokenEstimator
    {
        public int EstimateTokens(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
        }
    }
}
