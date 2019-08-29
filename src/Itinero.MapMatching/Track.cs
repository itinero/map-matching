using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Itinero.Attributes;
using Itinero.LocalGeo;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Represents a track.
    /// </summary>
    public class Track : IEnumerable<TrackPoint>
    {
        private readonly List<TrackPoint> _points;
        private readonly List<(TrackSegment segment, int index)> _segments;

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="segments">The attributes associated with parts of the track.</param>
        public Track(IEnumerable<TrackPoint> points, IEnumerable<TrackSegment> segments = null)
        {
            _points = points.ToList();

            if (segments == null) return;
            var index = 0;
            _segments = new List<(TrackSegment segment, int index)>();
            foreach (var segment in segments)
            {
                _segments.Add((segment, index));
                index += segment.Size;
            }
        }

        /// <summary>
        /// Creates a new track.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="segments">The attributes associated with parts of the track.</param>
        public Track(IEnumerable<(Coordinate, DateTime?, float?)> points, IEnumerable<(int size, IAttributeCollection attributes)> segments = null)
        {
            _points = (from point in points select new TrackPoint(point.Item1, point.Item2, point.Item3)).ToList();

            if (segments == null) return;
            var index = 0;
            _segments = new List<(TrackSegment segment, int index)>();
            foreach (var (size, attributes) in segments)
            {
                var segment = new TrackSegment(size, attributes);
                _segments.Add((segment, index));
                index += segment.Size;
            }
        }

        /// <summary>
        /// Gets the number of track points.
        /// </summary>
        public int Count => _points.Count;

        /// <summary>
        /// Gets the track point at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        public TrackPoint this[int index] => _points[index];

        /// <summary>
        /// Gets the number of segments.
        /// </summary>
        public int SegmentCount => _segments?.Count ?? 0;
        
        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<TrackPoint> GetEnumerator()
        {
            return _points.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}