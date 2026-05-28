// ─────────────────────────────────────────────────────────────────────────────
//  Class     : AppState
//  Namespace : AVA.UI.State
//  Purpose   : Centralized application state for Blazor.
//              Owns the SessionManager and UPSClientService.
//              All components bind to this — nothing touches UPS directly.
// ─────────────────────────────────────────────────────────────────────────────

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
        public const string PanelChatOutput = "chat-output";
        public const string PanelSettings = "settings";
        public const string PanelMemoryLog = "memory-log";
        public const string PanelReflection = "reflection";

        // ── Services ──────────────────────────────────────────────────────────

        private readonly AvaSettingsService _SettingsService;
        public readonly UPSClientService UPSClient;
        public readonly SessionManager Sessions;

        // ── Connection State ──────────────────────────────────────────────────

        public bool IsConnected { get; private set; } = false;
        public string ConnectionType { get; private set; } = string.Empty;
        public string ConnectionStatus { get; private set; } = "Not connected";
        public string ConnectionDetails { get; private set; } = string.Empty;

        // ── LLM Profiles ──────────────────────────────────────────────────────

        public List<LLMProfile> LLMProfiles =>
            _SettingsService.AppSettings.LLMProfiles;

        public LLMProfile? SelectedLLMProfile { get; set; }

        // ── Output ────────────────────────────────────────────────────────────

        public ObservableCollection<OutputSegment> OutputSegments { get; } = new();

        // ── Memory Log ────────────────────────────────────────────────────────

        public ObservableCollection<string> MemoryEvents { get; } = new();

        // ── Reflection ────────────────────────────────────────────────────────

        public ObservableCollection<string> Reflections { get; } = new();
        public string LatestInsight { get; private set; } = string.Empty;
        public bool HasContradiction { get; private set; } = false;

        // ── Prompt Preview ────────────────────────────────────────────────────

        public string PromptPreviewText { get; set; } = string.Empty;
        public int PromptTokenEstimate { get; set; } = 0;
        public bool ShowPromptPreview { get; set; } = false;

        // ── Settings ──────────────────────────────────────────────────────────

        public AppSettings AppSettings => _SettingsService.AppSettings;

        public bool UseDirectEndpoints
        {
            get => _SettingsService.AppSettings.UseDirectEndpoints;
            set => _SettingsService.AppSettings.UseDirectEndpoints = value;
        }

        // ── Workspace Panels ──────────────────────────────────────────────────

        public string SelectedNavigationItem { get; private set; } = "Chat";

        // ── Change Notification ───────────────────────────────────────────────

        public event Action? OnChange;

        private void Notify() => OnChange?.Invoke();

        // ── Constructor ───────────────────────────────────────────────────────

        public AppState(
            AvaSettingsService SettingsService,
            IEndpointClientService EndpointClient)
        {
            _SettingsService = SettingsService;
            _SettingsService.LoadSettings();

            UPSClient = new UPSClientService();
            Sessions = new SessionManager(UPSClient);

            Sessions.OnChange += Notify;

            SelectedLLMProfile = LLMProfiles.FirstOrDefault(P => P.IsDefault)
                              ?? LLMProfiles.FirstOrDefault();

            AppendMemory("🧠 Memory log initialized.");
            AppendReflection("🪞 Reflection system initialized.");
            AppendOutput(new OutputSegment { Type = "system", Value = "💬 AVA ready." });

            _ = ConnectAsync();
        }

        // ── UPS Connect ───────────────────────────────────────────────────────

        /// <summary>
        /// Connects all active LLM profiles through the SessionManager.
        /// Each profile is registered with UPS and its models are merged
        /// from saved settings and live API discovery.
        /// </summary>
        public async Task ConnectAsync(CancellationToken Token = default)
        {
            AppendMemory($"🔌 ConnectAsync: {LLMProfiles.Count} profile(s) in settings.");

            var ProfilesToConnect = LLMProfiles.Where(P => P.IsActive).ToList();
            AppendMemory($"🔌 Active profiles: {ProfilesToConnect.Count}. SelectedLLMProfile: {SelectedLLMProfile?.Name ?? "null"}.");

            if (!ProfilesToConnect.Any() && SelectedLLMProfile != null)
                ProfilesToConnect.Add(SelectedLLMProfile);

            if (!ProfilesToConnect.Any())
            {
                AppendMemory("❌ ConnectAsync: no profiles to connect.");
                UpdateStatus("Error", "No profile", "No active LLM profiles found.");
                return;
            }

            try
            {
                UpdateStatus("Connecting", "Connecting",
                    $"Connecting {ProfilesToConnect.Count} profile(s)...");

                foreach (var Profile in ProfilesToConnect)
                {
                    AppendMemory($"🔌 Registering profile: {Profile.Name} ({Profile.ResolvedProvider})...");
                    await Sessions.RegisterProfileAsync(Profile, Token);

                    var ModelCount = Profile.Models.Count;
                    AppendMemory($"✅ {Profile.Name} connected — {ModelCount} model(s) available.");
                }

                IsConnected = true;

                var TotalModels = ProfilesToConnect.Sum(P => P.Models.Count);
                UpdateStatus(
                    "UPS",
                    "Connected",
                    $"{ProfilesToConnect.Count} profile(s), {TotalModels} model(s)");

                Notify();
            }
            catch (Exception Ex)
            {
                IsConnected = false;
                AppendMemory($"❌ ConnectAsync failed: {Ex.GetType().Name}: {Ex.Message}");
                UpdateStatus("UPS", "Failed", Ex.Message);
                AppendOutput(new OutputSegment { Type = "error", Value = Ex.Message });
            }
        }

        /// <summary>
        /// Connects a single LLM profile explicitly.
        /// Used by the Settings pane Connect button per profile.
        /// </summary>
        public async Task ConnectProfileAsync(
            LLMProfile Profile,
            CancellationToken Token = default)
        {
            try
            {
                AppendMemory($"🔌 ConnectProfileAsync: {Profile.Name} ({Profile.ResolvedProvider})...");
                UpdateStatus("Connecting", "Connecting", $"Connecting to {Profile.Name}...");

                await Sessions.RegisterProfileAsync(Profile, Token);

                IsConnected = true;

                var ModelCount = Profile.Models.Count;
                UpdateStatus("UPS", "Connected", $"{Profile.Name} — {ModelCount} model(s) available.");
                AppendMemory($"✅ {Profile.Name} connected — {ModelCount} model(s) available.");

                Notify();
            }
            catch (Exception Ex)
            {
                AppendMemory($"❌ ConnectProfileAsync failed: {Ex.GetType().Name}: {Ex.Message}");
                UpdateStatus("UPS", "Failed", Ex.Message);
                AppendOutput(new OutputSegment { Type = "error", Value = Ex.Message });
            }
        }

        /// <summary>
        /// Tests connectivity to an LLM profile endpoint.
        /// No model involved — just confirms the endpoint is reachable.
        /// </summary>
        public async Task<TestResult> TestEndpointAsync(
            LLMProfile Profile,
            CancellationToken Token = default)
        {
            try
            {
                var IsNomi = Profile.ResolvedProvider
                    .Equals("Nomi", StringComparison.OrdinalIgnoreCase);

                if (IsNomi)
                {
                    var NomiClient = new NomiApiClient(Profile.ApiKey);
                    var Nomis = await NomiClient.GetNomisAsync(Token);
                    return new TestResult
                    {
                        Success = true,
                        Message = $"✅ Nomi connected — {Nomis.Count} character(s) found."
                    };
                }

                var Client = new EndpointClientService();
                var Ok = await Client.ConnectAsync(null, Profile, Token);

                return new TestResult
                {
                    Success = Ok,
                    Message = Ok
                        ? $"✅ Endpoint reachable: {Profile.Endpoint}"
                        : $"❌ Could not reach endpoint: {Profile.Endpoint}"
                };
            }
            catch (Exception Ex)
            {
                return new TestResult { Success = false, Message = $"❌ {Ex.Message}" };
            }
        }

        /// <summary>
        /// Tests connectivity to a specific model within an LLM profile.
        /// Sends "This is a model connection test." to the model.
        /// </summary>
        public async Task<TestResult> TestModelAsync(
            LLMProfile Profile,
            ModelProfile Model,
            CancellationToken Token = default)
        {
            try
            {
                await Sessions.RegisterProfileAsync(Profile, Token);

                var Session = Sessions.CreateSession(Profile, Model);

                await Session.SendAsync("This is a model connection test.", Token);

                var Error = Session.LastError;
                var Reply = Session.Output
                    .LastOrDefault(S => S.Type != "error")
                    ?.Value ?? string.Empty;

                Sessions.CloseSession(Session.SessionId);

                return new TestResult
                {
                    Success = string.IsNullOrWhiteSpace(Error),
                    Message = string.IsNullOrWhiteSpace(Error)
                        ? $"✅ {Model.Label} responded: {Reply[..Math.Min(80, Reply.Length)]}..."
                        : $"❌ {Model.Label}: {Error}"
                };
            }
            catch (Exception Ex)
            {
                return new TestResult { Success = false, Message = $"❌ {Ex.Message}" };
            }
        }

        // ── Chat ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a prompt to all active sessions in parallel.
        /// Single session: behaves as before. Multiple sessions: broadcasts and
        /// appends each reply labelled with its session display name.
        /// </summary>
        public async Task<string> SendPromptAsync(
            string Prompt,
            CancellationToken Token = default)
        {
            if (!IsConnected)
            {
                var Warning = "⚠️ Not connected — go to Settings and click Connect.";
                AppendOutput(new OutputSegment { Type = "system", Value = Warning });
                return Warning;
            }

            var Targets = Sessions.Sessions.Where(S => S.IsActive).ToList();

            if (!Targets.Any())
            {
                var Warning = "⚠️ No active session — pick a model in Chat and click Open.";
                AppendOutput(new OutputSegment { Type = "system", Value = Warning });
                return Warning;
            }

            AppendOutput(new OutputSegment { Type = "text", Value = $"You: {Prompt}" });
            AppendMemory($"📝 Prompt: {Prompt}");

            await Task.WhenAll(Targets.Select(S => S.SendAsync(Prompt, Token)));

            var LastReply = string.Empty;

            foreach (var Session in Targets)
            {
                var Error = Session.LastError;

                if (!string.IsNullOrWhiteSpace(Error))
                {
                    AppendOutput(new OutputSegment { Type = "error", Value = Error });
                    continue;
                }

                var Reply = Session.Output
                    .LastOrDefault(S => S.Type != "error")
                    ?.Value ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(Reply))
                {
                    AppendOutput(new OutputSegment
                    {
                        Type = "text",
                        Value = $"{Session.DisplayName}: {Reply}"
                    });
                    AppendMemory($"🤖 {Session.DisplayName}: {Reply}");
                    LastReply = Reply;
                }
            }

            Notify();
            return LastReply;
        }

        /// <summary>
        /// Broadcasts a prompt to all sessions in the broadcast group.
        /// Each session renders its own response in its own pane.
        /// </summary>
        public async Task BroadcastPromptAsync(
            string Prompt,
            CancellationToken Token = default)
        {
            if (!IsConnected)
            {
                AppendOutput(new OutputSegment
                {
                    Type = "system",
                    Value = "⚠️ Not connected. Please connect in Settings first."
                });
                return;
            }

            AppendOutput(new OutputSegment
            {
                Type = "text",
                Value = $"You (broadcast): {Prompt}"
            });

            AppendMemory($"📡 Broadcast: {Prompt}");

            await Sessions.BroadcastAsync(Prompt, Token);
            Notify();
        }

        // ── Output ────────────────────────────────────────────────────────────

        public void AppendOutput(OutputSegment Segment)
        {
            OutputSegments.Add(Segment);
            Notify();
        }

        public void AppendOutputText(string Text) =>
            AppendOutput(new OutputSegment { Type = "text", Value = Text });

        public void AppendOutputSystem(string Text) =>
            AppendOutput(new OutputSegment { Type = "system", Value = Text });

        public void ClearOutput()
        {
            OutputSegments.Clear();
            Notify();
        }

        // ── Memory ────────────────────────────────────────────────────────────

        public void AppendMemory(string Message)
        {
            MemoryEvents.Add($"[{DateTime.Now:HH:mm:ss}] {Message}");
            Notify();
        }

        public void ClearMemory()
        {
            MemoryEvents.Clear();
            AppendMemory("🧹 Memory log cleared.");
        }

        // ── Reflection ────────────────────────────────────────────────────────

        public void AppendReflection(
            string Insight,
            bool IsContradiction = false,
            string Score = "")
        {
            LatestInsight = Insight;
            HasContradiction = IsContradiction;
            var Tag = IsContradiction ? "⚠ CONTRADICTION" : "✔ ALIGNMENT";
            Reflections.Add($"[{DateTime.Now:HH:mm:ss}] {Tag}: {Insight}");
            if (!string.IsNullOrWhiteSpace(Score))
                Reflections.Add($"↳ Score: {Score}");
            Notify();
        }

        // ── Status ────────────────────────────────────────────────────────────

        public void UpdateStatus(string Type, string Status, string Details = "")
        {
            ConnectionType = Type;
            ConnectionStatus = Status;
            ConnectionDetails = Details;
            Notify();
        }

        // ── Canvas ────────────────────────────────────────────────────────────

        /// <summary>
        /// The active canvas session — holds all cards and layout state.
        /// Backed by AppSettings so it persists across restarts.
        /// </summary>
        public SessionState CurrentSession => AppSettings.ActiveCanvas;

        /// <summary>
        /// Saves the current canvas card positions and state to disk.
        /// Called after drag, close, or layout changes.
        /// </summary>
        public void SaveCanvasState() => _SettingsService.SaveSettings();

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
            var param = response.Payload.FirstOrDefault(p =>
                p.Key == "content" || p.Key == "reply" || p.Key == "text" ||
                p.Type == "string");
            return param?.Value?.ToString() ?? string.Empty;
        }

        // ── Settings ─────────────────────────────────────────────────────────

        public void SaveSettings() => _SettingsService.SaveSettings();
    }

    // ── Test Result ───────────────────────────────────────────────────────────

    /// <summary>
    /// Result of an endpoint or model connectivity test.
    /// </summary>
    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Enables tuple-style deconstruction: var (ok, msg) = testResult;
        /// </summary>
        public void Deconstruct(out bool Success, out string Message)
        {
            Success = this.Success;
            Message = this.Message;
        }
    }

    // ── Minimal Log Service ───────────────────────────────────────────────────

    public class ConsoleLogService : ILogService
    {
        public void LogInfo(string Message) => Console.WriteLine("[INFO] " + Message);
        public void LogWarning(string Message) => Console.WriteLine("[WARN] " + Message);
        public void LogError(string Message) => Console.WriteLine("[ERROR] " + Message);
    }
}
