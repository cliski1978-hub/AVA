namespace AVA.UI.Runtime;

/// <summary>
/// No-op identity runtime context used until AVA.Identity is wired into the UI runtime.
/// </summary>
public sealed class NullIdentityRuntimeContext : IIdentityRuntimeContext
{
}

/// <summary>
/// No-op memory runtime context used until AVA.Memory retrieval is wired into the UI runtime.
/// </summary>
public sealed class NullMemoryRuntimeContext : IMemoryRuntimeContext
{
}

/// <summary>
/// No-op Resolution Matrix context used until orchestration is wired into the UI runtime.
/// </summary>
public sealed class NullResolutionMatrixContext : IResolutionMatrixContext
{
}
