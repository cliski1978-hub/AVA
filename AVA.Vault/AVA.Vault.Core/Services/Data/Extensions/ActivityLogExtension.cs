
using AVA.Vault.Core.Data.Models;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data
{

    public static class ActivityLogExtensions
    {
        public static void Log(this IDbContext Context, string UserName, int Level, string Category, string TargetID, string Action, string Message = null)
        {
            Context.Set<ActivityLogEntry>().Add(new ActivityLogEntry
            {
                Date = DateTime.UtcNow,
                UserName = UserName,
                Level = Level,
                Category = Category,
                TargetID = TargetID,
                Action = Action,
                Message = Message
            });
        }
    }
}