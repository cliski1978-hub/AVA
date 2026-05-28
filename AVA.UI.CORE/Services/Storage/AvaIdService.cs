using AVA.UI.CORE.Interfaces.Storage;

namespace AVA.UI.CORE.Services.Storage;

/// <summary>
/// Temporary GUID-based implementation of IAvaIdService.
/// All ID generation routes through this service so future integration with
/// AVA.Identity, IdentityStamp, MachineIdentity, or distributed identity
/// systems requires no changes to consumers.
/// </summary>
public class AvaIdService : IAvaIdService
{
    public string NewSessionId()  => Guid.NewGuid().ToString();
    public string NewMessageId()  => Guid.NewGuid().ToString();
    public string NewToolCallId() => Guid.NewGuid().ToString();
    public string NewTurnId()     => Guid.NewGuid().ToString("N");
    public string NewId()         => Guid.NewGuid().ToString();
}
