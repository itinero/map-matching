using Itinero.Profiles;

namespace Itinero.MapMatching
{
    /// <summary>
    /// The map matcher settings.
    /// </summary>
    public class MapMatcherSettings : IMapMatcherSettings
    {
        /// <summary>
        /// The maximum snapping distance.
        /// </summary>
        public double SnappingMaxDistance { get; set; } = 50f;

        /// <summary>
        /// The minimum distance used for routing.
        /// </summary>
        /// <remarks>If the distance between location is smaller than this number, this number will be used.</remarks>
        public double MinRouteDistance { get; set; } = 20f;

        /// <summary>
        /// The factor use to determine the maximum search distance between two location.
        /// </summary>
        /// <remarks>The beeline distance between two locations is multiplied by this factor and used as the max search distance.</remarks>
        public double RouteDistanceFactor { get; set; } = 2f;

        /// <summary>
        /// Gets or sets the profile.
        /// </summary>
        public Profile Profile { get; set; } = Itinero.Profiles.Lua.Osm.OsmProfiles.Bicycle;
        
        /// <summary>
        /// The default settings.
        /// </summary>
        public static readonly MapMatcherSettings Default = new MapMatcherSettings();
    }
}