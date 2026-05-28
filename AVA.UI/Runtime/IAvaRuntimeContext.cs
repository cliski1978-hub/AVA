using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Services;
using AVA.UI.Errors;
using AVA.UI.Vault.Services;

namespace AVA.UI.Runtime;

/// <summary>
/// Provides a shared UI runtime access surface without exposing raw database contexts.
/// </summary>
public interface IAvaRuntimeContext
{
    /// <summary>
    /// Gets the locally resolved runtime user.
    /// </summary>
    AvaRuntimeUser CurrentUser { get; }

    /// <summary>
    /// Gets the machine name for the current desktop or server runtime.
    /// </summary>
    string MachineName { get; }

    /// <summary>
    /// Gets the storage-neutral Vault operation boundary.
    /// </summary>
    IVaultUiSyncService Vault { get; }

    /// <summary>
    /// Gets the application ID generator.
    /// </summary>
    IAvaIdService Ids { get; }

    /// <summary>
    /// Gets restart-persistent UI/session storage.
    /// </summary>
    ISessionStorageService Storage { get; }

    /// <summary>
    /// Gets the centralized UI error state.
    /// </summary>
    ErrorState Errors { get; }

    /// <summary>
    /// Gets application settings access.
    /// </summary>
    AvaSettingsService Settings { get; }

    /// <summary>
    /// Gets the identity runtime slot.
    /// </summary>
    IIdentityRuntimeContext Identity { get; }

    /// <summary>
    /// Gets the memory runtime slot.
    /// </summary>
    IMemoryRuntimeContext Memory { get; }

    /// <summary>
    /// Gets the Resolution Matrix runtime slot.
    /// </summary>
    IResolutionMatrixContext ResolutionMatrix { get; }
}
