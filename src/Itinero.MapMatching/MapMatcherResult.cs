using Itinero.LocalGeo;
using System;
using System.Collections.Generic;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Represents the result of a matching.
    /// </summary>
    public class MapMatcherResult
    {
        // TODO: do we need the route here?
        // TODO: do we need the source track again?
        // TODO: do we need all the projection points?

        /// <summary>
        /// Creates a new result.
        /// </summary>
        /// <param name="source">The source track.</param>
        /// <param name="projectionPoints">The possible projection points.</param>
        /// <param name="chosenIndices">The chosen indices.</param>
        /// <param name="route">The route.</param>
        public MapMatcherResult(Track source, IReadOnlyList<MapMatcherPoint[]> projectionPoints, int[] chosenIndices, Route route)
        {
            Source = source;

            Route = route;
            ProjectionPoints = TwoDClone(projectionPoints);
            ChosenIndices = new int[chosenIndices.Length];
            Array.Copy(chosenIndices, 0, ChosenIndices, 0, chosenIndices.Length);

            ChosenProjectionPoints = new MapMatcherPoint[ProjectionPoints.Length];
            for (var i = 0; i < ProjectionPoints.Length; i++)
            {
                ChosenProjectionPoints[i] = ProjectionPoints[i][ChosenIndices[i]];
            }
        }
        
        /// <summary>
        /// The source track.
        /// </summary>
        public Track Source { get; }
        
        /// <summary>
        /// The route.
        /// </summary>
        public Route Route { get; }

        /// <summary>
        /// Outermost array: per source point.
        /// Innermost array: per possible projection.
        /// </summary>
        public MapMatcherPoint[][] ProjectionPoints { get; }
        
        /// <summary>
        /// The indices of the chosen points.
        /// </summary>
        public int[] ChosenIndices { get; }
        
        /// <summary>
        /// The chosen projection points.
        /// </summary>
        public MapMatcherPoint[] ChosenProjectionPoints { get; }

        /// <summary>
        /// Gives lines that connect the original points with their chosen projection on the road network.
        /// </summary>
        public IEnumerable<Line> ChosenProjectionLines()
        {
            var lines = new Line[Source.Count];
            for (var i = 0; i < Source.Count; i++)
            {
                lines[i] = new Line(Source[i].Location, ChosenProjectionPoints[i].Coordinate);
            }
            return lines;
        }

        /// <summary>
        /// Gives lines that connect the original points with all possible projections on the road network, and also the probability assigned to each.
        /// </summary>
        public List<(Line line, float probability, bool chosen)> AllProjectionLines()
        {
            var lines = new List<(Line, float, bool)>();
            for (var i = 0; i < Source.Count; i++)
            {
                var point = Source[i];

                for (var j = 0; j < ProjectionPoints[i].Length; j++)
                {
                    var projection = ProjectionPoints[i][j];
                    var chosen = j == ChosenIndices[i];
                    lines.Add((new Line(point.Location, projection.Coordinate), projection.Probability, chosen));
                }
            }
            return lines;
        }
        
        // TODO: move this somewhere else?

        /// <summary>
        /// Deep clone for 2D arrays
        /// </summary>
        private static T[][] TwoDClone<T>(IReadOnlyList<T[]> array)
        {
            var clone = new T[array.Count][];
            for (var i = 0; i < array.Count; i++)
            {
                clone[i] = new T[array[i].Length];
                Array.Copy(array[i], 0, clone[i], 0, array[i].Length);
            }
            return clone;
        }
    }
}
