namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents deterministic RM-derived weighting for a prompt context item.
    /// </summary>
    public class RMContextScore
    {
        public string ItemId { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
        public double ContinuityScore { get; set; }
        public double RecencyScore { get; set; }
        public double IntentAlignmentScore { get; set; }
        public double ToolActivityScore { get; set; }
        public double MetadataWeightScore { get; set; }
        public double FinalWeightedScore { get; set; }
        public List<RMSelectionReason> Reasons { get; set; } = new();
    }
}
