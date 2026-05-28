using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Models;
using AVA.UI.CORE.Models.Settings;
using AVA.UI.CORE.Models.UI;
using AVA.UI.CORE.Services;
using AVA.UI.CORE.Services.Network;
using AVA.UI.CORE.UPS.Client;
using AVA.UI.CORE.UPS.Sessions;
using AVA.UPS.Adapter.Models;
using AVA.UI.Vault.Services;
using AVA.UI.Errors;
using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Services.Storage;
using AVA.UI.Features.Navigation.State;
using AVA.UI.Features.Reflection.State;
using AVA.UI.Features.Chat.State;
using AVA.UI.CORE.Services.Profiles;
using AVA.UI.Features.Settings.State;

namespace AVA.UI.State
{
    /// <summary>
    /// Application-level coordinator. Holds global active selection IDs,
    /// shared runtime references, and cross-feature events.
    /// Feature-specific state now lives in dedicated feature state stores.
    /// </summary>
    public class AppState
    {
        public sealed class AppUser
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public const string PanelChatOutput = "chat-output";
        public const string PanelSettings = "settings";
        public const string PanelMemoryLog = "memory-log";
        public const string PanelReflection = "reflection";

        // ── Core dependencies ─────────────────────────────────────────────────
        private readonly AvaSettingsService _settingsService;
        private readonly VaultWorkspaceFileService _vaultWorkspace;
        private readonly IVaultUiSyncService _vaultSync;
        private readonly ErrorState _errorState;
        private readonly ISessionStorageService _session;
        private readonly ISessionModelStateStore _sessionModelStateStore;
        private readonly IAvaIdService _ids;
        private readonly LlmProfileService _profileService;

        // ── Feature state stores ──────────────────────────────────────────────
        private readonly NavigationState _navState;
        private readonly ReflectionState _reflectionState;
        private readonly ChatConversationState _chatState;
        private readonly SettingsState _settingsState;

        // ── Shared runtime ────────────────────────────────────────────────────
        public readonly UPSClientService UPSClient;
        public readonly SessionManager Sessions;

        // ── Cross-feature event ───────────────────────────────────────────────
        public event Action? OnChange;
        private void Notify() => OnChange?.Invoke();

        public event Func<string, Task>? OnGlobalBroadcast;

        public async Task RaiseGlobalBroadcastAsync(string prompt)
        {
            if (OnGlobalBroadcast == null) return;
            var handlers = OnGlobalBroadcast.GetInvocationList().Cast<Func<string, Task>>();
            await Task.WhenAll(handlers.Select(h => h(prompt)));
        }

        // ── Global identity ───────────────────────────────────────────────────
        public AppUser CurrentUser { get; } = BuildCurrentUser();

        // ── Global active selections ──────────────────────────────────────────
        public string? ActiveVaultId { get; private set; }
        public string? ActiveProjectId { get; private set; }
        public string? ActiveWorkspaceSessionId { get; private set; }

        public List<VaultState> Vaults => _vaultWorkspace.Vaults;

        public VaultState? ActiveVault =>
            Vaults.FirstOrDefault(v => v.VaultId == ActiveVaultId);

        public ProjectState? ActiveProject =>
            ActiveVault?.Projects.FirstOrDefault(p => p.ProjectId == ActiveProjectId);

        public SessionState? ActiveWorkspaceSession =>
            ActiveProjectId == null
                ? ActiveVault?.Sessions.FirstOrDefault(s => s.SessionId == ActiveWorkspaceSessionId)
                : ActiveProject?.Sessions.FirstOrDefault(s => s.SessionId == ActiveWorkspaceSessionId);

        public SessionState? CurrentSession => ActiveWorkspaceSession;

        // ── Settings pass-through ─────────────────────────────────────────────
        public AppSettings AppSettings => _settingsService.AppSettings;
        public AppSettings Settings => _settingsService.AppSettings;
        public List<LLMProfile> LLMProfiles => _settingsService.AppSettings.LLMProfiles;
        public LLMProfile? SelectedLLMProfile { get; set; }

        public bool UseDirectEndpoints
        {
            get => _settingsService.AppSettings.UseDirectEndpoints;
            set => _settingsService.AppSettings.UseDirectEndpoints = value;
        }

        // ── Navigation — delegates to NavigationState ─────────────────────────
        public string SelectedNavigationItem => _navState.SelectedNavigationItem;

        public void SetSelectedNavigationItem(string item)
            => _navState.SetSelectedNavigationItem(item);

        // ── Connection status — delegates to SettingsState ────────────────────
        public bool IsConnected => _settingsState.IsConnected;
        public string ConnectionType => _settingsState.ConnectionType;
        public string ConnectionStatus => _settingsState.ConnectionStatus;
        public string ConnectionDetails => _settingsState.ConnectionDetails;

        public void UpdateStatus(string type, string status, string details = "")
            => _settingsState.UpdateStatus(type, status, details);

        // ── Reflection — delegates to ReflectionState ─────────────────────────
        public ObservableCollection<string> MemoryEvents => _reflectionState.MemoryEvents;
        public ObservableCollection<string> Reflections => _reflectionState.Reflections;
        public string LatestInsight => _reflectionState.LatestInsight;
        public bool HasContradiction => _reflectionState.HasContradiction;

        public void AppendMemory(string message) => _reflectionState.AppendMemory(message);
        public void ClearMemory() => _reflectionState.ClearMemory();
        public void AppendReflection(string insight, bool isContradiction = false, string score = "")
            => _reflectionState.AppendReflection(insight, isContradiction, score);

        // ── Chat — delegates to ChatConversationState ─────────────────────────
        public ObservableCollection<OutputSegment> OutputSegments => _chatState.OutputSegments;
        public string PromptPreviewText
        {
            get => _chatState.PromptPreviewText;
            set => _chatState.PromptPreviewText = value;
        }
        public int PromptTokenEstimate
        {
            get => _chatState.PromptTokenEstimate;
            set => _chatState.PromptTokenEstimate = value;
        }
        public bool ShowPromptPreview
        {
            get => _chatState.ShowPromptPreview;
            set => _chatState.ShowPromptPreview = value;
        }

        public IReadOnlyList<Message> GetConversation(string modelId)
            => _chatState.GetConversation(modelId);

        public List<Message> GetBroadcastConversation(IEnumerable<string> modelIds)
            => _chatState.GetBroadcastConversation(modelIds);

        public void AppendUserConversationMessage(
            string modelId, string content, string? modelLabel = null, string? turnId = null)
            => _chatState.AppendUserMessage(modelId, content, modelLabel, turnId);

        public ChatSessionContext CaptureChatContext()
            => _chatState.CaptureContext();

        public void SetModelTyping(string modelId, bool typing)
            => _chatState.SetTyping(modelId, typing);

        public bool IsModelTyping(string modelId)
            => _chatState.IsTyping(modelId);

        public bool IsChatSending => _chatState.IsChatSendingFor(_chatState.ActiveSessionId);

        public void AppendAssistantConversationMessage(
            string modelId, string content, string? modelLabel = null,
            bool isError = false, string? turnId = null,
            Dictionary<string, object>? responseMetadata = null,
            ChatSessionContext? sessionContext = null)
        {
            _chatState.AppendAssistantMessage(
                modelId,
                content,
                modelLabel,
                isError,
                turnId,
                responseMetadata,
                sessionContext: sessionContext);
            Notify();
        }

        public void ClearConversation(string modelId) => _chatState.ClearConversation(modelId);

        public void AppendOutput(OutputSegment segment) => _chatState.AppendOutput(segment);
        public void AppendOutputText(string text) => _chatState.AppendOutputText(text);
        public void AppendOutputSystem(string text) => _chatState.AppendOutputSystem(text);
        public void ClearOutput() => _chatState.ClearOutput();

        // ── Chat methods that still need Sessions (remain here until Step 6) ──
        public IReadOnlyList<Message> GetMainChatMessages()
        {
            var targets = GetMainChatSessions().ToList();
            if (!targets.Any()) return Array.Empty<Message>();

            var modelIds = targets
                .Select(s => s.ModelProfile?.Id ?? s.ModelEntry.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return modelIds.Count == 1
                ? GetConversation(modelIds[0])
                : GetBroadcastConversation(modelIds);
        }

        public string GetMainChatTitle()
        {
            var targets = GetMainChatSessions().ToList();
            if (!targets.Any()) return "Conversation";
            return targets.Count == 1 ? targets[0].DisplayName : $"Broadcast ({targets.Count})";
        }

        public bool IsMainChatWaiting =>
            _chatState.IsChatSendingFor(_chatState.ActiveSessionId)
            || GetMainChatSessions().Any(s => s.IsSending);

        public void ClearMainChat()
        {
            var modelIds = GetMainChatSessions()
                .Select(s => s.ModelProfile?.Id ?? s.ModelEntry.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            _chatState.ClearConversations(modelIds);
        }

        // ── Constructor ───────────────────────────────────────────────────────
        public AppState(
            AvaSettingsService settingsService,
            VaultWorkspaceFileService vaultWorkspace,
            IVaultUiSyncService vaultSync,
            ErrorState errorState,
            ISessionStorageService session,
            ISessionModelStateStore sessionModelStateStore,
            IAvaIdService ids,
            LlmProfileService profileService,
            NavigationState navState,
            ReflectionState reflectionState,
            ChatConversationState chatState,
            SettingsState settingsState)
        {
            _settingsService = settingsService;
            _vaultWorkspace  = vaultWorkspace;
            _vaultSync       = vaultSync;
            _errorState      = errorState;
            _session         = session;
            _sessionModelStateStore = sessionModelStateStore;
            _ids             = ids;
            _profileService  = profileService;
            _navState        = navState;
            _reflectionState = reflectionState;
            _chatState       = chatState;
            _settingsState   = settingsState;

            // Propagate feature state changes to AppState.OnChange
            _navState.OnChange += Notify;
            _reflectionState.OnChange += Notify;
            _chatState.OnChange += Notify;
            _settingsState.OnChange += Notify;

            _settingsService.LoadSettings();

            // Override profile data from Vault (source of truth for provider/model profiles)
            LoadProfilesFromVaultAsync().GetAwaiter().GetResult();

            UPSClient = new UPSClientService();
            Sessions = new SessionManager(UPSClient);
            Sessions.OnChange += Notify;

            RestoreSelectedLLMProfile();

            AppendMemory("Memory log initialized.");
            AppendReflection("Reflection system initialized.");
            AppendOutput(new OutputSegment { Type = "system", Value = "AVA ready." });

            InitializeWorkspaceState();
            _ = ConnectAsync();
        }

        public void NotifyStateChanged() => Notify();

        // ── Vault-backed profile management ──────────────────────────────────────
        private async Task LoadProfilesFromVaultAsync()
        {
            try
            {
                var providers = await _profileService.GetAllProviderProfilesAsync();
                var llmProfiles = await _profileService.GetAllLlmProfilesAsync();

                _settingsService.AppSettings.ProviderProfiles = providers;
                _settingsService.AppSettings.LLMProfiles = llmProfiles;

                var models = new List<ModelDefinition>();
                foreach (var provider in providers)
                {
                    var providerModels = await _profileService.GetModelsByProfileIdAsync(provider.ProviderProfileId);
                    models.AddRange(providerModels);
                }
                _settingsService.AppSettings.ModelDefinitions = models;
            }
            catch (Exception ex)
            {
                AppendMemory($"Vault profile load failed (falling back to JSON): {ex.Message}");
                _errorState.AddError(
                    "Your provider profiles could not be loaded from the database. Falling back to local settings.",
                    source: "ProfilePersistence",
                    feature: "Settings",
                    severity: AppErrorSeverity.Warning);
            }
        }

        private async Task<bool> SaveProfilesToVaultAsync()
        {
            try
            {
                foreach (var provider in _settingsService.AppSettings.ProviderProfiles)
                {
                    provider.ApiKey = string.Empty;
                    provider.Secret = string.Empty;
                    var saved = await _profileService.SaveProviderProfileAsync(provider);
                    provider.ProviderProfileId = saved.ProviderProfileId;
                }

                foreach (var model in _settingsService.AppSettings.ModelDefinitions)
                {
                    await _profileService.SaveModelDefinitionAsync(model);
                }
                return true;
            }
            catch (Exception ex)
            {
                AppendMemory($"Vault profile save failed: {ex.Message}");
                _errorState.AddError(
                    "Your provider profile could not be saved to the database. The profile database tables may be missing or unavailable. Please apply the latest Vault migration and try again.",
                    source: "ProfilePersistence",
                    feature: "Settings",
                    severity: AppErrorSeverity.Error);
                return false;
            }
        }

        public async Task<bool> SaveProviderProfileAsync(ProviderProfile provider)
        {
            try
            {
                provider.ApiKey = string.Empty;
                provider.Secret = string.Empty;
                var saved = await _profileService.SaveProviderProfileAsync(provider);
                provider.ProviderProfileId = saved.ProviderProfileId;
                AppendMemory($"Provider profile '{provider.Name}' saved.");
                _errorState.AddError($"Profile '{provider.Name}' saved.", source: "ProfilePersistence", feature: "Settings", severity: AppErrorSeverity.Info);
                return true;
            }
            catch (Exception ex)
            {
                AppendMemory($"Provider profile save failed: {ex.Message}");
                _errorState.AddError(
                    "Your provider profile could not be saved to the database. Please apply the latest Vault migration and try again.",
                    source: "ProfilePersistence",
                    feature: "Settings",
                    severity: AppErrorSeverity.Error);
                return false;
            }
        }

        public async Task DeleteProviderProfileAsync(string providerProfileId)
        {
            await _profileService.DeleteProviderProfileAsync(providerProfileId);
        }

        public async Task<bool> SaveModelDefinitionAsync(ModelDefinition model)
        {
            try
            {
                await _profileService.SaveModelDefinitionAsync(model);
                AppendMemory($"Model '{model.DisplayName}' saved.");
                _errorState.AddError($"Model '{model.DisplayName}' saved.", source: "ProfilePersistence", feature: "Settings", severity: AppErrorSeverity.Info);
                return true;
            }
            catch (Exception ex)
            {
                AppendMemory($"Model definition save failed: {ex.Message}");
                _errorState.AddError(
                    "Your model definition could not be saved to the database. Please apply the latest Vault migration and try again.",
                    source: "ProfilePersistence",
                    feature: "Settings",
                    severity: AppErrorSeverity.Error);
                return false;
            }
        }

        public async Task DeleteModelDefinitionAsync(string modelDefinitionId)
        {
            await _profileService.DeleteModelDefinitionAsync(modelDefinitionId);
        }

        // ── Navigation ────────────────────────────────────────────────────────
        public void OpenProfile()
        {
            SetSelectedNavigationItem("Settings");
            AppendOutputSystem("Profile panel opened in Settings.");
        }

        public void SignOut()
        {
            Sessions.CloseAll();
            _settingsState.SetConnected(false);
            UpdateStatus("Offline", "Signed out", "Active model sessions closed.");
            SetSelectedNavigationItem("Settings");
            AppendOutputSystem("Signed out of the current runtime session.");
        }

        // ── Workspace state ───────────────────────────────────────────────────
        public async Task LoadWorkspaceStateAsync()
        {
            // DB vaults — loaded via DbVaultPersistenceProvider
            var fromDb = await _vaultSync.LoadVaultsFromDatabaseAsync();

            // File vaults — loaded via FileVaultPersistenceProvider (VaultManager)
            // vaults.json serves as offline mirror/index only — not authoritative
            var fromFileSystem = await _vaultSync.LoadVaultsFromFileSystemAsync();

            // Merge: DB and file system are peers. StorageMode already set by each provider.
            // vaults.json used as fallback only if both providers return nothing.
            var merged = fromDb.ToDictionary(v => v.VaultId, v => v);

            foreach (var fileVault in fromFileSystem)
            {
                if (!merged.ContainsKey(fileVault.VaultId))
                {
                    merged[fileVault.VaultId] = fileVault;
                }
                else
                {
                    // Vault exists in both — merge projects missing from DB result.
                    var dbVault      = merged[fileVault.VaultId];
                    var dbProjectIds = dbVault.Projects.Select(p => p.ProjectId).ToHashSet();

                    foreach (var fileProject in fileVault.Projects)
                    {
                        if (!dbProjectIds.Contains(fileProject.ProjectId))
                            dbVault.Projects.Add(fileProject);
                    }
                }
            }

            var mergedList = merged.Values.ToList();

            // If both providers returned nothing, fall back to vaults.json mirror.
            if (!mergedList.Any())
            {
                await _vaultWorkspace.LoadAsync();
                mergedList = _vaultWorkspace.Vaults;
            }
            else
            {
                // Keep vaults.json in sync as offline mirror.
                await _vaultWorkspace.SaveVaultsAsync(mergedList);
            }

            // DB is the primary source for session model state (via MapToSessionState).
            // Apply JSON as a fallback for any session where DB had no model state.
            // JSON is never used to overwrite non-empty DB values.
            await _sessionModelStateStore.ApplyToVaultsAsync(mergedList);

            await _vaultWorkspace.SaveVaultsAsync(mergedList);

            // Restore last selection from session storage before initialising state.
            var lastVaultId   = await _session.GetAsync<string>(SessionStorageKeys.VaultSelectedVaultId);
            var lastProjectId = await _session.GetAsync<string>(SessionStorageKeys.VaultSelectedProjectId);
            var lastSessionId = await _session.GetAsync<string>(SessionStorageKeys.VaultSelectedSessionId);

            if (!string.IsNullOrWhiteSpace(lastVaultId))   ActiveVaultId              = lastVaultId;
            if (!string.IsNullOrWhiteSpace(lastProjectId)) ActiveProjectId            = lastProjectId;
            if (!string.IsNullOrWhiteSpace(lastSessionId)) ActiveWorkspaceSessionId   = lastSessionId;

            InitializeWorkspaceState();

            // If ConnectAsync already ran and registered profiles before this method completed,
            // sync runtime sessions now so the race condition is covered.
            // If ConnectAsync hasn't run yet, RestoreStartupSessions will handle it after
            if (Sessions.AvailableModels.Any())
                SyncRuntimeModelSessionsFromActiveSession();
        }

        // ── Vault CRUD ────────────────────────────────────────────────────────
        /// <summary>
        /// Updates a vault's StorageMode in the runtime list and persists to vaults.json.
        /// Called after a successful DB promotion to flip the badge from FILE to DB.
        /// </summary>
        public async Task UpdateVaultStorageModeAsync(string vaultId, string storageMode)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return;
            vault.StorageMode = storageMode;
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
        }

        /// <summary>
        /// Receives a pre-built VaultState after durable persistence succeeds and updates UI runtime state plus vaults.json mirror.
        /// </summary>
        public async Task CreateVaultAsync(VaultState vault)
        {
            Vaults.Add(vault);
            SelectVault(vault.VaultId);
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
        }

        public async Task<bool> RenameVaultAsync(string vaultId, string name)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null || string.IsNullOrWhiteSpace(name)) return false;
            vault.Name = name.Trim();
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        public async Task<bool> RemoveVaultAsync(string vaultId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return false;
            Vaults.Remove(vault);
            if (ActiveVaultId == vaultId)
            {
                var next = Vaults.FirstOrDefault();
                ActiveVaultId = next?.VaultId;
                ActiveProjectId = next?.Projects.FirstOrDefault()?.ProjectId;
                ActiveWorkspaceSessionId = GetMostRecentSession(next)?.SessionId;
            }
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        // ── Project CRUD ──────────────────────────────────────────────────────
        /// <summary>
        /// Receives a pre-built ProjectState after durable persistence succeeds and updates UI runtime state plus vaults.json mirror.
        /// </summary>
        public async Task CreateProjectAsync(string vaultId, ProjectState project)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return;
            vault.Projects.Add(project);
            SelectProject(vaultId, project.ProjectId);
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
        }

        public async Task<bool> RenameProjectAsync(string vaultId, string projectId, string name)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null || string.IsNullOrWhiteSpace(name)) return false;
            project.Name = name.Trim();
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        public async Task<bool> RemoveProjectAsync(string vaultId, string projectId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null) return false;
            vault!.Projects.Remove(project);
            if (ActiveProjectId == projectId)
            {
                ActiveProjectId = null;
                ActiveWorkspaceSessionId = null;
            }
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            InitializeWorkspaceState();
            Notify();
            return true;
        }

        public async Task<bool> AddProjectFileRefAsync(string path, string? name = null)
        {
            var project = ActiveProject;
            if (project == null || string.IsNullOrWhiteSpace(path)) return false;
            project.FileRefs.Add(new FileRef
            {
                Path = path,
                Name = string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileName(path) : name.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        public async Task<FileRef?> AddPlaceholderFileToActiveProjectAsync()
        {
            var project = ActiveProject;
            if (project == null) return null;
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var fileRef = new FileRef
            {
                Name = $"workspace-note-{stamp}.md",
                Path = $@"vault://{ActiveVaultId}/{project.ProjectId}/workspace-note-{stamp}.md",
                CreatedAt = DateTime.UtcNow
            };
            project.FileRefs.Add(fileRef);
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
            return fileRef;
        }

        public async Task<string?> ToggleKnowledgeBaseForActiveProjectAsync()
        {
            var project = ActiveProject;
            if (project == null) return null;
            project.KnowledgeBaseId = string.IsNullOrWhiteSpace(project.KnowledgeBaseId)
                ? $"kb-{project.ProjectId[..Math.Min(8, project.ProjectId.Length)]}"
                : null;
            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            Notify();
            return project.KnowledgeBaseId;
        }

        // ── Session CRUD ──────────────────────────────────────────────────────
        /// <summary>
        /// Receives a pre-built SessionState after durable persistence succeeds and updates UI runtime state plus vaults.json mirror.
        /// </summary>
        public async Task CreateWorkspaceSessionAsync(string vaultId, string? projectId, SessionState session)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return;
            var project = string.IsNullOrWhiteSpace(projectId) ? null
                : vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);

            if (project == null) vault.Sessions.Add(session);
            else project.Sessions.Add(session);

            _ = SelectSessionAsync(vaultId, projectId, session.SessionId);
            await _vaultWorkspace.SaveSessionAsync(vaultId, projectId, session);
            Notify();
        }

        public async Task<bool> RenameSessionAsync(
            string vaultId, string? projectId, string sessionId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var session = FindSession(vaultId, projectId, sessionId);
            if (session == null) return false;
            session.Name = name.Trim();
            await _vaultWorkspace.SaveSessionAsync(vaultId, projectId, session);
            Notify();
            return true;
        }

        public async Task<bool> RemoveSessionAsync(
            string vaultId, string? projectId, string sessionId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return false;

            SessionState? session;
            if (string.IsNullOrWhiteSpace(projectId))
            {
                session = vault.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session == null) return false;
                vault.Sessions.Remove(session);
            }
            else
            {
                var project = vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);
                session = project?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session == null || project == null) return false;
                project.Sessions.Remove(session);
            }

            if (ActiveWorkspaceSessionId == sessionId)
                ActiveWorkspaceSessionId = null;

            await _vaultWorkspace.SaveVaultsAsync(Vaults);
            InitializeWorkspaceState();
            Notify();
            return true;
        }

        public async Task<bool> SaveActiveWorkspaceSessionAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null || ActiveVaultId == null) return false;
            session.LastActiveAt = DateTime.UtcNow;
            EnsureSessionModelBindings(session);

            var storageMode = ActiveVault?.StorageMode ?? "Database";

            // Primary: persist model state to DB (or file session header).
            // Wrapped so a DB outage never breaks the save path — JSON backup covers it.
            try
            {
                await _vaultSync.UpdateSessionModelStateAsync(
                    ActiveVaultId,
                    session.SessionId,
                    storageMode,
                    session.AttachedModelIds.ToList(),
                    session.BroadcastGroupIds.ToList(),
                    session.DefaultModelId);
            }
            catch (Exception ex)
            {
                AppendMemory($"SaveActiveWorkspaceSessionAsync: DB model state update failed — JSON backup will cover it. {ex.Message}");
            }

            // Backup: mirror to session-model-state.json for offline resilience.
            await _sessionModelStateStore.SaveAsync(new SessionModelStateRecord
            {
                VaultId           = ActiveVaultId,
                ProjectId         = ActiveProjectId,
                SessionId         = session.SessionId,
                AttachedModelIds  = session.AttachedModelIds.ToList(),
                BroadcastGroupIds = session.BroadcastGroupIds.ToList(),
                DefaultModelId    = session.DefaultModelId,
                ModelBindings     = session.ModelBindings.Select(CopyBinding).ToList()
            });

            await _vaultWorkspace.SaveSessionAsync(ActiveVaultId, ActiveProjectId, session);
            Notify();
            return true;
        }

        public async Task<bool> RenameActiveWorkspaceSessionAsync(string name)
        {
            var session = ActiveWorkspaceSession;
            if (session == null || string.IsNullOrWhiteSpace(name)) return false;
            session.Name = name.Trim();
            return await SaveActiveWorkspaceSessionAsync();
        }

        // ── Session utilities ─────────────────────────────────────────────────
        public async Task<string?> AttachNextModelToActiveSessionAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null) return null;
            var next = GetAttachableModels()
                .FirstOrDefault(id => !session.AttachedModelIds.Contains(id, StringComparer.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(next)) return null;
            return await AttachModelToActiveSessionAsync(next) ? next : null;
        }

        public bool IsModelAttachedToActiveSession(string modelId)
            => CurrentSession?.AttachedModelIds.Contains(modelId, StringComparer.OrdinalIgnoreCase) == true;

        public bool IsDefaultModelForActiveSession(string modelId)
            => string.Equals(CurrentSession?.DefaultModelId, modelId, StringComparison.OrdinalIgnoreCase);

        public async Task<bool> AttachModelToActiveSessionAsync(string modelId)
        {
            var session = ActiveWorkspaceSession;
            if (session == null || string.IsNullOrWhiteSpace(modelId)) return false;

            var normalized = modelId.Trim();
            if (FindModelProfile(normalized) == null) return false;

            if (!session.AttachedModelIds.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                session.AttachedModelIds.Add(normalized);

            session.DefaultModelId ??= normalized;
            EnsureSessionModelBindings(session);
            EnsureRuntimeSessionForModel(normalized);
            SetRuntimeActiveModel(normalized);

            await SaveActiveWorkspaceSessionAsync();
            Notify();
            return true;
        }

        public async Task<bool> DetachModelFromActiveSessionAsync(string modelId)
        {
            var session = ActiveWorkspaceSession;
            if (session == null || string.IsNullOrWhiteSpace(modelId)) return false;

            session.AttachedModelIds = session.AttachedModelIds
                .Where(id => !id.Equals(modelId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            session.BroadcastGroupIds = session.BroadcastGroupIds
                .Where(id => !id.Equals(modelId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (string.Equals(session.DefaultModelId, modelId, StringComparison.OrdinalIgnoreCase))
                session.DefaultModelId = session.AttachedModelIds.FirstOrDefault();

            session.ModelBindings = session.ModelBindings
                .Where(binding => !binding.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase))
                .ToList();
            EnsureSessionModelBindings(session);
            CloseRuntimeSessionForModel(modelId);
            if (!string.IsNullOrWhiteSpace(session.DefaultModelId))
                SetRuntimeActiveModel(session.DefaultModelId);

            await SaveActiveWorkspaceSessionAsync();
            Notify();
            return true;
        }

        public async Task<bool> SetDefaultModelForActiveSessionAsync(string modelId)
        {
            var session = ActiveWorkspaceSession;
            if (session == null || string.IsNullOrWhiteSpace(modelId)) return false;
            if (!session.AttachedModelIds.Contains(modelId, StringComparer.OrdinalIgnoreCase)) return false;

            session.DefaultModelId = modelId;
            EnsureSessionModelBindings(session);
            EnsureRuntimeSessionForModel(modelId);
            SetRuntimeActiveModel(modelId);
            await SaveActiveWorkspaceSessionAsync();
            Notify();
            return true;
        }

        public async Task<bool> SetBroadcastForActiveSessionAsync(bool enabled)
        {
            var session = ActiveWorkspaceSession;
            if (session == null) return false;

            session.BroadcastGroupIds = enabled
                ? session.AttachedModelIds.ToList()
                : new List<string>();

            EnsureSessionModelBindings(session);
            SyncRuntimeModelSessionsFromActiveSession();
            await SaveActiveWorkspaceSessionAsync();
            Notify();
            return true;
        }

        public async Task<string?> CycleActiveSessionLayoutAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null) return null;
            session.Canvas.GlobalBarPosition = session.Canvas.GlobalBarPosition switch
            {
                "Bottom" => "Top",
                "Top" => "Floating",
                _ => "Bottom"
            };
            session.LastActiveAt = DateTime.UtcNow;
            await SaveActiveWorkspaceSessionAsync();
            return session.Canvas.GlobalBarPosition;
        }

        // ── Selection ─────────────────────────────────────────────────────────
        public void SelectVault(string vaultId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return;
            ActiveVaultId = vault.VaultId;
            ActiveProjectId = vault.Projects.FirstOrDefault()?.ProjectId;
            ActiveWorkspaceSessionId = GetMostRecentSession(vault)?.SessionId;
            if (vault.Sessions.Any() && vault.Sessions.Any(s => s.SessionId == ActiveWorkspaceSessionId))
                ActiveProjectId = null;
            _ = PersistSelectionSafeAsync();
            Notify();
        }

        public void SelectProject(string vaultId, string projectId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null) return;
            ActiveVaultId = vaultId;
            ActiveProjectId = project.ProjectId;
            ActiveWorkspaceSessionId = project.Sessions
                .OrderByDescending(s => s.LastActiveAt)
                .FirstOrDefault()?.SessionId;
            _ = PersistSelectionSafeAsync();
            Notify();
        }

        public async Task SelectSessionAsync(string vaultId, string? projectId, string sessionId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            ProjectState? project = null;
            SessionState? session;

            if (string.IsNullOrWhiteSpace(projectId))
                session = vault?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            else
            {
                project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
                session = project?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            }

            if (session == null) return;

            await _sessionModelStateStore.ApplyToSessionAsync(vaultId, project?.ProjectId, session);

            ActiveVaultId = vaultId;
            ActiveProjectId = project?.ProjectId;
            ActiveWorkspaceSessionId = sessionId;
            session.LastActiveAt = DateTime.UtcNow;

            // Keep ChatConversationState aware of active session for log routing.
            _chatState.ActiveSessionId   = sessionId;
            _chatState.ActiveSessionName = session.Name;
            _chatState.ActiveVaultId     = vaultId;
            _chatState.ActiveProjectId   = project?.ProjectId;

            await PersistSelectionAsync();

            // Only sync runtime sessions if the connection is established.
            // IsConnected = true only after RegisterProfileAsync has run for all profiles,
            // If called before connection, sessions would be created with an unregistered
            // RestoreStartupSessions handles the startup case after connection completes.
            if (IsConnected)
                SyncRuntimeModelSessionsFromActiveSession();

            // Sync model state to whichever store didn't have it.
            // If DB provided the IDs → JSON may be stale or empty → write JSON.
            // If JSON provided the IDs → DB was empty → write DB.
            // Either way, both stores end up in agreement after selection.
            if (session.AttachedModelIds.Any())
            {
                var storageMode = vault?.StorageMode ?? "Database";

                try
                {
                    await _vaultSync.UpdateSessionModelStateAsync(
                        vaultId, sessionId, storageMode,
                        session.AttachedModelIds.ToList(),
                        session.BroadcastGroupIds.ToList(),
                        session.DefaultModelId);
                }
                catch { /* DB unavailable — JSON backup still has correct data */ }

                await _sessionModelStateStore.SaveAsync(new SessionModelStateRecord
                {
                    VaultId           = vaultId,
                    ProjectId         = project?.ProjectId,
                    SessionId         = sessionId,
                    AttachedModelIds  = session.AttachedModelIds.ToList(),
                    BroadcastGroupIds = session.BroadcastGroupIds.ToList(),
                    DefaultModelId    = session.DefaultModelId
                });
            }

            await RestoreActiveChatHistoryAsync();
            Notify();
        }

        private async Task PersistSelectionAsync()
        {
            await _session.SetAsync(SessionStorageKeys.VaultSelectedVaultId,   ActiveVaultId   ?? string.Empty);
            await _session.SetAsync(SessionStorageKeys.VaultSelectedProjectId, ActiveProjectId ?? string.Empty);
            await _session.SetAsync(SessionStorageKeys.VaultSelectedSessionId, ActiveWorkspaceSessionId ?? string.Empty);
        }

        private async Task PersistSelectionSafeAsync()
        {
            try { await PersistSelectionAsync(); }
            catch { /* circuit may be disposed during re-render */ }
        }

        // ── Canvas ────────────────────────────────────────────────────────────
        // TODO Step 6: Move to CanvasRuntimeState / Canvas Actions
        public void SaveCanvasState() => SaveSettings();

        public void AddCard(string cardType = "SingleModel")
        {
            var offset = CurrentSession!.Canvas.Cards.Count * 32;
            var card = new CardState
            {
                CardType = cardType,
                X = 80 + offset,
                Y = 80 + offset,
                Width = cardType == "Broadcast" ? 520 : 380,
                ZIndex = CurrentSession.Canvas.Cards.Count + 1
            };

            if (cardType == "Broadcast")
                card.ModelProfileIds.AddRange(Sessions.AvailableModels.Select(m => m.Model.Id).Take(4));
            else
            {
                var first = Sessions.AvailableModels.Select(m => m.Model.Id).FirstOrDefault();
                if (first != null) card.ModelProfileIds.Add(first);
            }

            CurrentSession.Canvas.Cards.Add(card);
            Notify();
        }

        public void RemoveCard(string cardId)
        {
            var card = CurrentSession?.Canvas.Cards.FirstOrDefault(c => c.CardId == cardId);
            if (card == null) return;
            CurrentSession!.Canvas.Cards.Remove(card);
            SaveCanvasState();
            Notify();
        }

        public async Task<CardState?> AddCardToActiveSessionAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null) return null;
            var nextZ = session.Canvas.Cards.Count == 0
                ? 1
                : session.Canvas.Cards.Max(c => c.ZIndex) + 1;

            var card = new CardState
            {
                Title = $"Card {session.Canvas.Cards.Count + 1}",
                CardType = session.AttachedModelIds.Count > 1 ? "Broadcast" : "SingleModel",
                X = 40 + (session.Canvas.Cards.Count * 28),
                Y = 40 + (session.Canvas.Cards.Count * 20),
                ZIndex = nextZ,
                ModelProfileIds = session.AttachedModelIds.ToList(),
                ActiveProfileId = session.AttachedModelIds.FirstOrDefault()
            };

            session.Canvas.Cards.Add(card);
            session.Canvas.ActiveCardId = card.CardId;
            session.LastActiveAt = DateTime.UtcNow;
            await SaveActiveWorkspaceSessionAsync();
            return card;
        }

        public string GetModelName(string profileId)
        {
            var definition = AppSettings.ModelDefinitions
                .FirstOrDefault(model => model.ModelId.Equals(profileId, StringComparison.OrdinalIgnoreCase));
            if (definition != null)
                return definition.DisplayName;

            var match = LLMProfiles
                .SelectMany(profile => profile.Models)
                .FirstOrDefault(model => model.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
            return match?.Label ?? profileId;
        }

        /// <summary>
        /// Gets the configured maximum input character count for a model. Returns zero when no explicit limit is configured.
        /// </summary>
        public int GetModelCharacterLimit(string? modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return 0;

            var definition = AppSettings.ModelDefinitions.FirstOrDefault(model =>
                model.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));

            return Math.Max(0, definition?.MaxInputCharacters ?? 0);
        }

        /// <summary>
        /// Gets the effective character limit for a set of target models. The smallest configured limit wins.
        /// </summary>
        public int GetCharacterLimitForModels(IEnumerable<string> modelIds)
        {
            var limits = modelIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(GetModelCharacterLimit)
                .Where(limit => limit > 0)
                .ToList();

            return limits.Count == 0 ? 0 : limits.Min();
        }

        /// <summary>
        /// Gets the model ids targeted by the main chat input for the active workspace session.
        /// </summary>
        public IReadOnlyList<string> GetActiveChatTargetModelIds()
        {
            var workspaceSession = ActiveWorkspaceSession;
            if (workspaceSession == null)
                return Array.Empty<string>();

            if (workspaceSession.BroadcastGroupIds.Any())
                return workspaceSession.BroadcastGroupIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (!string.IsNullOrWhiteSpace(workspaceSession.DefaultModelId))
                return new List<string> { workspaceSession.DefaultModelId };

            return workspaceSession.AttachedModelIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Take(1)
                .ToList();
        }

        /// <summary>
        /// Gets the effective character limit for the main chat input. Returns zero when no target model has a limit.
        /// </summary>
        public int GetActivePromptCharacterLimit()
            => GetCharacterLimitForModels(GetActiveChatTargetModelIds());

        /// <summary>
        /// Validates prompt text against configured target model character limits.
        /// </summary>
        public bool TryGetCharacterLimitWarning(string prompt, IEnumerable<string> modelIds, out string warning)
        {
            var limit = GetCharacterLimitForModels(modelIds);
            var length = prompt?.Length ?? 0;
            if (limit <= 0 || length <= limit)
            {
                warning = string.Empty;
                return false;
            }

            warning = $"Prompt is {length:N0} characters and exceeds the configured model limit of {limit:N0}.";
            return true;
        }

        public RuntimeContextSettings GetRuntimeContextSettings(string sessionId, string modelId)
        {
            var session = FindSessionForRuntimeSettings(sessionId) ?? ActiveWorkspaceSession;
            if (session == null || string.IsNullOrWhiteSpace(modelId))
                return new RuntimeContextSettings();

            EnsureSessionModelBindings(session);
            return CopyRuntimeContextSettings(
                session.ModelBindings.FirstOrDefault(binding =>
                    binding.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase))
                ?.RuntimeContextSettings ?? new RuntimeContextSettings());
        }

        public async Task<bool> UpdateRuntimeContextSettingsAsync(
            string sessionId,
            string modelId,
            RuntimeContextSettings settings)
        {
            var session = FindSessionForRuntimeSettings(sessionId);
            if (session == null || string.IsNullOrWhiteSpace(modelId)) return false;

            EnsureSessionModelBindings(session);
            var binding = session.ModelBindings.FirstOrDefault(item =>
                item.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
            if (binding == null) return false;

            binding.RuntimeContextSettings = CopyRuntimeContextSettings(settings ?? new RuntimeContextSettings());
            return await SaveActiveWorkspaceSessionAsync();
        }

        // ── LLM / Connection ──────────────────────────────────────────────────
        // TODO Step 6: Move to ConnectAction, ConnectProfileAction, TestEndpointAction
        public async Task ConnectAsync(CancellationToken token = default)
        {
            RestoreSelectedLLMProfile();
            AppendMemory($"ConnectAsync: {LLMProfiles.Count} profile(s) in settings.");

            // Register every configured profile so all routing adapters (OpenAI, etc.)
            // are initialized and available. The selected profile drives default chat routing —
            // it does not limit which profiles get connected.
            var profiles = LLMProfiles.ToList();
            AppendMemory($"Registering {profiles.Count} profile(s). SelectedLLMProfile: {SelectedLLMProfile?.Name ?? "null"}.");

            if (!profiles.Any())
            {
                AppendMemory("ConnectAsync: no profiles configured.");
                UpdateStatus("Error", "No profile", "No LLM profiles found in settings.");
                return;
            }

            try
            {
                UpdateStatus("Connecting", "Connecting", $"Connecting {profiles.Count} profile(s)...");

                foreach (var profile in profiles)
                {
                    AppendMemory($"Registering profile: {profile.Name} ({profile.ResolvedProvider})...");
                    await Sessions.RegisterProfileAsync(profile, token);
                    AppendMemory($"{profile.Name} connected - {profile.Models.Count} model(s) available.");
                }

                _settingsState.SetConnected(true);
                var totalModels = profiles.Sum(p => p.Models.Count);
                RestoreStartupSessions(profiles);
                UpdateStatus("UPS", "Connected", $"{profiles.Count} profile(s), {totalModels} model(s)");
                Notify();
            }
            catch (Exception ex)
            {
                _settingsState.SetConnected(false);
                AppendMemory($"ConnectAsync failed: {ex.GetType().Name}: {ex.Message}");
                UpdateStatus("UPS", "Failed", ex.Message);
                AppendOutput(new OutputSegment { Type = "error", Value = ex.Message });
            }
        }

        public async Task ConnectProfileAsync(LLMProfile profile, CancellationToken token = default)
        {
            try
            {
                SelectedLLMProfile = profile;
                _settingsService.AppSettings.SelectedLLMProfileName = profile.Name;
                SaveSettings();
                AppendMemory($"ConnectProfileAsync: {profile.Name} ({profile.ResolvedProvider})...");
                UpdateStatus("Connecting", "Connecting", $"Connecting to {profile.Name}...");
                await Sessions.RegisterProfileAsync(profile, token);
                _settingsState.SetConnected(true);
                RestoreStartupSessions(new[] { profile });
                UpdateStatus("UPS", "Connected", $"{profile.Name} - {profile.Models.Count} model(s) available.");
                AppendMemory($"{profile.Name} connected - {profile.Models.Count} model(s) available.");
                Notify();
            }
            catch (Exception ex)
            {
                AppendMemory($"ConnectProfileAsync failed: {ex.GetType().Name}: {ex.Message}");
                UpdateStatus("UPS", "Failed", ex.Message);
                AppendOutput(new OutputSegment { Type = "error", Value = ex.Message });
            }
        }

        public async Task<TestResult> TestEndpointAsync(LLMProfile profile, CancellationToken token = default)
        {
            try
            {
                var client = new EndpointClientService();
                var ok = await client.ConnectAsync(null, profile, token);
                return new TestResult
                {
                    Success = ok,
                    Message = ok ? $"Endpoint reachable: {profile.Endpoint}" : $"Could not reach endpoint: {profile.Endpoint}"
                };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<TestResult> TestModelAsync(
            LLMProfile profile, ModelProfile model, CancellationToken token = default)
        {
            try
            {
                await Sessions.RegisterProfileAsync(profile, token);
                var session = Sessions.CreateSession(profile, model);
                await session.SendAsync("This is a model connection test.", token);
                var error = session.LastError;
                var reply = session.Output.LastOrDefault(s => s.Type != "error")?.Value ?? string.Empty;
                Sessions.CloseSession(session.SessionId);
                return new TestResult
                {
                    Success = string.IsNullOrWhiteSpace(error),
                    Message = string.IsNullOrWhiteSpace(error)
                        ? $"{model.Label} responded: {reply[..Math.Min(80, reply.Length)]}..."
                        : $"{model.Label}: {error}"
                };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }

        // ── Send / Broadcast ──────────────────────────────────────────────────
        public async Task<string> SendPromptAsync(string prompt, CancellationToken token = default)
        {
            if (!IsConnected)
            {
                const string warning = "Not connected - go to Settings and click Connect.";
                AppendOutput(new OutputSegment { Type = "system", Value = warning });
                return warning;
            }

            var targets = GetMainChatSessions().ToList();
            if (!targets.Any())
            {
                const string warning = "No active session - pick a model in Chat and click Open.";
                AppendOutput(new OutputSegment { Type = "system", Value = warning });
                return warning;
            }

            var targetModelIds = targets
                .Select(s => s.ModelProfile?.Id ?? s.ModelEntry.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToList();
            if (TryGetCharacterLimitWarning(prompt, targetModelIds, out var characterWarning))
            {
                AppendOutput(new OutputSegment { Type = "system", Value = characterWarning });
                return characterWarning;
            }

            AppendMemory($"Prompt: {prompt}");
            var turnId = _ids.NewTurnId();
            var ctx    = CaptureChatContext();
            _chatState.SetChatSending(true, ctx.SessionId);
            Notify();

            foreach (var s in targets)
            {
                var modelId = s.ModelProfile?.Id ?? s.ModelEntry.Id;
                AppendUserConversationMessage(modelId, prompt, s.DisplayName, turnId);
            }

            await Task.WhenAll(targets.Select(s => s.SendAsync(prompt, token)));

            var lastReply = string.Empty;

            foreach (var s in targets)
            {
                var modelId = s.ModelProfile?.Id ?? s.ModelEntry.Id;
                var error = s.LastError;
                if (!string.IsNullOrWhiteSpace(error))
                {
                    AppendAssistantConversationMessage(modelId, error, s.DisplayName, isError: true, turnId: turnId, sessionContext: ctx);
                    continue;
                }

                var reply = s.Output.LastOrDefault(seg => seg.Type != "error")?.Value ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    AppendAssistantConversationMessage(modelId, reply, s.DisplayName, turnId: turnId, sessionContext: ctx);
                    AppendMemory($"{s.DisplayName}: {reply}");
                    lastReply = reply;
                }
            }

            _chatState.SetChatSending(false, null);
            Notify();
            return lastReply;
        }

        public async Task BroadcastPromptAsync(string prompt, CancellationToken token = default)
        {
            if (!IsConnected)
            {
                AppendOutput(new OutputSegment { Type = "system", Value = "Not connected. Please connect in Settings first." });
                return;
            }

            AppendOutput(new OutputSegment { Type = "text", Value = $"You (broadcast): {prompt}" });
            AppendMemory($"Broadcast: {prompt}");
            await Sessions.BroadcastAsync(prompt, token);
            Notify();
        }

        // ── Settings ──────────────────────────────────────────────────────────
        public void SaveSettings()
        {
            var vaultSaved = SaveProfilesToVaultAsync().GetAwaiter().GetResult();

            var savedProviders = _settingsService.AppSettings.ProviderProfiles;
            var savedModels = _settingsService.AppSettings.ModelDefinitions;
            var savedLLMProfiles = _settingsService.AppSettings.LLMProfiles;

            _settingsService.AppSettings.ProviderProfiles = new();
            _settingsService.AppSettings.ModelDefinitions = new();
            _settingsService.AppSettings.LLMProfiles = new();

            _settingsService.SaveSettings();

            _settingsService.AppSettings.ProviderProfiles = savedProviders;
            _settingsService.AppSettings.ModelDefinitions = savedModels;
            _settingsService.AppSettings.LLMProfiles = savedLLMProfiles;
        }

        public async Task<TestResult> VerifyPersistenceAsync()
        {
            try
            {
                var verifier = new StatePersistenceVerifier(_vaultWorkspace);
                var result = await verifier.VerifyAsync();
                return new TestResult { Success = result.Success, Message = result.Message };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = $"Persistence verification failed: {ex.Message}" };
            }
        }

        // ── UPS extract helper ────────────────────────────────────────────────
        public static string ExtractContent(UPSResponse response)
        {
            if (!response.Success) return response.ErrorMessage ?? "Error";
            if (!string.IsNullOrEmpty(response.Content)) return response.Content;
            var param = response.Payload.FirstOrDefault(p =>
                p.Key == "content" || p.Key == "assistantMessage" ||
                p.Key == "reply" || p.Key == "text" || p.Type == "string");
            return param?.Value?.ToString() ?? string.Empty;
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private static AppUser BuildCurrentUser()
        {
            var userName = Environment.UserName;
            var safe = string.IsNullOrWhiteSpace(userName) ? "AVA User" : userName.Trim();
            return new AppUser
            {
                Name = safe,
                Email = $"{safe.Replace(' ', '.').ToLowerInvariant()}@local"
            };
        }

        private void InitializeWorkspaceState()
        {
            EnsureWorkspaceDefaults();

            var firstVault = Vaults.FirstOrDefault();
            ActiveVaultId ??= firstVault?.VaultId;

            var activeVault = Vaults.FirstOrDefault(v => v.VaultId == ActiveVaultId) ?? firstVault;
            ActiveVaultId = activeVault?.VaultId;

            var firstProject = activeVault?.Projects.FirstOrDefault();
            ActiveProjectId ??= firstProject?.ProjectId;

            var activeProject = activeVault?.Projects.FirstOrDefault(p => p.ProjectId == ActiveProjectId) ?? firstProject;
            ActiveProjectId = activeProject?.ProjectId;

            if (activeVault != null)
            {
                if (!string.IsNullOrWhiteSpace(ActiveWorkspaceSessionId))
                {
                    var rootSession = activeVault.Sessions
                        .FirstOrDefault(s => s.SessionId == ActiveWorkspaceSessionId);
                    var owningProject = rootSession == null
                        ? activeVault.Projects.FirstOrDefault(p =>
                            p.Sessions.Any(s => s.SessionId == ActiveWorkspaceSessionId))
                        : null;

                    if (rootSession != null)
                    {
                        ActiveProjectId = null;
                    }
                    else if (owningProject != null)
                    {
                        ActiveProjectId = owningProject.ProjectId;
                    }
                    else
                    {
                        ActiveWorkspaceSessionId = null;
                    }
                }

                if (string.IsNullOrWhiteSpace(ActiveWorkspaceSessionId))
                {
                    var recentVault = activeVault.Sessions.OrderByDescending(s => s.LastActiveAt).FirstOrDefault();
                    var recentProject = activeProject?.Sessions.OrderByDescending(s => s.LastActiveAt).FirstOrDefault();

                    var mostRecent = recentVault == null ? recentProject
                        : recentProject == null ? recentVault
                        : recentVault.LastActiveAt >= recentProject.LastActiveAt ? recentVault : recentProject;

                    ActiveWorkspaceSessionId = mostRecent?.SessionId;

                    if (mostRecent != null && activeVault.Sessions.Any(s => s.SessionId == mostRecent.SessionId))
                        ActiveProjectId = null;
                }
            }

            var activeSession = ActiveWorkspaceSession;
            if (activeSession != null)
            {
                _chatState.ActiveSessionId   = activeSession.SessionId;
                _chatState.ActiveSessionName = activeSession.Name;
                _chatState.ActiveVaultId     = ActiveVaultId;
                _chatState.ActiveProjectId   = ActiveProjectId;
                _ = RestoreActiveChatHistoryAsync();
            }

            SyncRuntimeModelSessionsFromActiveSession();
            Notify();
        }

        private void EnsureWorkspaceDefaults()
        {
            if (Vaults.Count > 0) return;

            var now = DateTime.UtcNow;
            var session = new SessionState { Name = "Session 1", CreatedAt = now, LastActiveAt = now };
            var project = new ProjectState { Name = "Project 1", IsExpanded = true, Sessions = new List<SessionState> { session } };
            var vault = new VaultState { Name = "Vault 1", IsExpanded = true, Projects = new List<ProjectState> { project } };

            Vaults.Add(vault);
            _vaultWorkspace.SaveVaultsAsync(Vaults).GetAwaiter().GetResult();
        }

        private async Task RestoreActiveChatHistoryAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null || string.IsNullOrWhiteSpace(ActiveVaultId)) return;

            _chatState.ActiveSessionId = session.SessionId;
            _chatState.ActiveSessionName = session.Name;
            _chatState.ActiveVaultId = ActiveVaultId;
            _chatState.ActiveProjectId = ActiveProjectId;

            await _chatState.LoadSelectedSessionAsync(session.SessionId);
        }

        private static SessionState? GetMostRecentSession(VaultState? vault)
        {
            if (vault == null) return null;
            var vs = vault.Sessions.OrderByDescending(s => s.LastActiveAt).FirstOrDefault();
            var ps = vault.Projects.SelectMany(p => p.Sessions).OrderByDescending(s => s.LastActiveAt).FirstOrDefault();
            if (vs == null) return ps;
            if (ps == null) return vs;
            return vs.LastActiveAt >= ps.LastActiveAt ? vs : ps;
        }

        private SessionState? FindSession(string vaultId, string? projectId, string sessionId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return null;
            if (string.IsNullOrWhiteSpace(projectId))
                return vault.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            return vault.Projects.FirstOrDefault(p => p.ProjectId == projectId)
                ?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }

        private SessionState? FindSessionForRuntimeSettings(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return null;

            return Vaults.SelectMany(vault => vault.Sessions)
                .Concat(Vaults.SelectMany(vault => vault.Projects).SelectMany(project => project.Sessions))
                .FirstOrDefault(session => session.SessionId.Equals(sessionId, StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureSessionModelBindings(SessionState session)
        {
            var attachedIds = session.AttachedModelIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            session.AttachedModelIds = attachedIds;
            session.BroadcastGroupIds = session.BroadcastGroupIds
                .Where(id => attachedIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!string.IsNullOrWhiteSpace(session.DefaultModelId) &&
                !attachedIds.Contains(session.DefaultModelId, StringComparer.OrdinalIgnoreCase))
            {
                session.DefaultModelId = attachedIds.FirstOrDefault();
            }

            session.ModelBindings = attachedIds.Select(modelId =>
            {
                var existing = session.ModelBindings.FirstOrDefault(binding =>
                    binding.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
                var binding = existing == null
                    ? new SessionModelBinding { ModelId = modelId }
                    : CopyBinding(existing);

                binding.ModelId = modelId;
                binding.ProviderProfileId = string.IsNullOrWhiteSpace(binding.ProviderProfileId)
                    ? GetProviderProfileIdForModel(modelId)
                    : binding.ProviderProfileId;
                binding.IsDefault = string.Equals(modelId, session.DefaultModelId, StringComparison.OrdinalIgnoreCase);
                binding.IsBroadcastEnabled = session.BroadcastGroupIds.Contains(modelId, StringComparer.OrdinalIgnoreCase);
                binding.RuntimeContextSettings ??= new RuntimeContextSettings();
                return binding;
            }).ToList();
        }

        private string GetProviderProfileIdForModel(string modelId)
        {
            var definition = AppSettings.ModelDefinitions.FirstOrDefault(model =>
                model.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
            if (definition != null)
                return definition.ProviderProfileId;

            return LLMProfiles.FirstOrDefault(profile =>
                profile.Models.Any(model => model.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase)))
                ?.ProfileId ?? string.Empty;
        }

        private static SessionModelBinding CopyBinding(SessionModelBinding binding) => new()
        {
            ModelId = binding.ModelId,
            ProviderProfileId = binding.ProviderProfileId,
            IsDefault = binding.IsDefault,
            IsBroadcastEnabled = binding.IsBroadcastEnabled,
            RuntimeContextSettings = CopyRuntimeContextSettings(binding.RuntimeContextSettings)
        };

        private static RuntimeContextSettings CopyRuntimeContextSettings(RuntimeContextSettings settings) => new()
        {
            HistoryPolicy = settings.HistoryPolicy,
            UseFullHistoryPayload = settings.UseFullHistoryPayload,
            IncludeConversationHistory = settings.IncludeConversationHistory,
            IncludeToolCalls = settings.IncludeToolCalls,
            IncludeToolMetadata = settings.IncludeToolMetadata,
            IncludeMetadata = settings.IncludeMetadata,
            EnableAutomaticCompression = settings.EnableAutomaticCompression,
            EnableMemoryInjection = settings.EnableMemoryInjection,
            MaxHistoryMessages = settings.MaxHistoryMessages,
            AllowManualHistorySelection = settings.AllowManualHistorySelection
        };

        private IEnumerable<string> GetAttachableModels()
        {
            var definitions = AppSettings.ModelDefinitions
                .Select(m => m.ModelId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (definitions.Any())
                return definitions;

            return LLMProfiles
                .SelectMany(p => p.Models)
                .Select(m => m.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private (LLMProfile Profile, ModelProfile Model)? FindModelProfile(string modelId)
        {
            foreach (var profile in LLMProfiles)
            {
                var model = profile.Models.FirstOrDefault(m =>
                    m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

                if (model != null)
                    return (profile, model);
            }

            return null;
        }

        private static string GetRuntimeModelId(UPSSession session)
            => session.ModelProfile?.Id ?? session.ModelEntry.Id;

        private UPSSession? FindRuntimeSessionForModel(string modelId)
        {
            return Sessions.Sessions.FirstOrDefault(session =>
                GetRuntimeModelId(session).Equals(modelId, StringComparison.OrdinalIgnoreCase));
        }

        private UPSSession? EnsureRuntimeSessionForModel(string modelId)
        {
            var existing = FindRuntimeSessionForModel(modelId);
            if (existing != null) return existing;

            var record = FindModelProfile(modelId);
            return record == null
                ? null
                : Sessions.CreateSession(record.Value.Profile, record.Value.Model);
        }

        private void CloseRuntimeSessionForModel(string modelId)
        {
            var sessionIds = Sessions.Sessions
                .Where(session => GetRuntimeModelId(session).Equals(modelId, StringComparison.OrdinalIgnoreCase))
                .Select(session => session.SessionId)
                .ToList();

            foreach (var sessionId in sessionIds)
                Sessions.CloseSession(sessionId);
        }

        private void SetRuntimeActiveModel(string modelId)
        {
            var session = EnsureRuntimeSessionForModel(modelId);
            if (session != null)
                Sessions.SetActiveSession(session.SessionId);
        }

        private void SyncRuntimeModelSessionsFromActiveSession()
        {
            var workspaceSession = ActiveWorkspaceSession;
            Sessions.CloseAll();

            if (workspaceSession == null) return;
            EnsureSessionModelBindings(workspaceSession);

            var attachedIds = workspaceSession.AttachedModelIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            workspaceSession.AttachedModelIds = attachedIds;
            workspaceSession.BroadcastGroupIds = workspaceSession.BroadcastGroupIds
                .Where(id => attachedIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!string.IsNullOrWhiteSpace(workspaceSession.DefaultModelId) &&
                !attachedIds.Contains(workspaceSession.DefaultModelId, StringComparer.OrdinalIgnoreCase))
            {
                workspaceSession.DefaultModelId = attachedIds.FirstOrDefault();
            }

            foreach (var modelId in attachedIds)
                EnsureRuntimeSessionForModel(modelId);

            Sessions.ClearBroadcast();
            foreach (var modelId in workspaceSession.BroadcastGroupIds)
            {
                var session = FindRuntimeSessionForModel(modelId);
                if (session != null)
                    Sessions.AddToBroadcast(session.SessionId);
            }

            var defaultModelId = workspaceSession.DefaultModelId ?? attachedIds.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(defaultModelId))
                SetRuntimeActiveModel(defaultModelId);
        }

        private void RestoreSelectedLLMProfile()
        {
            var preferred = _settingsService.AppSettings.SelectedLLMProfileName;
            SelectedLLMProfile =
                LLMProfiles.FirstOrDefault(p => !string.IsNullOrWhiteSpace(preferred) && p.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase))
                ?? LLMProfiles.FirstOrDefault(p => p.IsDefault)
                ?? LLMProfiles.FirstOrDefault();

            if (SelectedLLMProfile != null)
                _settingsService.AppSettings.SelectedLLMProfileName = SelectedLLMProfile.Name;
        }

        private void RestoreStartupSessions(IEnumerable<LLMProfile> connectedProfiles)
        {
            var workspaceSession = ActiveWorkspaceSession;

            if (workspaceSession == null)
                return;

            foreach (var modelId in workspaceSession.AttachedModelIds
                         .Where(id => !string.IsNullOrWhiteSpace(id))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                EnsureRuntimeSessionForModel(modelId);
            }

            SyncRuntimeModelSessionsFromActiveSession();
        }

        private IEnumerable<UPSSession> GetMainChatSessions()
        {
            var workspaceSession = ActiveWorkspaceSession;
            if (workspaceSession == null) return Enumerable.Empty<UPSSession>();

            var targetIds = workspaceSession.BroadcastGroupIds.Any()
                ? workspaceSession.BroadcastGroupIds
                : !string.IsNullOrWhiteSpace(workspaceSession.DefaultModelId)
                    ? new List<string> { workspaceSession.DefaultModelId }
                    : workspaceSession.AttachedModelIds.Take(1).ToList();

            return targetIds
                .Select(FindRuntimeSessionForModel)
                .Where(session => session?.IsActive == true)
                .Cast<UPSSession>();
        }

        public static Dictionary<string, object> BuildResponseMetadata(UPSResponse response)
        {
            var metadata = new Dictionary<string, object>
            {
                ["success"] = response.Success
            };

            if (!string.IsNullOrWhiteSpace(response.ModelId))
                metadata[ResponseMetadataKeys.ModelId] = response.ModelId;

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage))
                metadata["errorMessage"] = response.ErrorMessage;

            if (!string.IsNullOrWhiteSpace(response.ProviderResponse))
                metadata[ResponseMetadataKeys.RawResponseJson] = response.ProviderResponse;

            if (response.RespondedAt.HasValue)
                metadata[ResponseMetadataKeys.RequestEndUtc] = response.RespondedAt.Value.ToString("O");

            if (response.Payload.Count > 0)
            {
                metadata["payload"] = response.Payload.Select(param => new Dictionary<string, object?>
                {
                    ["key"] = param.Key,
                    ["type"] = param.Type,
                    ["value"] = param.Value
                }).ToList();
            }

            return metadata;
        }
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public void Deconstruct(out bool success, out string message)
        {
            success = Success;
            message = Message;
        }
    }

    public class ConsoleLogService : ILogService
    {
        public void LogInfo(string message) => Console.WriteLine("[INFO] " + message);
        public void LogWarning(string message) => Console.WriteLine("[WARN] " + message);
        public void LogError(string message) => Console.WriteLine("[ERROR] " + message);
    }
}

