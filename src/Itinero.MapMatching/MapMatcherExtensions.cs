using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Network;
using Itinero.Routes;
using Itinero.Routes.Builders;
using Itinero.Routes.Paths;

namespace Itinero.MapMatching;

/// <summary>
/// Contains extensions 
/// </summary>
public static class MapMatcherExtensions
{
    /// <summary>
    /// Returns a route for each segment of the track matched.
    /// </summary>
    /// <param name="matcher">The matcher.</param>
    /// <param name="match">The match.</param>
    /// <returns>The resulting route(s).</returns>
    public static Result<IEnumerable<Route>> Routes(this MapMatcher matcher, MapMatch match)
    {
        if (matcher.Settings.Profile == null) throw new Exception("Cannot build routes without a profile");

        var routes = new List<Route>();
        foreach (var path in match)
        {
            var route = RouteBuilder.Default.Build(matcher.RoutingNetwork, matcher.Settings.Profile, path);
            routes.Add(route);
        }

        return routes;
    }

    public static IEnumerable<Route> Route(this MapMatcher matcher, MapMatch match)
    {
        if (matcher.Settings.Profile == null) throw new Exception("Cannot build routes without a profile");

        var paths = matcher.MergedPaths(match);

        var routes = new List<Route>();
        foreach (var path in paths)
        {
            var route = RouteBuilder.Default.Build(matcher.RoutingNetwork, matcher.Settings.Profile, path);
            routes.Add(route);
        }

        return routes;
    }

    public static IEnumerable<Path> MergedPaths(this MapMatcher matcher, MapMatch match)
    {
        if (matcher.Settings.Profile == null) throw new Exception("Cannot build routes without a profile");

        var toMerge = new List<Path>(match);
        var paths = new List<Path>();

        var merge = true;
        while (merge)
        {
            merge = false;
            
            Path? currentPath = null;
            foreach (var path in toMerge)
            {
                path.Trim();
                if (!path.HasLength()) continue;

                if (currentPath == null)
                {
                    currentPath = path;
                    continue;
                }

                var merged = currentPath.TryAppend(path);
                if (merged == null)
                {
                    merged = currentPath.TryMergeAsUTurn(path);
                    if (merged == null)
                    {
                        // merged = currentPath.TryMergeOverlap(path);
                        // if (merged == null)
                        // {
                            paths.Add(currentPath);
                            currentPath = path;
                            continue;
                        // }
                    }
                }

                merge = true;
                currentPath = merged;
            }

            if (currentPath != null)
            {
                paths.Add(currentPath);
            }

            toMerge = paths;
            paths = new List<Path>();
        }

        return toMerge;
    }

    private static Path? TryAppend(this Path path, Path next)
    {
        if (!path.IsNext(next)) return null;

        return new[] { path, next }.Merge();
    }
}
