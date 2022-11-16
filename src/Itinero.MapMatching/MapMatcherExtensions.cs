using System;
using System.Collections.Generic;
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

    
    public static Result<IEnumerable<Route>> Route(this MapMatcher matcher, MapMatch match)
    {
        if (matcher.Settings.Profile == null) throw new Exception("Cannot build routes without a profile");
        
        var paths = new List<Path>();

        Path? currentPath = null;
        foreach (var path in match)
        {
            if (!path.HasLength()) continue;
            
            if (currentPath == null)
            {
                currentPath = path;
                continue;
            }

            var merged = currentPath.TryAppend(path);
            if (merged == null)
            {
                paths.Add(currentPath);
                currentPath = path;
                continue;
            }

            currentPath = merged;
        }

        if (currentPath != null)
        {
            paths.Add(currentPath);
        }
                
        var routes = new List<Route>();
        foreach (var path in paths)
        {
            var route = RouteBuilder.Default.Build(matcher.RoutingNetwork, matcher.Settings.Profile, path);
            routes.Add(route);
        }

        return routes;
    }

    private static Path? TryAppend(this Path path, Path next)
    {
        if (!path.IsNext(next)) return null;

        return new [] { path, next }.Merge();
    }
}
