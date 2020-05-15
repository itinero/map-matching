using System.Collections.Generic;
using Itinero.Algorithms.DataStructures;

namespace Itinero.MapMatching
{
    /// <summary>
    /// A map matched segment. Corresponds to a segment in a track but matched to edges in the network.
    /// </summary>
    public class MapMatchSegment
    {
        internal MapMatchSegment(TrackSegment segment, IReadOnlyList<Path> paths)
        {
            Paths = paths;
            Segment = segment;
        }

        /// <summary>
        /// Gets the original segment.
        /// </summary>
        public TrackSegment Segment { get; }

        /// <summary>
        /// Gets the path sequence.
        /// </summary>
        public IReadOnlyList<Path> Paths { get; }
    }
}