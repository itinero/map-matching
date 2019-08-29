using Itinero.Profiles;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Contains extension method for the router class.
    /// </summary>
    public static class RouterExtensions
    {
        /// <summary>
        /// Matches the given track using the given profile to the road network.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="profile">The profile to use.</param>
        /// <param name="track">The track to match.</param>
        /// <returns></returns>
        public static Result<MapMatcherResult> Match(this Router router, Profile profile, Track track)
        {
            var matcher = new MapMatcher(router, profile);
            return matcher.TryMatch(track);
        }
    }
}