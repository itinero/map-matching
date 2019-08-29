using Itinero.LocalGeo;

namespace Itinero.MapMatching
{
    /// <summary>
    /// A single (possibly) matched point.
    /// </summary>
    public struct MapMatcherPoint
    {
        // TODO: do we need the original location here, that is potentially already present in the router point.
        
        /// <summary>
        /// Creates a new point.
        /// </summary>
        /// <param name="routerPoint">The resolved location.</param>
        /// <param name="coordinate">The original location.</param>
        /// <param name="probability">The probability.</param>
        internal MapMatcherPoint(RouterPoint routerPoint, Coordinate coordinate, float probability)
        {
            RouterPoint = routerPoint;
            Coordinate = coordinate;
            Probability = probability;
        }
        
        /// <summary>
        /// Gets the router point.
        /// </summary>
        public RouterPoint RouterPoint { get; }
        
        /// <summary>
        /// Gets the original location.
        /// </summary>
        public Coordinate Coordinate { get; }
        
        /// <summary>
        /// Gets the probability.
        /// </summary>
        public float Probability { get; }
    }
}