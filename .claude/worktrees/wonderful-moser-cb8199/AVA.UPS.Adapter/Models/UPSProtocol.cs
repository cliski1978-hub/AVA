namespace AVA.UPS.Adapter
{
    /// <summary>
    /// Central definition of the UPS protocol version.
    /// This is the only place where the protocol version is defined.
    /// </summary>
    public static class UPSProtocol
    {
        /// <summary>
        /// Current UPS protocol version.
        /// Bump this only when the UPS message contract or handshake rules change.
        /// </summary>
        public const string Version = "1.0.0";
    }
}
