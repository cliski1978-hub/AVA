namespace AVA.UI.CORE.ChatContext.Models
{
    public enum PromptContextSelectionStatus
    {
        NotEvaluated     = 0,
        Required         = 1,
        Recommended      = 2,
        Included         = 3,
        ExcludedByBudget = 4,
        ExcludedByPolicy = 5,
        ExcludedByUser   = 6,
        ForcedByUser     = 7,
        Pinned           = 8
    }
}
