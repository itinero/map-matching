using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

            _segments = new List<(TrackSegment segment, int index)>();
            if (segments == null) return;
            
            var index = 0;
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
        public Track(IEnumerable<((double longitude, double latitude), DateTime?, double?)> points, IEnumerable<(int size, IEnumerable<(string key, string value)> attributes)> segments = null)
        {
            _points = (from point in points select new TrackPoint(point.Item1, point.Item2, point.Item3)).ToList();

            _segments = new List<(TrackSegment segment, int index)>();
            if (segments == null) return;

            var index = 0;
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
        /// Gets the segment at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The segment.</returns>
        public TrackSegment Segment(int index)
        {
            return _segments[index].segment;
        }

        /// <summary>
        /// Gets the segment for the given track point. For track points where one segment ends and another begins the beginning segment is returned. The last segment is returned for the last track point.
        /// </summary>
        /// <param name="trackIndex">The track index.</param>
        /// <returns>The segment for the given track point and the track index where it starts.</returns>
        public (TrackSegment segment, int start) SegmentFor(int trackIndex)
        {
            for (var s = 0; s < this.SegmentCount; s++)
            {
                var segment = _segments[s];
                var end = segment.index + segment.segment.Size;

                if (end == this.Count && trackIndex == this.Count) return segment;
                if (end <= trackIndex) continue;

                return segment;
            }

            throw new ArgumentOutOfRangeException(nameof(trackIndex), "Segment not found at the given track point.");
        }
        
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