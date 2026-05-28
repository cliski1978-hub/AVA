using System.Globalization;
using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Utilities
{
    /// <summary>
    /// Formats deterministic RM scores and reasons for display metadata.
    /// </summary>
    public static class RMScoreFormatter
    {
        /// <summary>
        /// Formats a score for stable metadata display.
        /// </summary>
        public static string FormatScore(double score) =>
            score.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>
        /// Formats RM selection reasons as a compact display string.
        /// </summary>
        public static string FormatReasons(IEnumerable<RMSelectionReason>? reasons)
        {
            if (reasons == null)
                return string.Empty;

            return string.Join("; ", reasons.Select(r => $"{r.Code}:{FormatScore(r.Weight)}"));
        }
    }
}
