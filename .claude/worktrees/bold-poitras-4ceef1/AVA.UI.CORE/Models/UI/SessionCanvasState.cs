using System;
using System.Collections.Generic;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Represents the canvas workspace within a session.
    /// </summary>
    public class SessionCanvasState
    {
        /// <summary>
        /// Cards placed on the canvas.
        /// </summary>
        public List<CardState> Cards { get; set; } = new();

        /// <summary>
        /// Currently focused card identifier.
        /// </summary>
        public string? ActiveCardId { get; set; }

        /// <summary>
        /// Horizontal scroll offset.
        /// </summary>
        public double ScrollX { get; set; }

        /// <summary>
        /// Vertical scroll offset.
        /// </summary>
        public double ScrollY { get; set; }

        /// <summary>
        /// Current zoom level where 1.0 is 100 percent.
        /// </summary>
        public float Zoom { get; set; } = 1.0f;

        /// <summary>
        /// Whether the background grid is visible.
        /// </summary>
        public bool ShowGrid { get; set; }

        /// <summary>
        /// Whether card movement snaps to the grid.
        /// </summary>
        public bool SnapToGrid { get; set; }

        /// <summary>
        /// Grid size used when snap-to-grid is enabled.
        /// </summary>
        public int GridSize { get; set; } = 20;

        /// <summary>
        /// Saved canvas layouts.
        /// </summary>
        public List<CanvasSnapshot> SavedLayouts { get; set; } = new();

        /// <summary>
        /// Whether the global prompt bar is visible.
        /// </summary>
        public bool GlobalBarVisible { get; set; } = true;

        /// <summary>
        /// Global prompt bar position.
        /// </summary>
        public string GlobalBarPosition { get; set; } = "Bottom";

        /// <summary>
        /// Optional list of target card IDs for the global bar.
        /// Null means all cards.
        /// </summary>
        public List<string>? GlobalTargetCardIds { get; set; }
    }

    /// <summary>
    /// Saved snapshot of a canvas layout.
    /// </summary>
    public class CanvasSnapshot
    {
        /// <summary>
        /// Unique snapshot identifier.
        /// </summary>
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Human-readable snapshot name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Card positions by card identifier.
        /// </summary>
        public Dictionary<string, CardPosition> CardPositions { get; set; } = new();

        /// <summary>
        /// Timestamp when the snapshot was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Persisted position and size for a card within a snapshot.
    /// </summary>
    public class CardPosition
    {
        /// <summary>
        /// X coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Card width.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Optional card height.
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// Z-index stacking order.
        /// </summary>
        public int ZIndex { get; set; }
    }
}
