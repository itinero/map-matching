using System.Collections.Generic;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Represents a track segment, a part of the track with the same attributes.
    /// </summary>
    public class TrackSegment
    {
        /// <summary>
        /// Creates a new track segment.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="attributes">The attributes.</param>
        public TrackSegment(int size, IEnumerable<(string key, string value)> attributes)
        {
            Size = size;
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        public IEnumerable<(string key, string value)> Attributes { get; }
        
        /// <summary>
        /// Gets the size.
        /// </summary>
        public int Size { get; }
    }
}