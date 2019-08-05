using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Itinero.LocalGeo;

namespace Itinero.MapMatching
{
    public struct TrackPoint
    {
        public Coordinate Coord { get; }
        public DateTime? Time { get; }
        public float? Hdop { get; }

        public TrackPoint(Coordinate coord, DateTime? time = null, float? hdop = null)
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
        private readonly List<TrackPoint> _points;

        public Track(Track track) :
            this(track.Points) 
        {
        
        }

        public Track(IEnumerable<TrackPoint> points)
        {
            _points = points.ToList();
        }

        public Track(List<(Coordinate, DateTime?, float?)> points)
        {
            _points = (from point in points select new TrackPoint(point)).ToList();
        }

        public ReadOnlyCollection<TrackPoint> Points => new ReadOnlyCollection<TrackPoint>(_points);
    }
}