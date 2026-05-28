using System.Collections.Generic;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.Validation
{
    /// <summary>
    /// Static factory for generating standardized UPS error responses.
    /// </summary>
    public static class StandardUPSErrors
    {
        public static UParam ContractError(
            string module,
            string method,
            List<string> errors,
            object? expected = null,
            object? received = null)
        {
            var details = new Dictionary<string, object>
            {
                ["module"] = module,
                ["method"] = method,
                ["errors"] = errors
            };

            if (expected != null)
                details["expected"] = expected;

            if (received != null)
                details["received"] = received;

            var error = new UPSError
            {
                ErrorType = "ContractError",
                Message = $"Contract mismatch calling {module}.{method}",
                Details = details
            };

            return new UParam
            {
                Key = "error",
                Type = "upsError",
                Value = error
            };
        }
    }
}
