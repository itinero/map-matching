namespace Itinero.MapMatching
{
    /// <summary>
    /// A map matched segment. Corresponds to a segment in a track but matched to edges in the network.
    /// </summary>
    public class MapMatchSegment
    {
        internal MapMatchSegment(TrackSegment segment, MapMatchPath path)
        {
            Path = path;
            Segment = segment;
        }

        /// <summary>
        /// Gets the original segment.
        /// </summary>
        public TrackSegment Segment { get; }

        /// <summary>
        /// Gets the map.
        /// </summary>
        public MapMatchPath Path { get; }
    }
}