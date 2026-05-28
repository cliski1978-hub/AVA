namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents a deterministic RM context analysis pass.
    /// </summary>
    public class RMContextAnalysisResult
    {
        public List<RMContextScore> Scores { get; set; } = new();
        public List<PromptContextItem> PrioritizedItems { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public bool AnalysisApplied { get; set; }
    }
}
