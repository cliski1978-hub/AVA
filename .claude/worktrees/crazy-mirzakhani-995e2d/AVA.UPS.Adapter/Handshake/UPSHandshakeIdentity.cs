namespace AVA.UPS.Adapter.Handshake
{
    /// <summary>
    /// Declares identity information exchanged during handshake.
    /// </summary>
    public class UPSHandshakeIdentity
    {
        /// <summary>
        /// Unique ID for this machine or AVA instance.
        /// </summary>
        public string NodeId { get; set; } = "unknown-node";

        /// <summary>
        /// Module name (Vault, Memory, Agent, etc.).
        /// </summary>
        public string ModuleName { get; set; } = "unknown-module";

        /// <summary>
        /// Optional human-readable name.
        /// </summary>
        public string DisplayName { get; set; } = "UPS Module";

        /// <summary>
        /// Operating system or environment details.
        /// </summary>
        public string Environment { get; set; } = "unknown-environment";

        /// <summary>
        /// Instance version of the module, used for compatibility checks.
        /// </summary>
        public string ModuleVersion { get; set; } = "1.0.0";
    }
}
