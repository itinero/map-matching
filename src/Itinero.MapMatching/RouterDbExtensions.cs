using Itinero.Network;
using Itinero.Profiles;

namespace Itinero.MapMatching;

/// <summary>
/// Contains extension method for the router class.
/// </summary>
public static class RouterDbExtensions
{
    /// <summary>
    /// Creates a configured map matcher.
    /// </summary>
    /// <param name="routingNetwork">The router db.</param>
    /// <param name="profile">The profile.</param>
    /// <returns>Map matcher results.</returns>
    public static MapMatcher Matcher(this RoutingNetwork routingNetwork, Profile? profile = null)
    {
        return new MapMatcher(routingNetwork, new MapMatcherSettings()
        {
            Profile = profile
        });
    }
}
