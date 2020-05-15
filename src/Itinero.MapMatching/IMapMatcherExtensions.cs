using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.DataStructures;
using Itinero.Algorithms.Routes;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Contains extensions 
    /// </summary>
    public static class IMapMatcherExtensions
    {
        /// <summary>
        /// The route(s) generate but the match.
        ///
        /// It's possible a track has multiple independent matching.
        /// </summary>
        /// <param name="matcher">The matcher.</param>
        /// <param name="match">The match.</param>
        /// <returns>The resulting route(s).</returns>
        public static Result<IEnumerable<Route>> Routes(this IMapMatcher matcher, Result<MapMatch> match)
        {
            if (match.IsError) return match.ConvertError<IEnumerable<Route>>();

            var routes = new List<Route>();
            var paths = new List<Path>();
            foreach (var path in match.Value)
            {
                if (paths.Count <= 0)
                {
                    paths.Add(path);   
                    continue;
                }
                
                var last = paths[paths.Count - 1];
                if (last.IsNext(path))
                {
                    paths.Add(path);
                    continue;
                }
                
                // concat path and build route.
                var merged = paths.Merge();
                if (merged == null) continue;

                var route = RouteBuilder.Default.Build(matcher.RouterDb, matcher.Settings.Profile, merged);
                routes.Add(route);
            }

            return routes;
        }
    }
}