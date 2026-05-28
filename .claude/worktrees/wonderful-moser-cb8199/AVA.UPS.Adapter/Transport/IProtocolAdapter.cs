using System.Threading;
using System.Threading.Tasks;

namespace AVA.UPS.Adapter.Transport
{
    /// <summary>
    /// A dynamically provided protocol adapter used by UPS routing.
    /// Implementations are supplied by the host module.
    /// </summary>
    public interface IProtocolAdapter
    {
        string ProtocolName { get; }

        /// <summary>
        /// Optional config object passed in by the host module.
        /// </summary>
        Task InitializeAsync(object? config = null);

        /// <summary>
        /// Sends a serialized UPS envelope to the external protocol.
        /// </summary>
        Task<byte[]> SendAsync(byte[] payload, CancellationToken token = default);
    }
}
