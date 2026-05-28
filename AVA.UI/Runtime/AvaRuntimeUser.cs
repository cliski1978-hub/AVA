namespace AVA.UI.Runtime;

/// <summary>
/// Describes the locally resolved runtime user for UI coordination.
/// </summary>
public sealed class AvaRuntimeUser
{
    /// <summary>
    /// Gets or sets the display name resolved for the current runtime user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email or local identity token resolved for the current runtime user.
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
