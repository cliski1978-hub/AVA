namespace AVA.UI.CORE.Interfaces.Storage;

/// <summary>
/// Abstraction layer for ID generation.
/// All consumers must request IDs through this service — never call Guid.NewGuid() directly.
/// The temporary implementation uses GUIDs internally.
/// Future implementations can integrate with AVA.Identity, IdentityStamp,
/// MachineIdentity, or distributed identity systems without changing any
/// storage keys, session logic, ViewModels, or chat log models.
/// </summary>
public interface IAvaIdService
{
    /// <summary>Generates a new unique session ID.</summary>
    string NewSessionId();

    /// <summary>Generates a new unique message ID.</summary>
    string NewMessageId();

    /// <summary>Generates a new unique tool call ID.</summary>
    string NewToolCallId();

    /// <summary>Generates a new unique turn ID for broadcast grouping.</summary>
    string NewTurnId();

    /// <summary>Generates a new unique general-purpose ID.</summary>
    string NewId();
}
