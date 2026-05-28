using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Services;
using AVA.UI.Errors;
using AVA.UI.Vault.Services;

namespace AVA.UI.Runtime;

/// <summary>
/// Default application runtime context for shared UI services and runtime identity.
/// </summary>
public sealed class AvaRuntimeContext : IAvaRuntimeContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AvaRuntimeContext"/> class.
    /// </summary>
    public AvaRuntimeContext(
        IVaultUiSyncService vault,
        IAvaIdService ids,
        ISessionStorageService storage,
        ErrorState errors,
        AvaSettingsService settings,
        IIdentityRuntimeContext identity,
        IMemoryRuntimeContext memory,
        IResolutionMatrixContext resolutionMatrix)
    {
        Vault = vault;
        Ids = ids;
        Storage = storage;
        Errors = errors;
        Settings = settings;
        Identity = identity;
        Memory = memory;
        ResolutionMatrix = resolutionMatrix;
        MachineName = Environment.MachineName;
        CurrentUser = BuildCurrentUser();
    }

    /// <inheritdoc />
    public AvaRuntimeUser CurrentUser { get; }

    /// <inheritdoc />
    public string MachineName { get; }

    /// <inheritdoc />
    public IVaultUiSyncService Vault { get; }

    /// <inheritdoc />
    public IAvaIdService Ids { get; }

    /// <inheritdoc />
    public ISessionStorageService Storage { get; }

    /// <inheritdoc />
    public ErrorState Errors { get; }

    /// <inheritdoc />
    public AvaSettingsService Settings { get; }

    /// <inheritdoc />
    public IIdentityRuntimeContext Identity { get; }

    /// <inheritdoc />
    public IMemoryRuntimeContext Memory { get; }

    /// <inheritdoc />
    public IResolutionMatrixContext ResolutionMatrix { get; }

    private static AvaRuntimeUser BuildCurrentUser()
    {
        var userName = Environment.UserName;
        var safe = string.IsNullOrWhiteSpace(userName) ? "AVA User" : userName.Trim();

        return new AvaRuntimeUser
        {
            Name = safe,
            Email = $"{safe.Replace(' ', '.').ToLowerInvariant()}@local"
        };
    }
}
