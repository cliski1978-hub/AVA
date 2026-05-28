namespace AVA.UPS.Adapter.Host
{
    /// <summary>
    /// Configuration for UPS host behavior.
    /// </summary>
    public class UPSHostOptions
    {
        public bool EnableHandshake { get; set; } = true;
        public bool EnableLogging { get; set; } = true;
        public bool EnableExceptionDetail { get; set; } = false;

        /// <summary>
        /// Maximum allowed payload size.
        /// </summary>
        public long MaxPayloadBytes { get; set; } = 20_000_000;
    }
}
