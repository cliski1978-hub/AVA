// ─────────────────────────────────────────────────────────────────────────────
//  Enums
//  Namespace: AVA.UI.CORE.Models.Enums
//  Purpose  : Shared enumerations used across AVA.UI.CORE.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models.Enums
{
    public enum CoreConnectionMode
    {
        Mock,
        Local,
        Remote,
        Agent
    }

    public enum EndpointProtocol
    {
        Http,
        WebSocket,
        Grpc
    }

    public enum OutputSegmentType
    {
        Text,
        System,
        Error,
        Link,
        Image,
        File,
        Code,
        Markdown
    }

    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Failed
    }
}