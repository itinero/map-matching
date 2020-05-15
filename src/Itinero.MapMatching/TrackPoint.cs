using System;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Represents a track point.
    /// </summary>
    public readonly struct TrackPoint
    {
        /// <summary>
        /// Creates a new track point.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="hdop">The horizontal dilution of precision.</param>
        public TrackPoint((double longitude, double latitude) location, DateTime? timestamp = null, double? hdop = null)
        {
            Location = location;
            Timestamp = timestamp;
            Hdop = hdop;
        }
        
        /// <summary>
        /// Gets the location.
        /// </summary>
        public (double longitude, double latitude) Location { get; }
        
        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTime? Timestamp { get; }
        
        /// <summary>
        /// Gets the horizontal dilution of precision.
        /// </summary>
        public double? Hdop { get; }
    }
}