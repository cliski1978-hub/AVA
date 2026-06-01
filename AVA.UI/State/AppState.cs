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
        private readonly VaultWorkspaceState _vaultWorkspace;
        private readonly IVaultUiSyncService _vaultSync;
        private readonly ErrorState _errorState;
        private readonly ISessionStorageService _session;
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
        public bool SettingsDirty { get; private set; }

        public bool UseDirectEndpoints
        {
            get => _settingsService.AppSettings.UseDirectEndpoints;
            set
            {
                if (_settingsService.AppSettings.UseDirectEndpoints == value)
                {
                    return;
                }

                _settingsService.AppSettings.UseDirectEndpoints = value;
                MarkSettingsDirty();
            }
        }

        // ── Navigation — delegates to NavigationState ─────────────────────────
        public string SelectedNavigationItem => _navState.SelectedNavigationItem;

        public void SetSelectedNavigationItem(string item)
            => _navState.SetSelectedNavigationItem(item);

        public void NavigateToNotes(string vaultId, string? projectId)
        {
            ActiveVaultId   = vaultId;
            ActiveProjectId = projectId;
            ActiveWorkspaceSessionId = null;
            SetSelectedNavigationItem("Notes");
            NotifyStateChanged();
        }

        public void NavigateToWorkflows(string vaultId, string? projectId)
        {
            ActiveVaultId   = vaultId;
            ActiveProjectId = projectId;
            ActiveWorkspaceSessionId = null;
            SetSelectedNavigationItem("Workflows");
            NotifyStateChanged();
        }

        public void NavigateToSessions(string vaultId, string? projectId)
        {
            ActiveVaultId   = vaultId;
            ActiveProjectId = projectId;
            ActiveWorkspaceSessionId = null;
            SetSelectedNavigationItem("Sessions");
            NotifyStateChanged();
        }

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
            VaultWorkspaceState vaultWorkspace,
            IVaultUiSyncService vaultSync,
            ErrorState errorState,
            ISessionStorageService session,
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

            UPSClient = new UPSClientService();
            Sessions = new SessionManager(UPSClient);
            Sessions.OnChange += Notify;

            RestoreSelectedLLMProfile();

            AppendMemory("Memory log initialized.");
            AppendReflection("Reflection system initialized.");
            AppendOutput(new OutputSegment { Type = "system", Value = "AVA ready." });
        }

        public void NotifyStateChanged() => Notify();

        // ── Vault-backed profile management ──────────────────────────────────────
        public async Task LoadProfilesFromVaultAsync(CancellationToken ct = default)
        {
            const string source = "ProfilePersistence";

            try
            {
                _errorState.ClearSource(source);

                var providers = await _profileService.GetAllProviderProfilesAsync(ct);

                _settingsService.AppSettings.ProviderProfiles = providers;
                _settingsService.AppSettings.LLMProfiles = new();

                var models = new List<ModelDefinition>();
                foreach (var provider in providers)
                {
                    var providerModels = await _profileService.GetModelsByProfileIdAsync(provider.ProviderProfileId, ct);
                    models.AddRange(providerModels);
                }
                _settingsService.AppSettings.ModelDefinitions = models;
                SettingsArchitectureMigration.Normalize(_settingsService.AppSettings);

                if (providers.Count == 0)
                {
                    AppendMemory("No provider profiles are configured in the Vault database yet.");
                    _errorState.AddError(
                        "No provider profiles are configured yet. Open Settings and add a provider profile to get started.",
                        source: source,
                        feature: "Settings",
                        severity: AppErrorSeverity.Info);
                }
            }
            catch (Exception ex)
            {
                AppendMemory($"Vault profile load failed: {ex.Message}");
                _errorState.AddError(
                    "Provider profiles could not be loaded from the database. Database may be unavailable.",
                    source: source,
                    feature: "Settings",
                    severity: AppErrorSeverity.Error);
            }
        }

        private async Task<bool> SaveProfilesToVaultAsync()
        {
            try
            {
                foreach (var provider in _settingsService.AppSettings.ProviderProfiles)
                {
                    var saved = await _profileService.SaveProviderProfileAsync(provider);
                    CopyProviderProfile(saved, provider);
                }

                foreach (var model in _settingsService.AppSettings.ModelDefinitions)
                {
                    await _profileService.SaveModelDefinitionAsync(model);
                }

                SettingsArchitectureMigration.Normalize(_settingsService.AppSettings);
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
                var saved = await _profileService.SaveProviderProfileAsync(provider);
                CopyProviderProfile(saved, provider);
                SettingsArchitectureMigration.Normalize(_settingsService.AppSettings);
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

        private static void CopyProviderProfile(ProviderProfile source, ProviderProfile target)
        {
            target.ProviderProfileId = source.ProviderProfileId;
            target.Name = source.Name;
            target.ProviderType = source.ProviderType;
            target.CustomProviderType = source.CustomProviderType;
            target.TransportType = source.TransportType;
            target.CustomTransportType = source.CustomTransportType;
            target.Endpoint = source.Endpoint;
            target.ApiKey = source.ApiKey;
            target.Secret = source.Secret;
            target.CustomHeadersAsText = source.CustomHeadersAsText;
            target.TimeoutSeconds = source.TimeoutSeconds;
            target.RetryCount = source.RetryCount;
            target.SupportsStreaming = source.SupportsStreaming;
            target.Metadata = new Dictionary<string, string>(source.Metadata);
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

        public async Task<ModelDiscoveryResult> LoadModelsForProviderAsync(
            ProviderProfile provider,
            CancellationToken token = default)
        {
            try
            {
                var savedProvider = await _profileService.SaveProviderProfileAsync(provider, token);
                CopyProviderProfile(savedProvider, provider);
                SettingsArchitectureMigration.Normalize(_settingsService.AppSettings);

                var runtimeProfile = LLMProfiles.FirstOrDefault(profile =>
                    profile.ProfileId.Equals(provider.ProviderProfileId, StringComparison.OrdinalIgnoreCase));
                if (runtimeProfile == null)
                {
                    return ModelDiscoveryResult.Fail("Provider runtime profile was not created.");
                }

                if (!UPSClient.SupportsModelDiscovery(runtimeProfile))
                {
                    return ModelDiscoveryResult.Fail(
                        $"Model discovery is not available for provider type '{runtimeProfile.ResolvedProvider}'.");
                }

                var discovered = await UPSClient.DiscoverModelsAsync(runtimeProfile, token);
                if (discovered.Count == 0)
                {
                    return ModelDiscoveryResult.Ok(0, 0, "No models returned by provider.");
                }

                var existing = _settingsService.AppSettings.ModelDefinitions
                    .Where(model => model.ProviderProfileId.Equals(provider.ProviderProfileId, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(model => model.ModelId, StringComparer.OrdinalIgnoreCase);

                var added = 0;
                foreach (var model in discovered.Where(model => !string.IsNullOrWhiteSpace(model.Id)))
                {
                    if (existing.ContainsKey(model.Id))
                    {
                        continue;
                    }

                    var definition = new ModelDefinition
                    {
                        ProviderProfileId = provider.ProviderProfileId,
                        ModelId = model.Id.Trim(),
                        DisplayName = string.IsNullOrWhiteSpace(model.Label) ? model.Id.Trim() : model.Label.Trim(),
                        ModelType = string.IsNullOrWhiteSpace(model.Type) ? provider.ProviderType : model.Type,
                        IsDiscovered = true,
                        ContextWindow = 8192,
                        MaxOutputTokens = 1024,
                        MaxInputCharacters = 0,
                        DefaultTemperature = runtimeProfile.Temperature,
                        Metadata = new Dictionary<string, string>(model.Metadata)
                    };

                    _settingsService.AppSettings.ModelDefinitions.Add(definition);
                    existing[definition.ModelId] = definition;
                    await _profileService.SaveModelDefinitionAsync(definition, token);
                    added++;
                }

                SettingsArchitectureMigration.Normalize(_settingsService.AppSettings);

                var message = added == 0
                    ? $"Provider returned {discovered.Count} model(s); all are already loaded."
                    : $"Loaded {added} new model(s) from {provider.Name}.";
                AppendMemory(message);
                Notify();
                return ModelDiscoveryResult.Ok(added, discovered.Count, message);
            }
            catch (Exception ex)
            {
                AppendMemory($"Model discovery failed: {ex.Message}");
                _errorState.AddError(
                    $"Model discovery failed: {ex.Message}",
                    source: "ModelDiscovery",
                    feature: "Settings",
                    severity: AppErrorSeverity.Error);
                return ModelDiscoveryResult.Fail(ex.Message);
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
            var vaults = await _vaultSync.LoadVaultsFromDatabaseAsync();
            _vaultWorkspace.SetVaults(vaults);



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
        /// Receives a pre-built VaultState after durable persistence succeeds and updates UI runtime state.
        /// </summary>
        public Task CreateVaultAsync(VaultState vault)
        {
            Vaults.Add(vault);
            SelectVault(vault.VaultId);
            Notify();
            return Task.CompletedTask;
        }

        public Task<bool> RenameVaultAsync(string vaultId, string name)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null || string.IsNullOrWhiteSpace(name)) return Task.FromResult(false);
            vault.Name = name.Trim();
            Notify();
            return Task.FromResult(true);
        }

        public Task<bool> RemoveVaultAsync(string vaultId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return Task.FromResult(false);
            Vaults.Remove(vault);
            if (ActiveVaultId == vaultId)
            {
                var next = Vaults.FirstOrDefault();
                ActiveVaultId = next?.VaultId;
                ActiveProjectId = next?.Projects.FirstOrDefault()?.ProjectId;
                ActiveWorkspaceSessionId = GetMostRecentSession(next)?.SessionId;
            }
            Notify();
            return Task.FromResult(true);
        }

        // ── Project CRUD ──────────────────────────────────────────────────────
        /// <summary>
        /// Receives a pre-built ProjectState after durable persistence succeeds and updates UI runtime state.
        /// </summary>
        public Task CreateProjectAsync(string vaultId, ProjectState project)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return Task.CompletedTask;
            vault.Projects.Add(project);
            SelectProject(vaultId, project.ProjectId);
            Notify();
            return Task.CompletedTask;
        }

        public Task<bool> RenameProjectAsync(string vaultId, string projectId, string name)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null || string.IsNullOrWhiteSpace(name)) return Task.FromResult(false);
            project.Name = name.Trim();
            Notify();
            return Task.FromResult(true);
        }

        public Task<bool> RemoveProjectAsync(string vaultId, string projectId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null) return Task.FromResult(false);
            vault!.Projects.Remove(project);
            if (ActiveProjectId == projectId)
            {
                ActiveProjectId = null;
                ActiveWorkspaceSessionId = null;
            }
            InitializeWorkspaceState();
            Notify();
            return Task.FromResult(true);
        }

        public Task<bool> AddProjectFileRefAsync(string path, string? name = null)
        {
            var project = ActiveProject;
            if (project == null || string.IsNullOrWhiteSpace(path)) return Task.FromResult(false);
            project.FileRefs.Add(new FileRef
            {
                Path = path,
                Name = string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileName(path) : name.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            Notify();
            return Task.FromResult(true);
        }

        public Task<FileRef?> AddPlaceholderFileToActiveProjectAsync()
        {
            var project = ActiveProject;
            if (project == null) return Task.FromResult<FileRef?>(null);
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var fileRef = new FileRef
            {
                Name = $"workspace-note-{stamp}.md",
                Path = $@"vault://{ActiveVaultId}/{project.ProjectId}/workspace-note-{stamp}.md",
                CreatedAt = DateTime.UtcNow
            };
            project.FileRefs.Add(fileRef);
            Notify();
            return Task.FromResult<FileRef?>(fileRef);
        }

        public Task<string?> ToggleKnowledgeBaseForActiveProjectAsync()
        {
            var project = ActiveProject;
            if (project == null) return Task.FromResult<string?>(null);
            project.KnowledgeBaseId = string.IsNullOrWhiteSpace(project.KnowledgeBaseId)
                ? $"kb-{project.ProjectId[..Math.Min(8, project.ProjectId.Length)]}"
                : null;
            Notify();
            return Task.FromResult(project.KnowledgeBaseId);
        }

        // ── Session CRUD ──────────────────────────────────────────────────────
        /// <summary>
        /// Receives a pre-built SessionState after durable persistence succeeds and updates UI runtime state.
        /// </summary>
        public async Task CreateWorkspaceSessionAsync(string vaultId, string? projectId, SessionState session)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return;
            var project = string.IsNullOrWhiteSpace(projectId) ? null
                : vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);

            if (project == null) vault.Sessions.Add(session);
            else project.Sessions.Add(session);

            await SelectSessionAsync(vaultId, projectId, session.SessionId);
            Notify();
        }

        public Task<bool> RenameSessionAsync(
            string vaultId, string? projectId, string sessionId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return Task.FromResult(false);
            var session = FindSession(vaultId, projectId, sessionId);
            if (session == null) return Task.FromResult(false);
            session.Name = name.Trim();
            Notify();
            return Task.FromResult(true);
        }

        public Task<bool> RemoveSessionAsync(
            string vaultId, string? projectId, string sessionId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null) return Task.FromResult(false);

            SessionState? session;
            if (string.IsNullOrWhiteSpace(projectId))
            {
                session = vault.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session == null) return Task.FromResult(false);
                vault.Sessions.Remove(session);
            }
            else
            {
                var project = vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);
                session = project?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session == null || project == null) return Task.FromResult(false);
                project.Sessions.Remove(session);
            }

            if (ActiveWorkspaceSessionId == sessionId)
                ActiveWorkspaceSessionId = null;

            InitializeWorkspaceState();
            Notify();
            return Task.FromResult(true);
        }

        public async Task<bool> SaveActiveWorkspaceSessionAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null || ActiveVaultId == null) return false;
            session.LastActiveAt = DateTime.UtcNow;
            EnsureSessionModelBindings(session);

            // Primary: persist model state to DB.
            try
            {
                await _vaultSync.UpdateSessionModelStateAsync(
                    ActiveVaultId,
                    session.SessionId,
                    session.AttachedModelIds.ToList(),
                    session.BroadcastGroupIds.ToList(),
                    session.DefaultModelId);
            }
            catch (Exception ex)
            {
                AppendMemory($"SaveActiveWorkspaceSessionAsync: DB model state update failed. {ex.Message}");
            }

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
                try
                {
                    await _vaultSync.UpdateSessionModelStateAsync(
                        vaultId, sessionId,
                        session.AttachedModelIds.ToList(),
                        session.BroadcastGroupIds.ToList(),
                        session.DefaultModelId);
                }
                catch { /* DB unavailable; keep runtime state unchanged. */ }

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
                await SaveSettingsAsync(token);
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
                var result = await UPSClient.TestLLMProfileAsync(profile, token);
                var rateLimited = IsProviderRateLimited(result.Message);
                return new TestResult
                {
                    Success = result.IsSuccess || rateLimited,
                    Message = result.IsSuccess
                        ? $"Provider test succeeded: {profile.Name}"
                        : rateLimited
                            ? $"Provider route reached, but the provider rate limited the test request: {result.Message}"
                        : $"Provider test failed: {result.Message}"
                };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }

        private static bool IsProviderRateLimited(string message)
        {
            return message.Contains("429", StringComparison.OrdinalIgnoreCase)
                || message.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase)
                || message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
                || message.Contains("rate_limit", StringComparison.OrdinalIgnoreCase);
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
            _ = SaveSettingsAsync();
        }

        public void MarkSettingsDirty()
        {
            SettingsDirty = true;
            Notify();
        }

        public void ClearSettingsDirty()
        {
            if (!SettingsDirty)
            {
                return;
            }

            SettingsDirty = false;
            Notify();
        }

        public async Task<bool> SaveSettingsIfDirtyAsync(CancellationToken token = default)
        {
            if (!SettingsDirty)
            {
                return false;
            }

            await SaveSettingsAsync(token);
            return true;
        }

        public async Task SaveSettingsAsync(CancellationToken token = default)
        {
            await SaveProfilesToVaultAsync();
            await _settingsService.SaveAsync();

            SettingsDirty = false;
            Notify();
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
            if (Vaults.Count == 0)
            {
                ActiveVaultId = null;
                ActiveProjectId = null;
                ActiveWorkspaceSessionId = null;

                _chatState.ActiveSessionId = null;
                _chatState.ActiveSessionName = string.Empty;
                _chatState.ActiveVaultId = null;
                _chatState.ActiveProjectId = null;

                Sessions.CloseAll();
                Notify();
                return;
            }

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

    public class ModelDiscoveryResult
    {
        public bool Success { get; set; }
        public int AddedCount { get; set; }
        public int DiscoveredCount { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ModelDiscoveryResult Ok(int addedCount, int discoveredCount, string message)
        {
            return new ModelDiscoveryResult
            {
                Success = true,
                AddedCount = addedCount,
                DiscoveredCount = discoveredCount,
                Message = message
            };
        }

        public static ModelDiscoveryResult Fail(string message)
        {
            return new ModelDiscoveryResult
            {
                Success = false,
                Message = message
            };
        }
    }

    public class ConsoleLogService : ILogService
    {
        public void LogInfo(string message) => Console.WriteLine("[INFO] " + message);
        public void LogWarning(string message) => Console.WriteLine("[WARN] " + message);
        public void LogError(string message) => Console.WriteLine("[ERROR] " + message);
    }
}

