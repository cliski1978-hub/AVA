// ─────────────────────────────────────────────────────────────────────────────
//  Class     : PanelId
//  Namespace : AVA.UI.CORE.Models.UI
//  Purpose   : Constants for all registered dockable panel identifiers.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Panel identifier constants.
    /// Every dockable panel in AVA has an entry here.
    /// </summary>
    public static class PanelId
    {
        public const string ChatOutput    = "chat-output";
        public const string ChatInput     = "chat-input";
        public const string ModelPicker   = "model-picker";
        public const string SessionTabs   = "session-tabs";
        public const string MemoryLog     = "memory-log";
        public const string Reflection    = "reflection";
        public const string PromptPreview = "prompt-preview";
        public const string Settings      = "settings";
        public const string Canvas        = "canvas";
        public const string VaultNotes    = "vault-notes";
    }
}
