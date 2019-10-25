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
        /// <param name="track">The track to match.</param>
        /// <param name="settings">The settings to use.</param>
        /// <returns>Map matcher results.</returns>
        public static Result<MapMatcherResult> Match(this Router router, Track track, MapMatcherSettings settings = null)
        {
            var matcher = new MapMatcher(router, settings);
            return matcher.TryMatch(track);
        }

        /// <summary>
        /// Returns the route for the given map matching result.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="result">The result.</param>
        /// <returns>A route along the map matched result, possibly there are gaps.</returns>
        public static Route ToRoute(this Router router, MapMatcherResult result)
        {
            if (result.RouterPoints.Length <= 1) return null;

            var route = router.BuildRoute(result.Profile, router.GetDefaultWeightHandler(result.Profile),
                result.RouterPoints[0], result.RouterPoints[1], result.RawPaths[0]).Value;
            for (var i = 2; i < result.RouterPoints.Length; i++)
            {
                var nextRoute = router.BuildRoute(result.Profile, router.GetDefaultWeightHandler(result.Profile),
                    result.RouterPoints[i - 1], result.RouterPoints[i],
                    result.RawPaths[i - 1]).Value;
                route = route.Concatenate(nextRoute);
            }

            return route;
        }
    }
}