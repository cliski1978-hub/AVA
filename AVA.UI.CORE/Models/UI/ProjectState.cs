using System;
using System.Collections.Generic;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Represents a project within a vault.
    /// </summary>
    public class ProjectState
    {
        /// <summary>
        /// Unique project identifier.
        /// </summary>
        public string ProjectId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Human-readable project name.
        /// </summary>
        public string Name { get; set; } = "New Project";

        /// <summary>
        /// Whether the project tree is expanded in the UI.
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// Sessions contained within this project.
        /// </summary>
        public List<SessionState> Sessions { get; set; } = new();

        /// <summary>
        /// File references associated with the project.
        /// </summary>
        public List<FileRef> FileRefs { get; set; } = new();

        /// <summary>
        /// Optional knowledge base identifier associated with the project.
        /// </summary>
        public string? KnowledgeBaseId { get; set; }

        /// <summary>
        /// Default model IDs applied to new sessions in this project.
        /// </summary>
        public List<string> DefaultModelIds { get; set; } = new();
    }

    /// <summary>
    /// Lightweight reference to a file attached to a project.
    /// </summary>
    public class FileRef
    {
        /// <summary>
        /// Full or relative file path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable file name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the file was attached or indexed.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
