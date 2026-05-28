using System;
using System.Collections.Generic;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Represents a single session within a project.
    /// </summary>
    public class SessionState
    {
        /// <summary>
        /// Unique session identifier.
        /// </summary>
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Human-readable session name.
        /// </summary>
        public string Name { get; set; } = "New Session";

        /// <summary>
        /// Timestamp when the session was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the session was last active.
        /// </summary>
        public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether the session is pinned in the UI.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Model IDs attached at the session level.
        /// </summary>
        public List<string> AttachedModelIds { get; set; } = new();

        /// <summary>
        /// Model IDs participating in the session broadcast group.
        /// </summary>
        public List<string> BroadcastGroupIds { get; set; } = new();

        /// <summary>
        /// Default model ID for the session.
        /// </summary>
        public string? DefaultModelId { get; set; }

        /// <summary>
        /// Canvas state backing the session workspace.
        /// </summary>
        public SessionCanvasState Canvas { get; set; } = new();

        /// <summary>
        /// Active document for this session. Null until the user opens or creates a document.
        /// </summary>
        public CanvasDocument? ActiveDocument { get; set; }

        /// <summary>
        /// Whether this session acts as a reusable template.
        /// </summary>
        public bool IsTemplate { get; set; }

        /// <summary>
        /// Optional template display name.
        /// </summary>
        public string? TemplateName { get; set; }

        /// <summary>
        /// Number of times this template has been spawned.
        /// </summary>
        public int SpawnCount { get; set; }
    }
}
