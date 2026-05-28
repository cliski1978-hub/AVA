// ─────────────────────────────────────────────────────────────────────────────
//  Class     : UPSSession
//  Namespace : AVA.UI.CORE.UPS.Sessions
//  Purpose   : Represents a single active conversation with one target.
//              Owns its own message history, identity, and routing target.
//              The UI binds one pane per session.
//              Sessions are created from a model entry and routed through
//              UPSClientService — nothing talks to UPS directly.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Interfaces;
using AVA.UPS.Adapter.Models;
using AVA.UPS.Adapter.Utils;
using AVA.UI.CORE.Models;
using AVA.UI.CORE.Models.Settings;
using AVA.UI.CORE.UPS.Client;

namespace AVA.UI.CORE.UPS.Sessions
{
    /// <summary>
    /// Represents a single active conversation bound to one UPS target.
    /// </summary>
    public class UPSSession
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>
        /// Unique session identifier.
        /// </summary>
        public string SessionId { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The model entry this session is bound to.
        /// e.g. a specific room, an HTTP endpoint.
        /// </summary>
        public IModelEntry ModelEntry { get; }

        /// <summary>
        /// Human readable display name for the UI.
        /// </summary>
        public string DisplayName => ModelEntry.Label;

        /// <summary>
        /// The target module name used for UPS routing.
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// The target method name used for UPS routing.
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Optional LLM profile used by the modern profile/model send path.
        /// </summary>
        public LLMProfile? Profile { get; }

        /// <summary>
        /// Optional model profile used by the modern profile/model send path.
        /// </summary>
        public ModelProfile? ModelProfile { get; }

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>
        /// True when this session is actively connected and ready to send.
        /// </summary>
        public bool IsActive { get; private set; } = false;

        /// <summary>
        /// True when a message is currently in flight.
        /// </summary>
        public bool IsSending { get; private set; } = false;

        /// <summary>
        /// Last error message if the session encountered a failure.
        /// </summary>
        public string? LastError { get; private set; }

        // ── Output ────────────────────────────────────────────────────────────

        /// <summary>
        /// Output segments rendered in the UI for this session.
        /// </summary>
        public ObservableCollection<OutputSegment> Output { get; } = new();

        // ── Services ──────────────────────────────────────────────────────────

        private readonly UPSClientService _Client;

        // ── Change Notification ───────────────────────────────────────────────

        /// <summary>
        /// Fires whenever session state changes so the UI can re-render.
        /// </summary>
        public event Action? OnChange;

        private void Notify() => OnChange?.Invoke();

        // ── Constructor ───────────────────────────────────────────────────────

        public UPSSession(
            IModelEntry ModelEntry,
            UPSClientService Client,
            string Target,
            string Method = "chat")
        {
            this.ModelEntry = ModelEntry;
            _Client = Client;
            this.Target = Target;
            this.Method = Method;
            IsActive = true;
        }

        /// <summary>
        /// Creates a session bound to a concrete LLM profile and model.
        /// </summary>
        public UPSSession(
            LLMProfile Profile,
            ModelProfile ModelProfile,
            IModelEntry ModelEntry,
            UPSClientService Client,
            string Target,
            string Method = "chat")
            : this(ModelEntry, Client, Target, Method)
        {
            this.Profile = Profile;
            this.ModelProfile = ModelProfile;
        }

        // ── Send ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Sends a user message through UPS and appends the response to Output.
        /// </summary>
        public async Task SendAsync(string UserMessage, CancellationToken Token = default)
        {
            if (!IsActive)
            {
                AppendError("Session is not active.");
                return;
            }

            if (IsSending)
            {
                AppendError("A message is already in flight.");
                return;
            }

            IsSending = true;
            LastError = null;
            Notify();

            // Append user message to output
            AppendOutput(new OutputSegment
            {
                Type = "text",
                Value = UserMessage
            });

            try
            {
                if (Profile != null && ModelProfile != null)
                {
                    var Payload = new UPSPayload
                    {
                        Content = UserMessage,
                        FormatHint = "text/plain"
                    };

                    var DirectResponse = await _Client.SendAsync(
                        Payload,
                        Profile,
                        ModelProfile,
                        Token).ConfigureAwait(false);

                    if (!DirectResponse.Success)
                    {
                        AppendError(DirectResponse.ErrorMessage ?? "No response received from UPS.");
                        return;
                    }

                    AppendOutput(new OutputSegment
                    {
                        Type = "text",
                        Value = ExtractContent(DirectResponse)
                    });

                    return;
                }

                var Params = new List<UParam>
                {
                    UParamFactory.String("userMessage", UserMessage),
                };

                var Response = await _Client.SendAsync(
                    Target,
                    Method,
                    Params,
                    Token: Token);

                if (Response == null)
                {
                    AppendError("No response received from UPS.");
                    return;
                }

                if (Response.Error != null)
                {
                    AppendError($"{Response.Error.Code}: {Response.Error.Message}");
                    return;
                }

                var ReplyText = Response.Payload?
                    .FirstOrDefault(P => P.Key == "assistantMessage")
                    ?.Value?.ToString()
                    ?? string.Empty;

                AppendOutput(new OutputSegment
                {
                    Type = "text",
                    Value = ReplyText
                });
            }
            catch (Exception Ex)
            {
                LastError = Ex.Message;
                AppendError(Ex.Message);
            }
            finally
            {
                IsSending = false;
                Notify();
            }
        }

        // ── Output Helpers ────────────────────────────────────────────────────

        private void AppendOutput(OutputSegment Segment)
        {
            Output.Add(Segment);
            Notify();
        }

        private void AppendError(string Message)
        {
            Output.Add(new OutputSegment
            {
                Type = "error",
                Value = Message
            });
            LastError = Message;
            Notify();
        }

        private static string ExtractContent(UPSResponse Response)
        {
            if (!string.IsNullOrWhiteSpace(Response.Content))
                return Response.Content;

            var Param = Response.Payload.FirstOrDefault(P =>
                P.Key == "content" ||
                P.Key == "assistantMessage" ||
                P.Key == "reply" ||
                P.Key == "text" ||
                P.Type == "string");

            return Param?.Value?.ToString() ?? string.Empty;
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Closes the session and clears output.
        /// </summary>
        public void Close()
        {
            IsActive = false;
            Notify();
        }

        /// <summary>
        /// Clears the output history.
        /// </summary>
        public void ClearOutput()
        {
            Output.Clear();
            Notify();
        }
    }
}
