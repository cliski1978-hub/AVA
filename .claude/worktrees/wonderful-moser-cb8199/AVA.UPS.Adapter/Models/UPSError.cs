using System.Collections.Generic;

namespace AVA.UPS.Adapter.Models
{
    /// <summary>
    /// A standard UPS error object that can be wrapped into a UParam with type "upsError".
    /// </summary>
    public class UPSError
    {
        public string ErrorType { get; set; } = default!;
        public string Message { get; set; } = default!;
        public Dictionary<string, object>? Details { get; set; } = new();
    }
}
