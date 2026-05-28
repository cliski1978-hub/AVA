using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace AVA.UPS.Adapter.Host
{
    public class NamedPipeUPSHostListener : UPSHostListener
    {
        private readonly string _pipeName;
        private CancellationTokenSource? _cts;

        public NamedPipeUPSHostListener(string pipeName)
        {
            _pipeName = pipeName;
        }

        public override Task StartAsync(UPSHostContext context, CancellationToken token = default)
        {
            base.StartAsync(context, token);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Task.Run(() => RunServerLoop(_cts.Token), _cts.Token);
            return Task.CompletedTask;
        }

        private async Task RunServerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.InOut,
                    maxNumberOfServerInstances: 10,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token);
                await HandleRequestAsync(server, server, token);
            }
        }

        public override Task StopAsync(CancellationToken token = default)
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }
    }
}