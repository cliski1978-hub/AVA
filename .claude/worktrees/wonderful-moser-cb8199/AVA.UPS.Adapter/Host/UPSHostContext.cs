// ─────────────────────────────────────────────────────────────────────────────
//  Class     : UPSHostContext
//  Namespace : AVA.UPS.Adapter.Host
//  Purpose   : Context passed to all UPS host listeners.
//              Carries the dispatcher, gateway, contract and options.
// ─────────────────────────────────────────────────────────────────────────────

using AVA.UPS.Adapter.Contracts;
using AVA.UPS.Adapter.Dispatcher;
using AVA.UPS.Adapter.Gateway;

namespace AVA.UPS.Adapter.Host
{
    /// <summary>
    /// Context passed to all UPS host listeners on startup.
    /// Carries everything a listener needs to process inbound requests.
    /// </summary>
    public class UPSHostContext
    {
        /// <summary>
        /// The module instance being hosted.
        /// </summary>
        public object ModuleInstance { get; set; } = default!;

        /// <summary>
        /// The UPS contract file defining all methods for this module.
        /// </summary>
        public UPSContractFile Contract { get; set; } = default!;

        /// <summary>
        /// The dispatcher responsible for invoking module methods.
        /// </summary>
        public ModuleDispatcher Dispatcher { get; set; } = default!;

        /// <summary>
        /// The single UPS gateway entry point for all inbound traffic.
        /// All requests are routed through here regardless of format or transport.
        /// </summary>
        public UPSGateway Gateway { get; set; } = default!;

        /// <summary>
        /// Configuration options for host behavior.
        /// </summary>
        public UPSHostOptions Options { get; set; } = new UPSHostOptions();
    }
}