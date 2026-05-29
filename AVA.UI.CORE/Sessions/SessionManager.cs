// ─────────────────────────────────────────────────────────────────────────────
//  Class     : SessionManager
//  Namespace : AVA.UI.CORE.UPS.Sessions
//  Purpose   : Owns and manages all active UPS sessions.
//              Supports single session, multi-session, and broadcast topologies.
//              Model selection is two-layer:
//                  LLMProfile  — which providers are active
//                  ModelProfile — which models within each provider are active
//              Sessions are created from ModelProfiles within active LLMProfiles.
//              API-discovered models are merged at connect time.
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using AVA.UPS.Adapter.Interfaces;
using AVA.UPS.Adapter.Models;
using AVA.UI.CORE.Models.Settings;
using AVA.UI.CORE.UPS.Client;

namespace AVA.UI.CORE.UPS.Sessions
{
    /// <summary>
    /// Manages all active UPS sessions.
    /// The UI's single source of truth for conversations.
    /// </summary>
    public class SessionManager
    {
        // ── Services ──────────────────────────────────────────────────────────

        private readonly UPSClientService _Client;

        // ── Sessions ──────────────────────────────────────────────────────────

        private readonly ConcurrentDictionary<string, UPSSession> _Sessions =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// All active sessions ordered by creation time.
        /// The UI binds to this collection.
        /// </summary>
        public ObservableCollection<UPSSession> Sessions { get; } = new();

        /// <summary>
        /// The currently focused session — the one the input pane sends to.
        /// </summary>
        public UPSSession? ActiveSession { get; private set; }

        /// <summary>
        /// Sessions currently included in broadcast.
        /// Send to all in this set simultaneously.
        /// </summary>
        public ObservableCollection<UPSSession> BroadcastGroup { get; } = new();

        // ── LLM Profiles ──────────────────────────────────────────────────────

        /// <summary>
        /// All LLM profiles registered this session.
        /// Keyed by ProfileId.
        /// </summary>
        private readonly ConcurrentDictionary<string, LLMProfile> _Profiles =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// All currently active LLM profiles.
        /// </summary>
        public IEnumerable<LLMProfile> ActiveProfiles =>
            _Profiles.Values.Where(P => P.IsActive);

        // ── Merged Model List ─────────────────────────────────────────────────

        /// <summary>
        /// All available models across all registered profiles.
        /// Union of saved ModelProfiles and API-discovered models.
        /// Used to populate the chat model picker.
        /// </summary>
        public IEnumerable<(LLMProfile Profile, ModelProfile Model)> AvailableModels =>
            _Profiles.Values
                .SelectMany(P => P.Models.Select(M => (P, M)));

        /// <summary>
        /// All models currently marked IsActive across all active profiles.
        /// These are the models open as sessions.
        /// </summary>
        public IEnumerable<(LLMProfile Profile, ModelProfile Model)> ActiveModels =>
            _Profiles.Values
                .Where(P => P.IsActive)
                .SelectMany(P => P.ActiveModels.Select(M => (P, M)));


        // ── Change Notification ───────────────────────────────────────────────

        /// <summary>
        /// Fires when sessions, profiles, or model selections change.
        /// </summary>
        public event Action? OnChange;

        private void Notify() => OnChange?.Invoke();

        // ── Constructor ───────────────────────────────────────────────────────

        public SessionManager(UPSClientService Client)
        {
            _Client = Client;
        }

        // ── Profile Registration ──────────────────────────────────────────────

        /// <summary>
        /// Registers an LLM profile and initializes its provider connection.
        /// For HTTP: registers the endpoint in the UPS module registry.
        /// </summary>
        public async Task RegisterProfileAsync(
            LLMProfile Profile,
            CancellationToken Token = default)
        {
            _Profiles[Profile.ProfileId] = Profile;


            if (_Client.SupportsModelDiscovery(Profile))
            {
                var Discovered = await _Client.DiscoverModelsAsync(Profile, Token);
                if (Discovered.Count > 0)
                {
                    Profile.MergeDiscoveredModels(Discovered);
                }
            }

            // Register ILLMAdapter instances for each model in the profile.
            // ClaudeAdapter handles Anthropic; OpenAiCompatibleAdapter handles everything else.
            await _Client.RegisterLLMProfileAsync(Profile, Token);


            Notify();
        }

        /// <summary>
        /// Registers any HTTP endpoint as a routable UPS module.
        /// Compatibility support for registering a direct HTTP endpoint.
        /// </summary>
        public async Task RegisterHttpEndpointAsync(
            string ModuleName,
            string EndpointUrl,
            Dictionary<string, string>? DefaultHeaders = null,
            CancellationToken Token = default)
        {
            await _Client.RegisterHttpEndpointAsync(
                ModuleName,
                EndpointUrl,
                DefaultHeaders,
                Token);

            Notify();
        }

        // ── Session Management ────────────────────────────────────────────────

        /// <summary>
        /// Creates a session from a ModelProfile within an LLMProfile.
        /// This is the primary session creation path.
        /// </summary>
        public UPSSession CreateSession(
            LLMProfile Profile,
            ModelProfile Model,
            string? TargetOverride = null,
            string Method = "chat")
        {
            var Target = TargetOverride ?? Profile.ResolvedProvider;

            var Entry = new SessionModelEntry
            {
                Id = Model.Id,
                Label = Model.Label,
                Type = Model.Type
            };

            var Session = new UPSSession(Profile, Model, Entry, _Client, Target, Method);

            _Sessions[Session.SessionId] = Session;
            Sessions.Add(Session);

            SetActiveSession(Session.SessionId);
            Notify();

            return Session;
        }

        /// <summary>
        /// Creates sessions for all active models across all active profiles.
        /// </summary>
        public IEnumerable<UPSSession> CreateActiveModelSessions()
        {
            var Created = new List<UPSSession>();

            foreach (var (Profile, Model) in ActiveModels)
            {
                var Session = CreateSession(Profile, Model);
                Created.Add(Session);
            }

            return Created;
        }

        // ── Active Session ────────────────────────────────────────────────────

        /// <summary>
        /// Sets the focused session — the one the input pane sends to.
        /// </summary>
        public void SetActiveSession(string SessionId)
        {
            if (_Sessions.TryGetValue(SessionId, out var Session))
            {
                ActiveSession = Session;
                Notify();
            }
        }

        // ── Broadcast Group ───────────────────────────────────────────────────

        /// <summary>
        /// Adds a session to the broadcast group.
        /// </summary>
        public void AddToBroadcast(string SessionId)
        {
            if (_Sessions.TryGetValue(SessionId, out var Session) &&
                !BroadcastGroup.Contains(Session))
            {
                BroadcastGroup.Add(Session);
                Notify();
            }
        }

        /// <summary>
        /// Removes a session from the broadcast group.
        /// </summary>
        public void RemoveFromBroadcast(string SessionId)
        {
            if (_Sessions.TryGetValue(SessionId, out var Session))
            {
                BroadcastGroup.Remove(Session);
                Notify();
            }
        }

        /// <summary>
        /// Adds all active sessions to the broadcast group.
        /// </summary>
        public void SelectAllForBroadcast()
        {
            foreach (var Session in _Sessions.Values.Where(S => S.IsActive))
            {
                if (!BroadcastGroup.Contains(Session))
                    BroadcastGroup.Add(Session);
            }
            Notify();
        }

        /// <summary>
        /// Clears the broadcast group.
        /// </summary>
        public void ClearBroadcast()
        {
            BroadcastGroup.Clear();
            Notify();
        }

        // ── Close ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Closes and removes a session by session ID.
        /// </summary>
        public void CloseSession(string SessionId)
        {
            if (_Sessions.TryRemove(SessionId, out var Session))
            {
                Session.Close();
                Sessions.Remove(Session);
                BroadcastGroup.Remove(Session);

                if (ActiveSession?.SessionId == SessionId)
                    ActiveSession = Sessions.FirstOrDefault();

                Notify();
            }
        }

        /// <summary>
        /// Closes all sessions.
        /// </summary>
        public void CloseAll()
        {
            foreach (var Session in _Sessions.Values)
                Session.Close();

            _Sessions.Clear();
            Sessions.Clear();
            BroadcastGroup.Clear();
            ActiveSession = null;
            Notify();
        }

        // ── Send ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a message to the focused active session only.
        /// </summary>
        public async Task SendToActiveAsync(
            string Message,
            CancellationToken Token = default)
        {
            if (ActiveSession == null)
                throw new InvalidOperationException("No active session.");

            await ActiveSession.SendAsync(Message, Token);
        }

        /// <summary>
        /// Broadcasts a message to all sessions in the broadcast group in parallel.
        /// Each session handles its own response independently.
        /// </summary>
        public async Task BroadcastAsync(
            string Message,
            CancellationToken Token = default)
        {
            var Targets = BroadcastGroup.Any()
                ? BroadcastGroup
                : (IEnumerable<UPSSession>)_Sessions.Values.Where(S => S.IsActive);

            var Tasks = Targets.Select(S => S.SendAsync(Message, Token));
            await Task.WhenAll(Tasks);
        }

        /// <summary>
        /// Broadcasts to all sessions within a specific LLM profile.
        /// </summary>
        public async Task BroadcastToProfileAsync(
            string ProfileId,
            string Message,
            CancellationToken Token = default)
        {
            var ProfileSessions = _Sessions.Values
                .Where(S => S.IsActive &&
                    S.ModelEntry.Id.Contains(ProfileId,
                        StringComparison.OrdinalIgnoreCase));

            var Tasks = ProfileSessions.Select(S => S.SendAsync(Message, Token));
            await Task.WhenAll(Tasks);
        }

        /// <summary>
        /// Sends a prompt to multiple model targets and returns their UPS responses.
        /// </summary>
        public async Task<List<UPSResponse>> BroadcastAsync(
            string prompt,
            List<string> targetModelIds,
            Dictionary<string, string>? headers = null)
        {
            if (targetModelIds == null || targetModelIds.Count == 0)
            {
                return new List<UPSResponse>();
            }

            var tasks = targetModelIds
                .Select(modelId => SendToModelAsync(prompt, modelId, headers));

            return (await Task.WhenAll(tasks).ConfigureAwait(false)).ToList();
        }

        /// <summary>
        /// Sends a prompt to a single model target and returns the UPS response.
        /// </summary>
        public async Task<UPSResponse> SendToModelAsync(
            string prompt,
            string modelProfileId,
            Dictionary<string, string>? headers = null)
        {
            var profileRecord = GetModelProfileRecord(modelProfileId);
            if (profileRecord == null)
            {
                throw new InvalidOperationException($"Profile {modelProfileId} not found");
            }

            var payload = new UPSPayload
            {
                Content = prompt,
                Headers = headers ?? new Dictionary<string, string>(),
                FormatHint = "text/plain"
            };

            return await _Client.SendAsync(
                payload,
                profileRecord.Value.Profile,
                profileRecord.Value.Model).ConfigureAwait(false);
        }

        // ── Diagnostics ───────────────────────────────────────────────────────

        /// <summary>
        /// Total number of open sessions.
        /// </summary>
        public int SessionCount => _Sessions.Count;

        /// <summary>
        /// Total number of sessions in the broadcast group.
        /// </summary>
        public int BroadcastCount => BroadcastGroup.Count;

        /// <summary>
        /// All registered UPS module names.
        /// </summary>
        public IEnumerable<string> RegisteredModules =>
            _Client.RegisteredModules;

        private ModelProfile? GetModelProfile(string profileId)
        {
            return GetModelProfileRecord(profileId)?.Model;
        }

        private (LLMProfile Profile, ModelProfile Model)? GetModelProfileRecord(string profileId)
        {
            foreach (var profile in _Profiles.Values)
            {
                var model = profile.Models.FirstOrDefault(m =>
                    m.Id.Equals(profileId, StringComparison.OrdinalIgnoreCase));

                if (model != null)
                {
                    return (profile, model);
                }
            }

            return null;
        }
    }

    // ── Internal model entry adapter ──────────────────────────────────────────

    /// <summary>
    /// Adapts a ModelProfile into an IModelEntry for session creation.
    /// </summary>
    internal class SessionModelEntry : IModelEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
