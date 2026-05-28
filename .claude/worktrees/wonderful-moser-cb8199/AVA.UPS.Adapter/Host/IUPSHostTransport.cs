using System.Threading;
using System.Threading.Tasks;

namespace AVA.UPS.Adapter.Host
{
    /// <summary>
    /// Interface implemented by all UPS host listeners.
    /// </summary>
    public interface IUPSHostTransport
    {
        Task StartAsync(UPSHostContext context, CancellationToken cancellationToken = default);
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
