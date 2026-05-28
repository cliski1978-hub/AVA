// ─────────────────────────────────────────────────────────────────────────────
//  Class     : HttpProtocolAdapter
//  Namespace : AVA.UPS.Adapter.Adapters
//  Purpose   : Outbound HTTP transport adapter.
//              Sends serialized UPSMessageEnvelope to any HTTP endpoint.
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Transport;

namespace AVA.UPS.Adapter
{
    public class HttpProtocolAdapter : IProtocolAdapter
    {
        public string ProtocolName => "http";

        private HttpClient? _Http;
        private string? _BaseUrl;

        public async Task InitializeAsync(object? Config = null)
        {
            _Http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(300)
            };

            if (Config is HttpAdapterConfig HttpConfig)
            {
                _BaseUrl = HttpConfig.BaseUrl?.TrimEnd('/');

                if (HttpConfig.DefaultHeaders != null)
                    foreach (var Header in HttpConfig.DefaultHeaders)
                        _Http.DefaultRequestHeaders.TryAddWithoutValidation(Header.Key, Header.Value);
            }

            await Task.CompletedTask;
        }

        public async Task<byte[]> SendAsync(byte[] Payload, CancellationToken Token = default)
        {
            if (_Http == null)
                throw new InvalidOperationException($"{nameof(HttpProtocolAdapter)} has not been initialized.");

            if (string.IsNullOrWhiteSpace(_BaseUrl))
                throw new InvalidOperationException($"{nameof(HttpProtocolAdapter)} has no BaseUrl configured.");

            var Content = new ByteArrayContent(Payload);
            Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            try
            {
                var Response = await _Http.PostAsync(_BaseUrl, Content, Token);
                Response.EnsureSuccessStatusCode();
                return await Response.Content.ReadAsByteArrayAsync(Token);
            }
            catch (HttpRequestException Ex)
            {
                throw new InvalidOperationException($"HTTP request to '{_BaseUrl}' failed: {Ex.Message}", Ex);
            }
            catch (TaskCanceledException Ex)
            {
                throw new InvalidOperationException($"HTTP request to '{_BaseUrl}' timed out.", Ex);
            }
        }
    }

    public class HttpAdapterConfig
    {
        public string? BaseUrl { get; set; }
        public Dictionary<string, string>? DefaultHeaders { get; set; }
    }
}