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

        /// <summary>
        /// Returns the route for the given map matching result.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="result">The result.</param>
        /// <returns>A route along the map matched result, possibly there are gaps.</returns>
        public static Route ToRoute(this Router router, MapMatcherResult result)
        {
            if (result.ChosenProjectionPoints.Length <= 1) return null;

            var route = router.BuildRoute(result.Profile, router.GetDefaultWeightHandler(result.Profile),
                result.ChosenProjectionPoints[0].RouterPoint, result.ChosenProjectionPoints[1].RouterPoint, result.RawPaths[0].Value).Value;
            for (var i = 2; i < result.ChosenProjectionPoints.Length; i++)
            {
                var nextRoute = router.BuildRoute(result.Profile, router.GetDefaultWeightHandler(result.Profile),
                    result.ChosenProjectionPoints[i - 1].RouterPoint, result.ChosenProjectionPoints[i].RouterPoint,
                    result.RawPaths[i - 1].Value).Value;
                route = route.Concatenate(nextRoute);
            }

            return route;
        }
    }
}