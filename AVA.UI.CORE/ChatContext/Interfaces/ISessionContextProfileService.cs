using AVA.UI.CORE.ChatContext.Models;

namespace AVA.UI.CORE.ChatContext.Interfaces
{
    /// <summary>
    /// Provides deterministic session context profiles and applies them to model context settings.
    /// </summary>
    public interface ISessionContextProfileService
    {
        /// <summary>
        /// Gets the built-in session context profiles.
        /// </summary>
        IReadOnlyList<SessionContextProfile> GetDefaultProfiles();

        /// <summary>
        /// Gets a built-in profile by type.
        /// </summary>
        SessionContextProfile? GetProfile(SessionContextProfileType profileType);

        /// <summary>
        /// Applies a profile over base model context settings.
        /// </summary>
        ModelContextSettings ApplyProfile(
            ModelContextSettings baseSettings,
            SessionContextProfile profile);
    }
}
