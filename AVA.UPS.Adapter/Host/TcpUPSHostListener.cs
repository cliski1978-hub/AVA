using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AVA.UPS.Adapter.Host
{
    public class TcpUPSHostListener : UPSHostListener
    {
        private readonly int _port;
        private CancellationTokenSource? _cts;

        public TcpUPSHostListener(int port)
        {
            _port = port;
        }

        public override Task StartAsync(UPSHostContext context, CancellationToken token = default)
        {
            base.StartAsync(context, token);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task.Run(() => ListenLoop(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        private async Task ListenLoop(CancellationToken token)
        {
            var listener = new TcpListener(IPAddress.Any, _port);
            listener.Start();

            while (!token.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClient(client, token), token);
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken token)
        {
            using (client)
            {
                var stream = client.GetStream();
                await HandleRequestAsync(stream, stream, token);
            }
        }
        public override Task StopAsync(CancellationToken token = default)
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }
    }
}