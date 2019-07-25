using Itinero.LocalGeo;
using System;
using System.Collections.Generic;

namespace Itinero.MapMatching
{
    public struct MapMatcherPoint
    {
        public RouterPoint RPoint { get; }
        public Coordinate Coord { get; }
        public float Probability { get; }

        public MapMatcherPoint(RouterPoint routerPoint, Coordinate coord, float probability)
        {
            RPoint = routerPoint;
            Coord = coord;
            Probability = probability;
        }
    }

    public class MapMatcherResult
    {
        public Track Source { get; }
        public Route Route { get; }

        /// <summary>Outermost array: per source point. Innermost array: per possible
        /// projection.</summary>
        public MapMatcherPoint[][] ProjectionPoints { get; }
        public int[] ChosenIndices { get; }
        public MapMatcherPoint[] ChosenProjectionPoints { get; }


        public MapMatcherResult(
                Track source,
                MapMatcherPoint[][] projectionPoints,
                int[] chosenIndices,
                Route route)
        {
            Source = source;

            Route = route;
            ProjectionPoints = TwoDClone(projectionPoints);
            ChosenIndices = new int[chosenIndices.Length];
            Array.Copy(chosenIndices, 0, ChosenIndices, 0, chosenIndices.Length);

            ChosenProjectionPoints = new MapMatcherPoint[ProjectionPoints.Length];
            for (int i = 0; i < ProjectionPoints.Length; i++)
            {
                ChosenProjectionPoints[i] = ProjectionPoints[i][ChosenIndices[i]];
            }
        }


        /// <summary>Gives lines that connect the original points with their chosen projection on
        /// the road network</summary>
        public Line[] ChosenProjectionLines()
        {
            var lines = new Line[Source.Points.Count];
            for (int i = 0; i < Source.Points.Count; i++)
            {
                lines[i] = new Line(Source.Points[i].Coord, ChosenProjectionPoints[i].Coord);
            }
            return lines;
        }

        /// <summary>Gives lines that connect the original points with all possible projections on
        /// the road network, and also the probability assigned to each</summary>
        public List<(Line line, float probability, bool chosen)> AllProjectionLines()
        {
            var lines = new List<(Line, float, bool)>();
            for (int i = 0; i < Source.Points.Count; i++)
            {
                var point = Source.Points[i];

                for (int j = 0; j < ProjectionPoints[i].Length; j++)
                {
                    var projection = ProjectionPoints[i][j];
                    bool chosen = j == ChosenIndices[i];
                    lines.Add((new Line(point.Coord, projection.Coord), projection.Probability, chosen));
                }
            }
            return lines;
        }


        /// <summary>Deep clone for 2D arrays</summary>
        private T[][] TwoDClone<T>(T[][] array)
        {
            var clone = new T[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                clone[i] = new T[array[i].Length];
                Array.Copy(array[i], 0, clone[i], 0, array[i].Length);
            }
            return clone;
        }
    }
}
