// ─────────────────────────────────────────────────────────────────────────────
//  Class     : EndpointClientService
//  Namespace : AVA.UI.CORE.Services.Network
//  Purpose   : Legacy HTTP client used for endpoint connectivity testing.
//              Real message routing goes through UPSClientService.
//              ModelName removed — model identity lives in ModelProfile.
// ─────────────────────────────────────────────────────────────────────────────

using System.Net.Http;
using System.Text;
using System.Text.Json;
using AVA.UI.CORE.Interfaces;
using AVA.UI.CORE.Models.Settings;

namespace AVA.UI.CORE.Services.Network
{
    public sealed class EndpointClientService : IEndpointClientService
    {
        private static readonly HttpClient _Http = new HttpClient();

        // ── Connect ───────────────────────────────────────────────────────────

        public async Task<bool> ConnectAsync(
            ConnectionProfile? Profile,
            LLMProfile? SelectedLLM = null,
            CancellationToken Ct = default)
        {
            var (Kind, Url, Headers) = Normalize(Profile, SelectedLLM);

            if (string.IsNullOrWhiteSpace(Url))
                return false;

            try
            {
                switch (Kind)
                {
                    case EndpointKind.Http:
                        {
                            if (Url.Contains("/completions", StringComparison.OrdinalIgnoreCase))
                            {
                                // Use a minimal chat completions probe — no model name needed
                                var TestPayload = new
                                {
                                    model = "probe",
                                    messages = new[] { new { role = "user", content = "ping" } },
                                    max_tokens = 1
                                };

                                var Json = JsonSerializer.Serialize(TestPayload);
                                using var Content = new StringContent(Json, Encoding.UTF8, "application/json");
                                using var PostReq = new HttpRequestMessage(HttpMethod.Post, Url)
                                { Content = Content };
                                ApplyHeaders(PostReq, Headers);

                                using var PostResp = await _Http
                                    .SendAsync(PostReq, HttpCompletionOption.ResponseHeadersRead, Ct)
                                    .ConfigureAwait(false);

                                return (int)PostResp.StatusCode < 500;
                            }

                            // HEAD probe for REST endpoints
                            using var HeadReq = new HttpRequestMessage(HttpMethod.Head, Url);
                            ApplyHeaders(HeadReq, Headers);
                            using var HeadResp = await _Http
                                .SendAsync(HeadReq, HttpCompletionOption.ResponseHeadersRead, Ct)
                                .ConfigureAwait(false);

                            if ((int)HeadResp.StatusCode == 405)
                            {
                                using var GetReq = new HttpRequestMessage(HttpMethod.Get, Url);
                                ApplyHeaders(GetReq, Headers);
                                using var GetResp = await _Http
                                    .SendAsync(GetReq, HttpCompletionOption.ResponseHeadersRead, Ct)
                                    .ConfigureAwait(false);
                                return GetResp.IsSuccessStatusCode;
                            }

                            return HeadResp.IsSuccessStatusCode;
                        }

                    case EndpointKind.WebSocket:
                    case EndpointKind.Grpc:
                        return Uri.TryCreate(Url, UriKind.Absolute, out _);

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        // ── Send Text ─────────────────────────────────────────────────────────

        public async Task<EndpointResult> SendTextAsync(
            string Text,
            ConnectionProfile? Profile,
            LLMProfile? SelectedLLM = null,
            CancellationToken Ct = default)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return EndpointResult.Fail(400, "Text payload cannot be empty.");

            var (Kind, Url, Headers) = Normalize(Profile, SelectedLLM);

            if (string.IsNullOrWhiteSpace(Url))
                return EndpointResult.Fail(400, "No endpoint URL available.");

            return Kind switch
            {
                EndpointKind.Http => await PostJsonAsync(
                    Url,
                    BuildJsonPayload(Text, Profile, SelectedLLM),
                    Headers, Ct).ConfigureAwait(false),

                EndpointKind.WebSocket =>
                    EndpointResult.Fail(501, "WebSocket send not yet implemented."),

                EndpointKind.Grpc =>
                    EndpointResult.Fail(501, "gRPC send not yet implemented."),

                _ => EndpointResult.Fail(400, "Unsupported endpoint kind.")
            };
        }

        // ── Send Raw JSON ─────────────────────────────────────────────────────

        public async Task<EndpointResult> SendJsonAsync(
            string JsonPayload,
            ConnectionProfile? Profile,
            LLMProfile? SelectedLLM = null,
            CancellationToken Ct = default)
        {
            if (string.IsNullOrWhiteSpace(JsonPayload))
                return EndpointResult.Fail(400, "JSON payload cannot be empty.");

            var (Kind, Url, Headers) = Normalize(Profile, SelectedLLM);

            if (string.IsNullOrWhiteSpace(Url))
                return EndpointResult.Fail(400, "No endpoint URL available.");

            return Kind == EndpointKind.Http
                ? await PostJsonAsync(Url, JsonPayload, Headers, Ct).ConfigureAwait(false)
                : EndpointResult.Fail(501, $"{Kind} send not implemented.");
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private static async Task<EndpointResult> PostJsonAsync(
            string Url,
            string Json,
            Dictionary<string, string> Headers,
            CancellationToken Ct)
        {
            using var Content = new StringContent(Json, Encoding.UTF8, "application/json");
            using var Req = new HttpRequestMessage(HttpMethod.Post, Url)
            { Content = Content };
            ApplyHeaders(Req, Headers);

            try
            {
                using var Resp = await _Http
                    .SendAsync(Req, HttpCompletionOption.ResponseContentRead, Ct)
                    .ConfigureAwait(false);

                var Body = await Resp.Content.ReadAsStringAsync(Ct).ConfigureAwait(false);

                return Resp.IsSuccessStatusCode
                    ? EndpointResult.Ok((int)Resp.StatusCode, Body)
                    : EndpointResult.Fail((int)Resp.StatusCode, $"HTTP {Resp.StatusCode}", Body);
            }
            catch (Exception Ex)
            {
                return EndpointResult.Fail(0, $"Exception: {Ex.Message}");
            }
        }

        private static void ApplyHeaders(
            HttpRequestMessage Req,
            Dictionary<string, string> Headers)
        {
            if (Headers == null) return;
            foreach (var Kv in Headers)
            {
                if (string.Equals(Kv.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    continue;
                Req.Headers.Remove(Kv.Key);
                Req.Headers.TryAddWithoutValidation(Kv.Key, Kv.Value ?? string.Empty);
            }
        }

        private static string BuildJsonPayload(
            string Text,
            ConnectionProfile? Profile,
            LLMProfile? Llm)
        {
            // Agent payload
            if (Profile is AgentConnectionProfile)
            {
                return JsonSerializer.Serialize(new
                {
                    type = "chat",
                    input = Text,
                    model = Llm?.Name ?? "default",
                    meta = new { source = "AVA.UI", timestamp = DateTimeOffset.UtcNow }
                });
            }

            var Endpoint = Llm?.Endpoint?.ToLowerInvariant() ?? string.Empty;

            // Completion format
            if (Endpoint.Contains("/v1/completions"))
            {
                return JsonSerializer.Serialize(new
                {
                    model = Llm?.Name ?? "default",
                    prompt = Text,
                    max_tokens = Llm?.MaxTokens ?? 2048,
                    temperature = Llm?.Temperature ?? 0.7
                });
            }

            // Chat format
            return JsonSerializer.Serialize(new
            {
                model = Llm?.Name ?? "default",
                messages = new[]
                {
                    new { role = "user", content = Text }
                },
                max_tokens = Llm?.MaxTokens ?? 2048,
                temperature = Llm?.Temperature ?? 0.7
            });
        }

        private (EndpointKind Kind, string Url, Dictionary<string, string> Headers)
            Normalize(ConnectionProfile? Profile, LLMProfile? Llm)
        {
            var Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var Url = string.Empty;
            var Kind = EndpointKind.Http;

            // 1. Direct LLM endpoint mode
            if (Llm != null && !string.IsNullOrWhiteSpace(Llm.Endpoint))
            {
                Url = Llm.Endpoint.Trim();

                Kind = Url.StartsWith("ws", StringComparison.OrdinalIgnoreCase)
                    ? EndpointKind.WebSocket
                    : Url.StartsWith("grpc", StringComparison.OrdinalIgnoreCase)
                    ? EndpointKind.Grpc
                    : EndpointKind.Http;

                if (!string.IsNullOrWhiteSpace(Llm.ApiKey))
                    Headers["Authorization"] = $"Bearer {Llm.ApiKey}";

                if (!string.IsNullOrWhiteSpace(Llm.Secret))
                    Headers["X-AVA-Secret"] = Llm.Secret;

                Headers["User-Agent"] = "AVA.UI/EndpointClient";
                return (Kind, Url, Headers);
            }

            // 2. Connection profile
            if (Profile != null)
            {
                var Port = Profile.Port ?? 8080;
                var Host = Profile.Hostname ?? "localhost";

                Url = Profile switch
                {
                    RemoteConnectionProfile R =>
                        $"{(R.UseHttps ? "https" : "http")}://{Host}:{Port}{R.BasePath}",
                    AgentConnectionProfile =>
                        $"http://{Host}:{Port}/api/agent",
                    LocalConnectionProfile =>
                        $"http://{Host}:{Port}/api/core",
                    _ =>
                        $"http://{Host}:{Port}/v1"
                };

                if (Profile is RemoteConnectionProfile Remote &&
                    !string.IsNullOrWhiteSpace(Remote.AuthToken))
                    Headers["Authorization"] = $"Bearer {Remote.AuthToken}";

                Headers["User-Agent"] = "AVA.UI/EndpointClient";
                return (Kind, Url, Headers);
            }

            // 3. Fallback
            Headers["User-Agent"] = "AVA.UI/EndpointClient";
            return (EndpointKind.Http, string.Empty, Headers);
        }

        private enum EndpointKind { Http, WebSocket, Grpc }
    }
}