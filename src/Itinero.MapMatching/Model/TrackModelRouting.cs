using System;
using System.Collections.Generic;
using System.Linq;

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
            
            var pathTree = new PathTree();
            var heap = new BinaryHeap<uint>(1024);
            heap.Push(pathTree.AddVisit(model.Start, uint.MaxValue), 0);
            var settled = new HashSet<uint>();

            while (heap.Count > 0)
            {
                var settle = heap.Pop(out var settleWeight);
                var visit = pathTree.GetVisit(settle);
                if (settled.Contains(visit.vertex)) continue;
                settled.Add(visit.vertex);

                if (visit.vertex == model.End)
                {
                    var vertices = pathTree.GetVisits(settle).ToList();
                    vertices.Reverse();

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
                
                enumerator.MoveTo(visit.vertex);

                while (enumerator.MoveNext())
                {
                    var neighbour = enumerator.To;
                    if (settled.Contains(neighbour)) continue;
                    
                    var weight = TrackModel.FromEdgeWeight(enumerator.Data);

                    var neighbourVisit = pathTree.AddVisit(neighbour, settle);
                    
                    heap.Push(neighbourVisit, weight + settleWeight);
                }
            }
            
            // model could not be solved, end vertex was not settled.
            return Array.Empty<(int track, int point, bool arrival, bool departure)>();
        }
    }
}