using System;

namespace AVA.UI.Errors
{
    public class AppError
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Feature { get; set; } = string.Empty;
        public AppErrorSeverity Severity { get; set; } = AppErrorSeverity.Error;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDismissed { get; set; }
    }
}
