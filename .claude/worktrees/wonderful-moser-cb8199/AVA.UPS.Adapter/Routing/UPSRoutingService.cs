using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Models;

namespace AVA.UPS.Adapter.Routing
{
    public class UPSRoutingService
    {
        private readonly ProtocolRouter _router;

        public UPSRoutingService(ProtocolRouter router)
        {
            _router = router;
        }

        public async Task<UPSRouteResult> RouteAsync(
            UPSMessageEnvelope envelope,
            CancellationToken token = default)
        {
            return await _router.RouteAsync(envelope, token);
        }
    }
}
