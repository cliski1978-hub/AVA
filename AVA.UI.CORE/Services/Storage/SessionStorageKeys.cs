namespace AVA.UI.CORE.Services.Storage;

/// <summary>
/// Centralised key constants for session storage.
/// Use these instead of magic strings throughout ViewModels.
/// Pattern: ava.ui.{feature}.{item}
/// </summary>
public static class SessionStorageKeys
{
    // ── Dock ─────────────────────────────────────────────────────────────────
    public const string DockLeftWidth     = "ava.ui.dock.left.width";
    public const string DockLeftCollapsed = "ava.ui.dock.left.collapsed";
    public const string DockRightWidth    = "ava.ui.dock.right.width";
    public const string DockBottomHeight  = "ava.ui.dock.bottom.height";

    // ── Vault / Workspace ─────────────────────────────────────────────────────
    public const string VaultSelectedVaultId   = "ava.ui.vault.selectedVaultId";
    public const string VaultSelectedProjectId = "ava.ui.vault.selectedProjectId";
    public const string VaultSelectedSessionId = "ava.ui.vault.selectedSessionId";

    // ── Chat ──────────────────────────────────────────────────────────────────
    public const string ChatActiveModelProfileId = "ava.ui.chat.activeModelProfileId";
    public const string ChatIsBroadcastMode      = "ava.ui.chat.isBroadcastMode";
    public const string ChatSessionIndex         = "ava.chat.sessions.index";
    public const string ActiveChatSessionId      = "ava.chat.activeSessionId";

    /// <summary>Returns the storage key for a specific chat session log.</summary>
    public static string ChatSession(string sessionId) => $"ava.chat.sessions.{sessionId}.json";

    // ── Canvas ────────────────────────────────────────────────────────────────
    public const string CanvasActiveDocumentId = "ava.ui.canvas.activeDocumentId";
    public const string CanvasActiveTab        = "ava.ui.canvas.activeTab";

    // ── Navigation ────────────────────────────────────────────────────────────
    public const string NavigationSelectedItem    = "ava.ui.navigation.selectedItem";
    public const string NavigationSidebarCollapsed = "ava.ui.navigation.sidebarCollapsed";
}
