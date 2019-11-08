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
        public static Result<MapMatch> Match(this Router router, Track track, MapMatcherSettings settings = null)
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
        public static Route ToRoute(this Router router, MapMatch result)
        {
            if (result.RouterPoints.Length <= 1) return null;

            // convert to an edge path.
            var path = router.Db.ToEdgePath(result.Path);

            // build the route.
            return router.BuildRoute(result.Profile, router.GetDefaultWeightHandler(result.Profile),
                result.RouterPoints[0], result.RouterPoints[1], path).Value;
        }
    }
}