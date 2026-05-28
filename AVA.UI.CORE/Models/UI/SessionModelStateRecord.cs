using System;
using System.Collections.Generic;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// UI-owned model-card state for a workspace session.
    /// </summary>
    public class SessionModelStateRecord
    {
        /// <summary>
        /// Gets or sets the vault identifier that owns the session.
        /// </summary>
        public string VaultId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional project identifier that owns the session.
        /// </summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the workspace session identifier.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets model IDs attached to the session top-card area.
        /// </summary>
        public List<string> AttachedModelIds { get; set; } = new();

        /// <summary>
        /// Gets or sets model IDs selected for broadcast in this session.
        /// </summary>
        public List<string> BroadcastGroupIds { get; set; } = new();

        /// <summary>
        /// Gets or sets the default model ID for the session.
        /// </summary>
        public string? DefaultModelId { get; set; }

        /// <summary>
        /// Gets or sets session model bindings with runtime context behavior.
        /// </summary>
        public List<SessionModelBinding> ModelBindings { get; set; } = new();

        /// <summary>
        /// Gets or sets the last update timestamp.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
