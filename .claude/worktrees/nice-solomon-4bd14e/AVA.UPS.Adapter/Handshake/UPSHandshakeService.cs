using System.Threading;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Utils;
using AVA.UPS.Adapter.Handshake;
using AVA.Identity.Abstractions;

namespace AVA.UPS.Adapter.Handshake
{
    /// <summary>
    /// Performs UPS handshake negotiation between two modules.
    /// </summary>
    public class UPSHandshakeService
    {
        private readonly IUPSIdentityProvider _identityProvider;
        
        public UPSHandshakeService(IUPSIdentityProvider identityProvider)
        {
            _identityProvider = identityProvider;
        }

        /// <summary>
        /// Builds a handshake request for the calling module.
        /// </summary>
        public UPSHandshakeRequest BuildRequest(
            string nodeId,
            string moduleName,
            UPSHandshakeCapabilities capabilities,
            string moduleVersion)
        {
            return new UPSHandshakeRequest
            {
                ProtocolVersion = UPSProtocol.Version,
                Identity = new UPSHandshakeIdentity
                {
                    NodeId = nodeId,
                    ModuleName = moduleName,
                    ModuleVersion = moduleVersion,
                    DisplayName = moduleName,
                    Environment = System.Environment.OSVersion.ToString()
                },
                Capabilities = capabilities
            };
        }

        /// <summary>
        /// Validates an incoming handshake request
        /// and returns host module identity + acceptance status.
        /// </summary>
        public UPSHandshakeResponse ValidateHandshake(
            UPSHandshakeRequest incoming,
            string hostModuleVersion)
        {
            var response = new UPSHandshakeResponse
            {
                ProtocolVersion = UPSProtocol.Version,
                Identity = new UPSHandshakeIdentity
                {
                    NodeId = _identityProvider.GetNodeId(),
                    ModuleName = _identityProvider.GetModuleName(),
                    ModuleVersion = hostModuleVersion,
                    DisplayName = _identityProvider.GetModuleName(),
                    Environment = System.Environment.OSVersion.ToString()
                }
            };

            // -----------------------------------------
            // PROTOCOL VERSION CHECK
            // -----------------------------------------
            if (!UPSCompatibilityValidator.IsVersionCompatible(UPSProtocol.Version, incoming.ProtocolVersion))
            {
                response.Accepted = false;
                response.Error = UPSHandshakeError.VersionMismatch(
                    UPSProtocol.Version,
                    incoming.ProtocolVersion
                );
                return response;
            }

            // -----------------------------------------
            // CAPABILITY OVERLAP CHECK
            // (corrected to compare host vs caller)
            // -----------------------------------------
            if (!UPSCompatibilityValidator.HasTransportOverlap(response.Capabilities, incoming.Capabilities))
            {
                response.Accepted = false;
                response.Error = UPSHandshakeError.CapabilityMismatch();
                return response;
            }

            // -----------------------------------------
            // SUCCESS
            // -----------------------------------------
            response.Accepted = true;

            // Host mirrors its capabilities back — or customize if needed.
            response.Capabilities = response.Capabilities;

            return response;
        }
    }
}
