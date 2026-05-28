using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.LLMAdapters
{
    /// <summary>
    /// ILLMAdapter for ChatGPT-style OpenAI chat completions endpoints.
    /// </summary>
    public sealed class ChatGPTAdapter : ILLMAdapter
    {
        private readonly OpenAiCompatibleAdapter _inner;

        /// <summary>
        /// Initializes a ChatGPT adapter from a normalized config bag.
        /// </summary>
        public ChatGPTAdapter(LLMAdapterConfig config, HttpClient? httpClient = null)
        {
            ArgumentNullException.ThrowIfNull(config);
            config.Provider = string.IsNullOrWhiteSpace(config.Provider) ? "ChatGPT" : config.Provider;
            config.Endpoint = string.IsNullOrWhiteSpace(config.Endpoint) ? "https://api.openai.com" : config.Endpoint;
            _inner = new OpenAiCompatibleAdapter(config, httpClient);
        }

        /// <inheritdoc />
        public string ProviderId => "chatgpt";

        /// <inheritdoc />
        public string DisplayName => _inner.DisplayName;

        /// <inheritdoc />
        public string ModelName => _inner.ModelName;

        /// <inheritdoc />
        public Task<LLMConnectionResult> ConnectAsync(CancellationToken cancellationToken = default) =>
            _inner.ConnectAsync(cancellationToken);

        /// <inheritdoc />
        public Task<UPSResponse> SendAsync(UPSPayload payload, CancellationToken cancellationToken = default) =>
            _inner.SendAsync(payload, cancellationToken);

        /// <inheritdoc />
        public Task<LLMTestResult> TestAsync(CancellationToken cancellationToken = default) =>
            _inner.TestAsync(cancellationToken);

        /// <inheritdoc />
        public Task DisconnectAsync() => _inner.DisconnectAsync();

        /// <inheritdoc />
        public Task<LLMCapabilitySet> GetCapabilitiesAsync() => _inner.GetCapabilitiesAsync();
    }
}
