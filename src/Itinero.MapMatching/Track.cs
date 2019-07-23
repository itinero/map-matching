using System;
using Itinero.LocalGeo;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

public struct TrackPoint
{
    public Coordinate Coord { get; }
    public DateTime? Time { get; }
    public float? Hdop { get; }

    public TrackPoint(Coordinate coord) :
        this(coord, null, null) {
    }

    public TrackPoint(Coordinate coord, DateTime? time, float? hdop)
    {
        this.Coord = coord;
        this.Time = time;
        this.Hdop = hdop;
    }

    public TrackPoint((Coordinate coord, DateTime? time, float? hdop) values)
    {
        this.Coord = values.coord;
        this.Time = values.time;
        this.Hdop = values.hdop;
    }
}

public class Track
{
    List<TrackPoint> _points;

    public Track(Track track) :
        this(track.Points) {
    }

    public Track(IEnumerable<(Coordinate coord, DateTime? time, float? hdop)> points)
    {
        _points = (from point in points select new TrackPoint(point)).ToList();
    }

    public Track(IEnumerable<TrackPoint> points)
    {
        _points = points.ToList();
    }

    public ReadOnlyCollection<TrackPoint> Points
    {
        get { return new ReadOnlyCollection<TrackPoint>(_points); }
    }
}
