using System;
using System.Collections.Generic;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Represents a single card on the session canvas.
    /// </summary>
    public class CardState
    {
        /// <summary>
        /// Unique card identifier.
        /// </summary>
        public string CardId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Card type such as SingleModel or Broadcast.
        /// </summary>
        public string CardType { get; set; } = "SingleModel";

        /// <summary>
        /// Optional card title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Canvas X coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Canvas Y coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Card width.
        /// </summary>
        public double Width { get; set; } = 380.0;

        /// <summary>
        /// Optional card height.
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// Z-index stacking order.
        /// </summary>
        public int ZIndex { get; set; } = 1;

        /// <summary>
        /// Whether the card is minimised.
        /// </summary>
        public bool IsMinimised { get; set; }

        /// <summary>
        /// Model profile IDs assigned to this card.
        /// </summary>
        public List<string> ModelProfileIds { get; set; } = new();

        /// <summary>
        /// Active model profile identifier for cards that focus a single model.
        /// </summary>
        public string? ActiveProfileId { get; set; }

        /// <summary>
        /// Whether the card participates in global prompt sends.
        /// </summary>
        public bool ParticipatesInGlobal { get; set; } = true;

        /// <summary>
        /// Optional per-card system prompt override.
        /// </summary>
        public string? SystemPromptOverride { get; set; }

        /// <summary>
        /// Optional temperature override.
        /// </summary>
        public float? TemperatureOverride { get; set; }

        /// <summary>
        /// Optional max tokens override.
        /// </summary>
        public int? MaxTokensOverride { get; set; }

        /// <summary>
        /// Context window mode used by the card.
        /// </summary>
        public string ContextWindowMode { get; set; } = "Full";

        /// <summary>
        /// Broadcast response layout mode.
        /// </summary>
        public string ResponseLayout { get; set; } = "Stacked";

        /// <summary>
        /// Whether broadcast sends are executed in parallel.
        /// </summary>
        public bool SendInParallel { get; set; } = true;

        /// <summary>
        /// Whether model labels are visible in the card.
        /// </summary>
        public bool ShowModelLabels { get; set; } = true;

        /// <summary>
        /// Whether the card starts collapsed by default.
        /// </summary>
        public bool CollapseByDefault { get; set; }

        /// <summary>
        /// Optional accent color override for the card.
        /// </summary>
        public string? AccentColorOverride { get; set; }

        /// <summary>
        /// Whether the card input bar is visible.
        /// </summary>
        public bool ShowInputBar { get; set; } = true;

        /// <summary>
        /// Per-card font size scale factor.
        /// </summary>
        public float FontSizeScale { get; set; } = 1.0f;
    }
}
