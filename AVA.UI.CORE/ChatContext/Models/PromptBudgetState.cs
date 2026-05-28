namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Token budget snapshot for a single prompt context assembly.
    /// All values are estimates — actual billing may differ by provider.
    /// </summary>
    public class PromptBudgetState
    {
        public string ModelId         { get; set; } = string.Empty;
        public int    ContextWindow   { get; set; }
        public int    OutputReserve   { get; set; }
        public int    UsedTokens      { get; set; }
        public int    RemainingTokens { get; set; }
        public double UsagePercent    { get; set; }
        public bool   IsOverBudget    { get; set; }
    }
}
