using Itinero.Profiles;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Abstract readonly map matcher settings.
    /// </summary>
    public interface IMapMatcherSettings
    {
        /// <summary>
        /// The maximum snapping distance.
        /// </summary>
        double SnappingMaxDistance { get; }

        /// <summary>
        /// The minimum distance used for routing.
        /// </summary>
        /// <remarks>If the distance between location is smaller than this number, this number will be used.</remarks>
        double MinRouteDistance { get; }

        /// <summary>
        /// The factor use to determine the maximum search distance between two location.
        /// </summary>
        /// <remarks>The beeline distance between two locations is multiplied by this factor and used as the max search distance.</remarks>
        double RouteDistanceFactor { get; }

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        Profile Profile { get; set; }
    }
}