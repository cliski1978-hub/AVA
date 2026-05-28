namespace AVA.UI.CORE.ChatContext.Models
{
    public enum HistoryPolicyType
    {
        FullSession          = 0,
        RecentMessages       = 1,
        CurrentDay           = 2,
        LatestOnly           = 3,
        NoHistory            = 4,
        ManualOnly           = 5,
        MinimalConversation  = 6
    }
}
