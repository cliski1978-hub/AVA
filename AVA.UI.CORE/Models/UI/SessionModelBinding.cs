namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Session-owned binding between a workspace session and one model definition.
    /// </summary>
    public class SessionModelBinding
    {
        /// <summary>Model identifier for this binding.</summary>
        public string ModelId { get; set; } = string.Empty;

        /// <summary>Provider profile identifier for disambiguating duplicate model IDs.</summary>
        public string ProviderProfileId { get; set; } = string.Empty;

        /// <summary>Whether this model is the default model for the session.</summary>
        public bool IsDefault { get; set; }

        /// <summary>Whether this model participates in session broadcast.</summary>
        public bool IsBroadcastEnabled { get; set; }

        /// <summary>Session-specific runtime context behavior for this model.</summary>
        public RuntimeContextSettings RuntimeContextSettings { get; set; } = new();
    }
}
