namespace AVA.UPS.Adapter.Handshake
{
    /// <summary>
    /// Error produced during handshake negotiation.
    /// Includes helper factories for common handshake failures.
    /// </summary>
    public class UPSHandshakeError
    {
        public string Code { get; set; } = "HANDSHAKE_ERROR";
        public string Message { get; set; } = "";
        public string? RequiredVersion { get; set; }
        public string? ProvidedVersion { get; set; }

        /// <summary>
        /// Creates a version mismatch error.
        /// </summary>
        public static UPSHandshakeError VersionMismatch(string required, string provided)
        {
            return new UPSHandshakeError
            {
                Code = "VERSION_INCOMPATIBLE",
                Message = "Protocol version mismatch.",
                RequiredVersion = required,
                ProvidedVersion = provided
            };
        }

        /// <summary>
        /// Creates a capability mismatch error.
        /// </summary>
        public static UPSHandshakeError CapabilityMismatch()
        {
            return new UPSHandshakeError
            {
                Code = "CAPABILITY_MISMATCH",
                Message = "No overlapping transport capabilities."
            };
        }
    }
}
