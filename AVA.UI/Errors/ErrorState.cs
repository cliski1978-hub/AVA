using System;
using System.Collections.Generic;
using System.Linq;
using CliskiCore.DbAPI;

namespace AVA.UI.Errors
{
    public class ErrorState
    {
        private readonly List<AppError> _errors = new List<AppError>();

        public event Action? OnChange;

        public IReadOnlyList<AppError> Errors
        {
            get { return _errors.Where(e => !e.IsDismissed).ToList(); }
        }

        public bool HasErrors
        {
            get { return Errors.Any(); }
        }

        public void AddError(
            string message,
            string source = "",
            string feature = "",
            AppErrorSeverity severity = AppErrorSeverity.Error)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _errors.Add(new AppError
            {
                Message   = message,
                Source    = source,
                Feature   = feature,
                Severity  = severity,
                CreatedAt = DateTime.UtcNow
            });

            NotifyChanged();
        }

        public void AddErrors(
            IEnumerable<string> messages,
            string source = "",
            string feature = "",
            AppErrorSeverity severity = AppErrorSeverity.Error)
        {
            foreach (var message in messages)
                AddError(message, source, feature, severity);
        }

        public void AddModelErrors(
            CfkApiResponse response,
            string source = "",
            string feature = "",
            AppErrorSeverity severity = AppErrorSeverity.Error)
        {
            if (response == null)
                return;

            if (!string.IsNullOrWhiteSpace(response.UserMessage))
                AddError(response.UserMessage, source, feature, severity);
            else
                AddError("An unexpected error occurred.", source, feature, severity);
        }

        public void Clear()
        {
            _errors.Clear();
            NotifyChanged();
        }

        public void ClearSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return;

            _errors.RemoveAll(e => e.Source == source);
            NotifyChanged();
        }

        public void Dismiss(Guid id)
        {
            var error = _errors.FirstOrDefault(e => e.Id == id);

            if (error == null)
                return;

            error.IsDismissed = true;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            OnChange?.Invoke();
        }
    }
}
