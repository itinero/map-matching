using System;
using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Algorithms.PriorityQueues;

namespace Itinero.MapMatching.Model
{
    internal static class TrackModelRouting
    {
        /// <summary>
        /// Calculates the best path through the track model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The track with the lowest cost.</returns>
        public static IReadOnlyList<(int track, int point, bool arrival, bool departure)> BestPath(this TrackModel model)
        {
            // search the shortest path from the start to end.
            var enumerator = model.Enumerator;
            
            var heap = new BinaryHeap<EdgePath<float>>(1024);
            heap.Push(new EdgePath<float>(model.Start), 0);

            while (heap.Count > 0)
            {
                var settle = heap.Pop();

                if (settle.Vertex == model.End)
                {
                    var vertices = settle.ToListAsVertices();

                    var result = new List<(int track, int point, bool arrival, bool departure)>(vertices.Count);

                    for (var i = 1; i < vertices.Count - 1; i++)
                    {
                        var details = model.GetVertexDetails(vertices[i]);

                        if (result.Count > 0 &&
                            result[result.Count - 1].track == details.track)
                        { // this is probably a u-turn.
                            result[result.Count - 1] = (result[result.Count - 1].track, result[result.Count - 1].point,
                                result[result.Count - 1].arrival, details.forward);
                            continue;
                        }

                        result.Add((details.track, details.point, details.forward, details.forward));
                    }

                    return result;
                }
                
                if (!enumerator.MoveTo(settle.Vertex)) continue;

                while (enumerator.MoveNext())
                {
                    var neighbour = enumerator.Neighbour;
                    var weight = TrackModel.FromEdgeWeight(enumerator.Data0);

                    var pathToNeighbour = new EdgePath<float>(neighbour, weight + settle.Weight,
                        settle);
                    
                    heap.Push(pathToNeighbour, pathToNeighbour.Weight);
                }
            }
            
            // model could not be solved, end vertex was not settled.
            return null;
        }
    }
}