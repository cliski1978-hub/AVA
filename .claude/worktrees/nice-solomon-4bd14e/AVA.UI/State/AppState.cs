using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Nomi.Bridge;
using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Models;
using AVA.UI.CORE.Models.Settings;
using AVA.UI.CORE.Models.UI;
using AVA.UI.CORE.Services;
using AVA.UI.CORE.Services.Network;
using AVA.UI.CORE.UPS.Client;
using AVA.UI.CORE.UPS.Sessions;
using AVA.UPS.Adapter.Models;

namespace AVA.UI.State
{
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

        private readonly AvaSettingsService _settingsService;

        public readonly UPSClientService UPSClient;
        public readonly SessionManager Sessions;

        public bool IsConnected { get; private set; }
        public string ConnectionType { get; private set; } = string.Empty;
        public string ConnectionStatus { get; private set; } = "Not connected";
        public string ConnectionDetails { get; private set; } = string.Empty;

        public List<LLMProfile> LLMProfiles => _settingsService.AppSettings.LLMProfiles;
        public LLMProfile? SelectedLLMProfile { get; set; }

        public ObservableCollection<OutputSegment> OutputSegments { get; } = new();
        public ObservableCollection<string> MemoryEvents { get; } = new();
        public ObservableCollection<string> Reflections { get; } = new();
        private readonly Dictionary<string, List<Message>> _modelConversations =
            new(StringComparer.OrdinalIgnoreCase);

        public string LatestInsight { get; private set; } = string.Empty;
        public bool HasContradiction { get; private set; }

        public string PromptPreviewText { get; set; } = string.Empty;
        public int PromptTokenEstimate { get; set; }
        public bool ShowPromptPreview { get; set; }

        public AppSettings AppSettings => _settingsService.AppSettings;
        public AppSettings Settings => _settingsService.AppSettings;
        public SessionState? CurrentSession => ActiveWorkspaceSession;
        public AppUser CurrentUser { get; } = BuildCurrentUser();

        public bool UseDirectEndpoints
        {
            get => _settingsService.AppSettings.UseDirectEndpoints;
            set => _settingsService.AppSettings.UseDirectEndpoints = value;
        }

        public List<VaultState> Vaults => _settingsService.Vaults;
        public string? ActiveVaultId { get; private set; }
        public string? ActiveProjectId { get; private set; }
        public string? ActiveWorkspaceSessionId { get; private set; }

        public VaultState? ActiveVault => Vaults.FirstOrDefault(v => v.VaultId == ActiveVaultId);

        public ProjectState? ActiveProject =>
            ActiveVault?.Projects.FirstOrDefault(p => p.ProjectId == ActiveProjectId);

        public SessionState? ActiveWorkspaceSession =>
            ActiveProjectId == null
                ? ActiveVault?.Sessions.FirstOrDefault(s => s.SessionId == ActiveWorkspaceSessionId)
                : ActiveProject?.Sessions.FirstOrDefault(s => s.SessionId == ActiveWorkspaceSessionId);

        public string SelectedNavigationItem { get; private set; } = "Chat";

        public event Action? OnChange;

        private void Notify() => OnChange?.Invoke();

        /// <summary>
        /// Fired when the global prompt bar sends a prompt.
        /// Cards subscribe to this and handle their own send/display.
        /// </summary>
        public event Func<string, Task>? OnGlobalBroadcast;

        public async Task RaiseGlobalBroadcastAsync(string prompt)
        {
            if (OnGlobalBroadcast == null) return;
            var handlers = OnGlobalBroadcast.GetInvocationList()
                .Cast<Func<string, Task>>();
            await Task.WhenAll(handlers.Select(h => h(prompt)));
        }

        public IReadOnlyList<Message> GetConversation(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return Array.Empty<Message>();
            }

            return GetOrCreateConversation(modelId);
        }

        public List<Message> GetBroadcastConversation(IEnumerable<string> modelIds)
        {
            var orderedMessages = modelIds
                .Where(modelId => !string.IsNullOrWhiteSpace(modelId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(modelId => GetConversation(modelId))
                .OrderBy(message => message.Timestamp)
                .ToList();

            var seenUserTurns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mergedMessages = new List<Message>(orderedMessages.Count);

            foreach (var message in orderedMessages)
            {
                if (!string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
                {
                    mergedMessages.Add(message);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(message.TurnId))
                {
                    mergedMessages.Add(message);
                    continue;
                }

                if (seenUserTurns.Add(message.TurnId))
                {
                    mergedMessages.Add(message);
                }
            }

            return mergedMessages;
        }

        public IReadOnlyList<Message> GetMainChatMessages()
        {
            var targetSessions = GetMainChatSessions().ToList();
            if (!targetSessions.Any())
            {
                return Array.Empty<Message>();
            }

            var targetModelIds = targetSessions
                .Select(session => session.ModelProfile?.Id ?? session.ModelEntry.Id)
                .Where(modelId => !string.IsNullOrWhiteSpace(modelId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (targetModelIds.Count == 1)
            {
                return GetConversation(targetModelIds[0]);
            }

            return GetBroadcastConversation(targetModelIds);
        }

        public string GetMainChatTitle()
        {
            var targetSessions = GetMainChatSessions().ToList();
            if (!targetSessions.Any())
            {
                return "Conversation";
            }

            if (targetSessions.Count == 1)
            {
                return targetSessions[0].DisplayName;
            }

            return $"Broadcast ({targetSessions.Count})";
        }

        public bool IsMainChatWaiting =>
            GetMainChatSessions().Any(session => session.IsSending);

        public void AppendUserConversationMessage(
            string modelId,
            string content,
            string? modelLabel = null,
            string? turnId = null)
        {
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return;
            }

            GetOrCreateConversation(modelId).Add(new Message
            {
                Role = "user",
                Content = content,
                ModelId = modelId,
                ModelLabel = modelLabel,
                TurnId = turnId
            });

            Notify();
        }

        public void AppendAssistantConversationMessage(
            string modelId,
            string content,
            string? modelLabel = null,
            bool isError = false,
            string? turnId = null)
        {
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return;
            }

            GetOrCreateConversation(modelId).Add(new Message
            {
                Role = "assistant",
                Content = content,
                ModelId = modelId,
                ModelLabel = modelLabel,
                IsError = isError,
                TurnId = turnId
            });

            Notify();
        }

        public void ClearConversation(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return;
            }

            _modelConversations.Remove(modelId);
            Notify();
        }

        public void ClearMainChat()
        {
            var targetModelIds = GetMainChatSessions()
                .Select(session => session.ModelProfile?.Id ?? session.ModelEntry.Id)
                .Where(modelId => !string.IsNullOrWhiteSpace(modelId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var modelId in targetModelIds)
            {
                _modelConversations.Remove(modelId);
            }

            OutputSegments.Clear();
            Notify();
        }

        public void NotifyStateChanged() => Notify();

        private List<Message> GetOrCreateConversation(string modelId)
        {
            if (!_modelConversations.TryGetValue(modelId, out var conversation))
            {
                conversation = new List<Message>();
                _modelConversations[modelId] = conversation;
            }

            return conversation;
        }

        public AppState(
            AvaSettingsService settingsService,
            IEndpointClientService endpointClient)
        {
            _settingsService = settingsService;
            _settingsService.LoadSettings();

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

        public void SetSelectedNavigationItem(string item)
        {
            SelectedNavigationItem = string.IsNullOrWhiteSpace(item) ? "Chat" : item;
            Notify();
        }

        public async Task LoadWorkspaceStateAsync()
        {
            var loadedVaults = await _settingsService.LoadVaultsAsync();
            _settingsService.Vaults = loadedVaults;
            InitializeWorkspaceState();
        }

        public async Task<VaultState> CreateVaultAsync(
            string name,
            string? icon = null,
            string? accentColor = null,
            string? storagePath = null)
        {
            var vault = new VaultState
            {
                Name = string.IsNullOrWhiteSpace(name) ? "New Vault" : name.Trim(),
                Icon = icon,
                AccentColor = accentColor,
                StoragePath = storagePath,
                IsExpanded = true
            };

            Vaults.Add(vault);
            SelectVault(vault.VaultId);
            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return vault;
        }

        public async Task<ProjectState?> CreateProjectAsync(
            string vaultId,
            string name,
            string? knowledgeBaseId = null)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return null;
            }

            var project = new ProjectState
            {
                Name = string.IsNullOrWhiteSpace(name) ? "New Project" : name.Trim(),
                KnowledgeBaseId = knowledgeBaseId,
                IsExpanded = true
            };

            vault.Projects.Add(project);
            SelectProject(vaultId, project.ProjectId);
            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return project;
        }

        public async Task<bool> RenameVaultAsync(string vaultId, string name)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            vault.Name = name.Trim();
            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        public async Task<bool> RemoveVaultAsync(string vaultId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return false;
            }

            Vaults.Remove(vault);

            if (ActiveVaultId == vaultId)
            {
                var next = Vaults.FirstOrDefault();
                ActiveVaultId = next?.VaultId;
                ActiveProjectId = next?.Projects.FirstOrDefault()?.ProjectId;
                ActiveWorkspaceSessionId = GetMostRecentSession(next)?.SessionId;
            }

            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        public async Task<bool> RenameProjectAsync(string vaultId, string projectId, string name)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            project.Name = name.Trim();
            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        public async Task<bool> RemoveProjectAsync(string vaultId, string projectId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null)
            {
                return false;
            }

            vault!.Projects.Remove(project);

            if (ActiveProjectId == projectId)
            {
                ActiveProjectId = null;
                ActiveWorkspaceSessionId = null;
            }

            await _settingsService.SaveVaultsAsync(Vaults);
            InitializeWorkspaceState();
            Notify();
            return true;
        }

        public async Task<SessionState?> CreateWorkspaceSessionAsync(
            string vaultId,
            string? projectId,
            string name,
            string? defaultModelId = null)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return null;
            }

            var project = string.IsNullOrWhiteSpace(projectId)
                ? null
                : vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);

            if (!string.IsNullOrWhiteSpace(projectId) && project == null)
            {
                return null;
            }

            var now = DateTime.UtcNow;
            var session = new SessionState
            {
                Name = string.IsNullOrWhiteSpace(name) ? "New Session" : name.Trim(),
                CreatedAt = now,
                LastActiveAt = now,
                DefaultModelId = defaultModelId ?? project?.DefaultModelIds.FirstOrDefault()
            };

            if (!string.IsNullOrWhiteSpace(session.DefaultModelId))
            {
                session.AttachedModelIds.Add(session.DefaultModelId);
            }

            if (project == null)
            {
                vault.Sessions.Add(session);
            }
            else
            {
                project.Sessions.Add(session);
            }

            SelectSession(vaultId, projectId, session.SessionId);
            await _settingsService.SaveSessionAsync(vaultId, projectId, session);
            Notify();
            return session;
        }

        public async Task<bool> RenameSessionAsync(string vaultId, string? projectId, string sessionId, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var session = FindSession(vaultId, projectId, sessionId);
            if (session == null)
            {
                return false;
            }

            session.Name = name.Trim();
            await _settingsService.SaveSessionAsync(vaultId, projectId, session);
            Notify();
            return true;
        }

        public async Task<bool> RemoveSessionAsync(string vaultId, string? projectId, string sessionId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return false;
            }

            SessionState? session;

            if (string.IsNullOrWhiteSpace(projectId))
            {
                session = vault.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session == null)
                {
                    return false;
                }

                vault.Sessions.Remove(session);
            }
            else
            {
                var project = vault.Projects.FirstOrDefault(p => p.ProjectId == projectId);
                session = project?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session == null || project == null)
                {
                    return false;
                }

                project.Sessions.Remove(session);
            }

            if (ActiveWorkspaceSessionId == sessionId)
            {
                ActiveWorkspaceSessionId = null;
            }

            await _settingsService.SaveVaultsAsync(Vaults);
            InitializeWorkspaceState();
            Notify();
            return true;
        }

        public void SelectVault(string vaultId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return;
            }

            ActiveVaultId = vault.VaultId;
            ActiveProjectId = vault.Projects.FirstOrDefault()?.ProjectId;
            ActiveWorkspaceSessionId = GetMostRecentSession(vault)?.SessionId;
            if (vault.Sessions.Any() && vault.Sessions.Any(s => s.SessionId == ActiveWorkspaceSessionId))
            {
                ActiveProjectId = null;
            }
            Notify();
        }

        public void SelectProject(string vaultId, string projectId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            var project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
            if (project == null)
            {
                return;
            }

            ActiveVaultId = vaultId;
            ActiveProjectId = project.ProjectId;
            ActiveWorkspaceSessionId = project.Sessions
                .OrderByDescending(s => s.LastActiveAt)
                .FirstOrDefault()?
                .SessionId;
            Notify();
        }

        public void SelectSession(string vaultId, string? projectId, string sessionId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            ProjectState? project = null;
            SessionState? session;

            if (string.IsNullOrWhiteSpace(projectId))
            {
                session = vault?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            }
            else
            {
                project = vault?.Projects.FirstOrDefault(p => p.ProjectId == projectId);
                session = project?.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            }

            if (session == null)
            {
                return;
            }

            ActiveVaultId = vaultId;
            ActiveProjectId = project?.ProjectId;
            ActiveWorkspaceSessionId = sessionId;
            session.LastActiveAt = DateTime.UtcNow;
            Notify();
        }

        public async Task<bool> SaveActiveWorkspaceSessionAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null || ActiveVaultId == null)
            {
                return false;
            }

            session.LastActiveAt = DateTime.UtcNow;
            await _settingsService.SaveSessionAsync(ActiveVaultId, ActiveProjectId, session);
            Notify();
            return true;
        }

        public async Task<bool> RenameActiveWorkspaceSessionAsync(string name)
        {
            var session = ActiveWorkspaceSession;
            if (session == null || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            session.Name = name.Trim();
            return await SaveActiveWorkspaceSessionAsync();
        }

        public async Task<bool> AddProjectFileRefAsync(string path, string? name = null)
        {
            var project = ActiveProject;
            if (project == null || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            project.FileRefs.Add(new FileRef
            {
                Path = path,
                Name = string.IsNullOrWhiteSpace(name) ? System.IO.Path.GetFileName(path) : name.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return true;
        }

        /// <summary>
        /// Attaches the next available model to the active workspace session.
        /// </summary>
        public async Task<string?> AttachNextModelToActiveSessionAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null)
            {
                return null;
            }

            var nextModel = GetAttachableModels()
                .FirstOrDefault(modelId => !session.AttachedModelIds.Contains(modelId, StringComparer.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(nextModel))
            {
                return null;
            }

            session.AttachedModelIds.Add(nextModel);
            session.DefaultModelId ??= nextModel;
            session.LastActiveAt = DateTime.UtcNow;
            await SaveActiveWorkspaceSessionAsync();
            return nextModel;
        }

        /// <summary>
        /// Cycles the active session layout anchor through the supported shell positions.
        /// </summary>
        public async Task<string?> CycleActiveSessionLayoutAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null)
            {
                return null;
            }

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

        /// <summary>
        /// Adds a placeholder file reference to the active project.
        /// </summary>
        public async Task<FileRef?> AddPlaceholderFileToActiveProjectAsync()
        {
            var project = ActiveProject;
            if (project == null)
            {
                return null;
            }

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var fileRef = new FileRef
            {
                Name = $"workspace-note-{stamp}.md",
                Path = $@"vault://{ActiveVaultId}/{project.ProjectId}/workspace-note-{stamp}.md",
                CreatedAt = DateTime.UtcNow
            };

            project.FileRefs.Add(fileRef);
            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return fileRef;
        }

        /// <summary>
        /// Toggles a placeholder knowledge-base identifier for the active project.
        /// </summary>
        public async Task<string?> ToggleKnowledgeBaseForActiveProjectAsync()
        {
            var project = ActiveProject;
            if (project == null)
            {
                return null;
            }

            project.KnowledgeBaseId = string.IsNullOrWhiteSpace(project.KnowledgeBaseId)
                ? $"kb-{project.ProjectId[..Math.Min(8, project.ProjectId.Length)]}"
                : null;

            await _settingsService.SaveVaultsAsync(Vaults);
            Notify();
            return project.KnowledgeBaseId;
        }

        /// <summary>
        /// Adds a new card to the active session canvas.
        /// </summary>
        public async Task<CardState?> AddCardToActiveSessionAsync()
        {
            var session = ActiveWorkspaceSession;
            if (session == null)
            {
                return null;
            }

            var nextZ = session.Canvas.Cards.Count == 0
                ? 1
                : session.Canvas.Cards.Max(card => card.ZIndex) + 1;

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

        /// <summary>
        /// Routes the shell to settings as the current profile surface.
        /// </summary>
        public void OpenProfile()
        {
            SetSelectedNavigationItem("Settings");
            AppendOutputSystem("Profile panel opened in Settings.");
        }

        /// <summary>
        /// Closes active runtime sessions and returns the shell to settings.
        /// </summary>
        public void SignOut()
        {
            Sessions.CloseAll();
            IsConnected = false;
            UpdateStatus("Offline", "Signed out", "Active model sessions closed.");
            SetSelectedNavigationItem("Settings");
            AppendOutputSystem("Signed out of the current runtime session.");
        }

        public async Task<TestResult> VerifyPersistenceAsync()
        {
            try
            {
                var verifier = new StatePersistenceVerifier(_settingsService);
                var result = await verifier.VerifyAsync();
                return new TestResult
                {
                    Success = result.Success,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    Success = false,
                    Message = $"Persistence verification failed: {ex.Message}"
                };
            }
        }

        public async Task ConnectAsync(CancellationToken token = default)
        {
            RestoreSelectedLLMProfile();
            AppendMemory($"ConnectAsync: {LLMProfiles.Count} profile(s) in settings.");

            var profilesToConnect = LLMProfiles.Where(p => p.IsActive).ToList();
            AppendMemory($"Active profiles: {profilesToConnect.Count}. SelectedLLMProfile: {SelectedLLMProfile?.Name ?? "null"}.");

            if (!profilesToConnect.Any() && SelectedLLMProfile != null)
            {
                profilesToConnect.Add(SelectedLLMProfile);
            }

            if (!profilesToConnect.Any())
            {
                AppendMemory("ConnectAsync: no profiles to connect.");
                UpdateStatus("Error", "No profile", "No active LLM profiles found.");
                return;
            }

            try
            {
                UpdateStatus("Connecting", "Connecting", $"Connecting {profilesToConnect.Count} profile(s)...");

                foreach (var profile in profilesToConnect)
                {
                    AppendMemory($"Registering profile: {profile.Name} ({profile.ResolvedProvider})...");
                    await Sessions.RegisterProfileAsync(profile, token);

                    var modelCount = profile.Models.Count;
                    AppendMemory($"{profile.Name} connected - {modelCount} model(s) available.");
                }

                IsConnected = true;
                var totalModels = profilesToConnect.Sum(p => p.Models.Count);
                RestoreStartupSessions(profilesToConnect);
                UpdateStatus("UPS", "Connected", $"{profilesToConnect.Count} profile(s), {totalModels} model(s)");
                Notify();
            }
            catch (Exception ex)
            {
                IsConnected = false;
                AppendMemory($"ConnectAsync failed: {ex.GetType().Name}: {ex.Message}");
                UpdateStatus("UPS", "Failed", ex.Message);
                AppendOutput(new OutputSegment { Type = "error", Value = ex.Message });
            }
        }

        public async Task ConnectProfileAsync(LLMProfile profile, CancellationToken token = default)
        {
            try
            {
                profile.IsActive = true;
                SelectedLLMProfile = profile;
                _settingsService.AppSettings.SelectedLLMProfileName = profile.Name;
                SaveSettings();
                AppendMemory($"ConnectProfileAsync: {profile.Name} ({profile.ResolvedProvider})...");
                UpdateStatus("Connecting", "Connecting", $"Connecting to {profile.Name}...");

                await Sessions.RegisterProfileAsync(profile, token);

                IsConnected = true;
                var modelCount = profile.Models.Count;
                RestoreStartupSessions(new[] { profile });
                UpdateStatus("UPS", "Connected", $"{profile.Name} - {modelCount} model(s) available.");
                AppendMemory($"{profile.Name} connected - {modelCount} model(s) available.");
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
                var isNomi = profile.ResolvedProvider.Equals("Nomi", StringComparison.OrdinalIgnoreCase);

                if (isNomi)
                {
                    var nomiClient = new NomiApiClient(profile.ApiKey);
                    var nomis = await nomiClient.GetNomisAsync(token);
                    return new TestResult
                    {
                        Success = true,
                        Message = $"Nomi connected - {nomis.Count} character(s) found."
                    };
                }

                var client = new EndpointClientService();
                var ok = await client.ConnectAsync(null, profile, token);

                return new TestResult
                {
                    Success = ok,
                    Message = ok
                        ? $"Endpoint reachable: {profile.Endpoint}"
                        : $"Could not reach endpoint: {profile.Endpoint}"
                };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }

        public async Task<TestResult> TestModelAsync(
            LLMProfile profile,
            ModelProfile model,
            CancellationToken token = default)
        {
            try
            {
                await Sessions.RegisterProfileAsync(profile, token);

                var session = Sessions.CreateSession(profile, model);
                await session.SendAsync("This is a model connection test.", token);

                var error = session.LastError;
                var reply = session.Output
                    .LastOrDefault(s => s.Type != "error")
                    ?.Value ?? string.Empty;

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

            AppendMemory($"Prompt: {prompt}");
            var turnId = Guid.NewGuid().ToString("N");

            foreach (var session in targets)
            {
                var modelId = session.ModelProfile?.Id ?? session.ModelEntry.Id;
                AppendUserConversationMessage(modelId, prompt, session.DisplayName, turnId);
            }

            await Task.WhenAll(targets.Select(s => s.SendAsync(prompt, token)));

            var lastReply = string.Empty;

            foreach (var session in targets)
            {
                var modelId = session.ModelProfile?.Id ?? session.ModelEntry.Id;
                var error = session.LastError;
                if (!string.IsNullOrWhiteSpace(error))
                {
                    AppendAssistantConversationMessage(
                        modelId,
                        error,
                        session.DisplayName,
                        isError: true,
                        turnId: turnId);
                    continue;
                }

                var reply = session.Output.LastOrDefault(s => s.Type != "error")?.Value ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(reply))
                {
                    AppendAssistantConversationMessage(
                        modelId,
                        reply,
                        session.DisplayName,
                        turnId: turnId);
                    AppendMemory($"{session.DisplayName}: {reply}");
                    lastReply = reply;
                }
            }

            Notify();
            return lastReply;
        }

        public async Task BroadcastPromptAsync(string prompt, CancellationToken token = default)
        {
            if (!IsConnected)
            {
                AppendOutput(new OutputSegment
                {
                    Type = "system",
                    Value = "Not connected. Please connect in Settings first."
                });
                return;
            }

            AppendOutput(new OutputSegment
            {
                Type = "text",
                Value = $"You (broadcast): {prompt}"
            });

            AppendMemory($"Broadcast: {prompt}");
            await Sessions.BroadcastAsync(prompt, token);
            Notify();
        }

        public void AppendOutput(OutputSegment segment)
        {
            OutputSegments.Add(segment);
            Notify();
        }

        public void AppendOutputText(string text) =>
            AppendOutput(new OutputSegment { Type = "text", Value = text });

        public void AppendOutputSystem(string text) =>
            AppendOutput(new OutputSegment { Type = "system", Value = text });

        public void ClearOutput()
        {
            OutputSegments.Clear();
            Notify();
        }

        public void AppendMemory(string message)
        {
            MemoryEvents.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            Notify();
        }

        public void ClearMemory()
        {
            MemoryEvents.Clear();
            AppendMemory("Memory log cleared.");
        }

        public void AppendReflection(string insight, bool isContradiction = false, string score = "")
        {
            LatestInsight = insight;
            HasContradiction = isContradiction;

            var tag = isContradiction ? "CONTRADICTION" : "ALIGNMENT";
            Reflections.Add($"[{DateTime.Now:HH:mm:ss}] {tag}: {insight}");

            if (!string.IsNullOrWhiteSpace(score))
            {
                Reflections.Add($"Score: {score}");
            }

            Notify();
        }

        public void UpdateStatus(string type, string status, string details = "")
        {
            ConnectionType = type;
            ConnectionStatus = status;
            ConnectionDetails = details;
            Notify();
        }

        public void SaveSettings() => _settingsService.SaveSettings();

        // ── Canvas ────────────────────────────────────────────────────────────

        /// <summary>
        /// Saves the current canvas card positions and state to disk.
        /// Called after drag, close, or layout changes.
        /// </summary>
        public void SaveCanvasState() => _settingsService.SaveSettings();

        /// <summary>
        /// Adds a new card to the canvas with auto-positioned X/Y offset.
        /// </summary>
        public void AddCard(string cardType = "SingleModel")
        {
            var offset = CurrentSession.Canvas.Cards.Count * 32;
            var card = new CardState
            {
                CardType = cardType,
                X = 80 + offset,
                Y = 80 + offset,
                Width = cardType == "Broadcast" ? 520 : 380,
                ZIndex = CurrentSession.Canvas.Cards.Count + 1
            };

            if (cardType == "Broadcast")
            {
                card.ModelProfileIds.AddRange(
                    Sessions.AvailableModels.Select(m => m.Model.Id).Take(4));
            }
            else
            {
                var first = Sessions.AvailableModels.Select(m => m.Model.Id).FirstOrDefault();
                if (first != null) card.ModelProfileIds.Add(first);
            }

            CurrentSession.Canvas.Cards.Add(card);
            Notify();
        }

        /// <summary>
        /// Removes a card from the canvas by ID and saves state.
        /// </summary>
        public void RemoveCard(string cardId)
        {
            var card = CurrentSession.Canvas.Cards.FirstOrDefault(c => c.CardId == cardId);
            if (card != null)
            {
                CurrentSession.Canvas.Cards.Remove(card);
                SaveCanvasState();
                Notify();
            }
        }

        /// <summary>
        /// Returns the display label for a model profile ID.
        /// Falls back to the raw ID if not found.
        /// </summary>
        public string GetModelName(string profileId)
        {
            var match = Sessions.AvailableModels
                .FirstOrDefault(m => m.Model.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));
            return match.Model?.Label ?? profileId;
        }

        /// <summary>
        /// Extracts the response text from a UPS payload.
        /// Checks common content keys before falling back to the first string value.
        /// </summary>
        public static string ExtractContent(UPSResponse response)
        {
            if (!response.Success) return response.ErrorMessage ?? "Error";
            // Check the convenience Content field set directly by ILLMAdapter implementations
            if (!string.IsNullOrEmpty(response.Content)) return response.Content;
            // Fall back to walking the Payload list (Nomi and legacy paths)
            var param = response.Payload.FirstOrDefault(p =>
                p.Key == "content" || p.Key == "assistantMessage" ||
                p.Key == "reply"   || p.Key == "text" || p.Type == "string");
            return param?.Value?.ToString() ?? string.Empty;
        }

        private static AppUser BuildCurrentUser()
        {
            var userName = Environment.UserName;
            var safeName = string.IsNullOrWhiteSpace(userName) ? "AVA User" : userName.Trim();

            return new AppUser
            {
                Name = safeName,
                Email = $"{safeName.Replace(' ', '.').ToLowerInvariant()}@local"
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
                var recentVaultSession = activeVault.Sessions
                    .OrderByDescending(s => s.LastActiveAt)
                    .FirstOrDefault();

                var recentProjectSession = activeProject?.Sessions
                    .OrderByDescending(s => s.LastActiveAt)
                    .FirstOrDefault();

                var mostRecent = recentVaultSession == null
                    ? recentProjectSession
                    : recentProjectSession == null
                        ? recentVaultSession
                        : recentVaultSession.LastActiveAt >= recentProjectSession.LastActiveAt
                            ? recentVaultSession
                            : recentProjectSession;

                ActiveWorkspaceSessionId ??= mostRecent?.SessionId;

                if (mostRecent != null && activeVault.Sessions.Any(s => s.SessionId == mostRecent.SessionId))
                {
                    ActiveProjectId = null;
                }
            }

            Notify();
        }

        private void EnsureWorkspaceDefaults()
        {
            if (Vaults.Count > 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var session = new SessionState
            {
                Name = "Session 1",
                CreatedAt = now,
                LastActiveAt = now
            };

            var project = new ProjectState
            {
                Name = "Project 1",
                IsExpanded = true,
                Sessions = new List<SessionState> { session }
            };

            var vault = new VaultState
            {
                Name = "Vault 1",
                IsExpanded = true,
                Projects = new List<ProjectState> { project }
            };

            Vaults.Add(vault);
            _settingsService.SaveVaultsAsync(Vaults).GetAwaiter().GetResult();
        }

        private static SessionState? GetMostRecentSession(VaultState? vault)
        {
            if (vault == null)
            {
                return null;
            }

            var vaultSession = vault.Sessions
                .OrderByDescending(s => s.LastActiveAt)
                .FirstOrDefault();

            var projectSession = vault.Projects
                .SelectMany(p => p.Sessions)
                .OrderByDescending(s => s.LastActiveAt)
                .FirstOrDefault();

            if (vaultSession == null)
            {
                return projectSession;
            }

            if (projectSession == null)
            {
                return vaultSession;
            }

            return vaultSession.LastActiveAt >= projectSession.LastActiveAt
                ? vaultSession
                : projectSession;
        }

        private SessionState? FindSession(string vaultId, string? projectId, string sessionId)
        {
            var vault = Vaults.FirstOrDefault(v => v.VaultId == vaultId);
            if (vault == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                return vault.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            }

            return vault.Projects
                .FirstOrDefault(p => p.ProjectId == projectId)?
                .Sessions
                .FirstOrDefault(s => s.SessionId == sessionId);
        }

        private IEnumerable<string> GetAttachableModels()
        {
            var activeModelIds = LLMProfiles
                .Where(profile => profile.IsActive)
                .SelectMany(profile => profile.Models)
                .Where(model => model.IsActive)
                .Select(model => model.Id);

            var fallbackModelIds = LLMProfiles
                .SelectMany(profile => profile.Models)
                .Select(model => model.Id);

            return activeModelIds
                .Concat(fallbackModelIds)
                .Where(modelId => !string.IsNullOrWhiteSpace(modelId))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void RestoreSelectedLLMProfile()
        {
            var preferredName = _settingsService.AppSettings.SelectedLLMProfileName;

            SelectedLLMProfile =
                LLMProfiles.FirstOrDefault(profile =>
                    !string.IsNullOrWhiteSpace(preferredName) &&
                    profile.Name.Equals(preferredName, StringComparison.OrdinalIgnoreCase))
                ?? LLMProfiles.FirstOrDefault(profile => profile.IsDefault)
                ?? LLMProfiles.FirstOrDefault(profile => profile.IsActive)
                ?? LLMProfiles.FirstOrDefault();

            if (SelectedLLMProfile != null)
            {
                _settingsService.AppSettings.SelectedLLMProfileName = SelectedLLMProfile.Name;
            }
        }

        private void RestoreStartupSessions(IEnumerable<LLMProfile> connectedProfiles)
        {
            var connectedProfileList = connectedProfiles
                .Where(profile => profile != null)
                .DistinctBy(profile => profile.ProfileId)
                .ToList();

            if (!connectedProfileList.Any())
            {
                return;
            }

            var targetModelIds = new List<string>();
            var workspaceSession = ActiveWorkspaceSession;

            if (workspaceSession?.AttachedModelIds.Any() == true)
            {
                targetModelIds.AddRange(workspaceSession.AttachedModelIds);
            }
            else if (!string.IsNullOrWhiteSpace(workspaceSession?.DefaultModelId))
            {
                targetModelIds.Add(workspaceSession.DefaultModelId);
            }

            if (!targetModelIds.Any() && SelectedLLMProfile != null)
            {
                targetModelIds.AddRange(
                    SelectedLLMProfile.Models
                        .Where(model => model.IsActive)
                        .Select(model => model.Id));

                if (!targetModelIds.Any())
                {
                    var firstSelectedModel = SelectedLLMProfile.Models.FirstOrDefault()?.Id;
                    if (!string.IsNullOrWhiteSpace(firstSelectedModel))
                    {
                        targetModelIds.Add(firstSelectedModel);
                    }
                }
            }

            if (!targetModelIds.Any())
            {
                targetModelIds.AddRange(
                    connectedProfileList
                        .SelectMany(profile => profile.Models.Where(model => model.IsActive))
                        .Select(model => model.Id));
            }

            foreach (var modelId in targetModelIds
                .Where(modelId => !string.IsNullOrWhiteSpace(modelId))
                .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Sessions.Sessions.Any(session =>
                    string.Equals(session.ModelProfile?.Id ?? session.ModelEntry.Id, modelId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var profileRecord = connectedProfileList
                    .Select(profile => new
                    {
                        Profile = profile,
                        Model = profile.Models.FirstOrDefault(model =>
                            model.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase))
                    })
                    .FirstOrDefault(record => record.Model != null);

                if (profileRecord?.Model == null)
                {
                    continue;
                }

                Sessions.CreateSession(profileRecord.Profile, profileRecord.Model);
            }
        }

        private IEnumerable<UPSSession> GetMainChatSessions()
        {
            if (Sessions.BroadcastGroup.Any())
            {
                return Sessions.BroadcastGroup.Where(session => session.IsActive);
            }

            if (Sessions.ActiveSession != null && Sessions.ActiveSession.IsActive)
            {
                return new[] { Sessions.ActiveSession };
            }

            return Sessions.Sessions.Where(session => session.IsActive);
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
