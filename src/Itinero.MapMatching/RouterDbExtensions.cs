using Itinero.Profiles;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Contains extension method for the router class.
    /// </summary>
    public static class RouterDbExtensions
    {
        /// <summary>
        /// Creates a configured map matcher.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Map matcher results.</returns>
        public static IMapMatcher Matcher(this RouterDb routerDb, MapMatcherSettings settings = null)
        {
            return new MapMatcher(routerDb, settings);
        }
        
        /// <summary>
        /// Creates a configured map matcher.
        /// </summary>
        /// <param name="routerDb">The router db.</param>
        /// <param name="profile">The profile.</param>
        /// <returns>Map matcher results.</returns>
        public static IMapMatcher Matcher(this RouterDb routerDb, Profile profile = null)
        {
            return routerDb.Matcher(new MapMatcherSettings()
            {
                Profile = profile
            });
        }
    }
}