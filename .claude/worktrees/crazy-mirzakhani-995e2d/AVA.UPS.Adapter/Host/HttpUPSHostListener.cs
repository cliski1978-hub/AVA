using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Utils;

namespace AVA.UPS.Adapter.Host
{
    /// <summary>
    /// Bare-metal HTTP listener for UPS envelopes.
    /// Uses HttpListener (Windows-compatible), no ASP.NET Core required.
    /// POST /invoke → accepts UPSMessageEnvelope JSON and returns a UPSMessageEnvelope JSON.
    /// </summary>
    public class HttpUPSHostListener : UPSHostListener
    {
        private readonly HttpListener _listener = new();
        private readonly string _url;

        public HttpUPSHostListener(string url)
        {
            // Required for HttpListener prefix
            _url = url.EndsWith("/") ? url : url + "/";
        }

        public override async Task StartAsync(UPSHostContext context, CancellationToken token = default)
        {
            await base.StartAsync(context, token);

            _listener.Prefixes.Clear();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            _ = Task.Run(() => RunLoop(context, token), token);
        }

        private async Task RunLoop(UPSHostContext context, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext http;

                try
                {
                    http = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    if (token.IsCancellationRequested)
                        return;

                    continue;
                }

                _ = Task.Run(() => ProcessRequestAsync(context, http, token), token);
            }
        }

        private async Task ProcessRequestAsync(
            UPSHostContext hostContext,
            HttpListenerContext ctx,
            CancellationToken token)
        {
            try
            {
                // Only allow POST /invoke
                if (ctx.Request.HttpMethod != "POST" ||
                    ctx.Request.Url?.AbsolutePath != "/invoke")
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                    return;
                }

                // ------------------------------------------------------------
                // Delegate to the base-class stream handler
                // ------------------------------------------------------------
                using var input = ctx.Request.InputStream;
                using var output = new MemoryStream();

                await HandleRequestAsync(input, output, token);

                // ------------------------------------------------------------
                // Write response buffer to HTTP client
                // ------------------------------------------------------------
                var bytes = output.ToArray();

                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentEncoding = Encoding.UTF8;
                ctx.Response.ContentLength64 = bytes.Length;

                await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length, token);
                ctx.Response.Close();
            }
            catch
            {
                try
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.Close();
                }
                catch { }
            }
        }

        public override Task StopAsync(CancellationToken token = default)
        {
            _listener.Stop();
            _listener.Close();
            return Task.CompletedTask;
        }
    }
}
