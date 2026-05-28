using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Contracts;
using AVA.UPS.Adapter.Dispatcher;

namespace AVA.UPS.Adapter.Host
{
    /// <summary>
    /// Core host for UPS. Creates dispatchers and runs listeners.
    /// </summary>
    public class UPSHost
    {
        private readonly List<IUPSHostTransport> _transports = new();
        private readonly UPSHostContext _context;

        public UPSHost(
            object moduleInstance,
            UPSContractFile contractFile,
            UPSHostOptions? options = null)
        {
            _context = new UPSHostContext
            {
                ModuleInstance = moduleInstance,
                Contract = contractFile,
                Dispatcher = new ModuleDispatcher(moduleInstance, contractFile),
                Options = options ?? new UPSHostOptions()
            };
        }

        public UPSHost AddTransport(IUPSHostTransport transport)
        {
            _transports.Add(transport);
            return this;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            foreach (var t in _transports)
                await t.StartAsync(_context, cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            foreach (var t in _transports)
                await t.StopAsync(cancellationToken);
        }
    }
}
