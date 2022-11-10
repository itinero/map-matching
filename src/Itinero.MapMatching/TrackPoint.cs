using System;

namespace Itinero.MapMatching;

/// <summary>
/// Represents a track point.
/// </summary>
public readonly struct TrackPoint
{
    /// <summary>
    /// Creates a new track point.
    /// </summary>
    /// <param name="location">The location.</param>
    public TrackPoint(double longitude, double latitude)
    {
        this.Location = (longitude, latitude);
    }

    /// <summary>
    /// Gets the location.
    /// </summary>
    public (double longitude, double latitude) Location { get; }
}
