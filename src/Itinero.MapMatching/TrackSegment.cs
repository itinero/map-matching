using Itinero.Attributes;

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
        public TrackSegment(int size, IAttributeCollection attributes)
        {
            Size = size;
            Attributes = attributes;
        }

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        public IAttributeCollection Attributes { get; }
        
        /// <summary>
        /// Gets the size.
        /// </summary>
        public int Size { get; }
    }
}