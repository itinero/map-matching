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
        public static IEnumerable<IReadOnlyList<(int track, int point, bool arrival, bool departure)>> BestPaths(this TrackModel model)
        {
            while (true)
            {
                var bestPath = model.BestPath();
                if (bestPath.path != null) yield return bestPath.path;
                
                if (bestPath.track == -1) break;
                
                model.Close(bestPath.track + 1);
            }
        }
        
        
        public static (IReadOnlyList<(int track, int point, bool arrival, bool departure)> path, int track) BestPath(this TrackModel model)
        {
            // search the shortest path from the start to end.
            var enumerator = model.Enumerator;
            
            var heap = new BinaryHeap<EdgePath<float>>(1024);
            heap.Push(new EdgePath<float>(model.Start), 0);
            var settled = new HashSet<uint>();

            (EdgePath<float> path, int track) best = (null, -1);

            while (heap.Count > 0)
            {
                // pop the latest from queue.
                var settle = heap.Pop();
                if (settled.Contains(settle.Vertex)) continue;
                settled.Add(settle.Vertex);

                // check if we've reached the end of the model.
                if (settle.Vertex == model.End)
                {
                    best = (settle, -1);
                    break;
                }

                // keep the best current solution, the last track point with the lowest weight.
                var vertexDetails = model.GetVertexDetails(settle.Vertex);
                if (best.track < vertexDetails.track)
                {
                    best = (settle, vertexDetails.track);
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
            
            // if there is no best, no path was found.
            if (best.path == null) return (null, -1);

            if (best.track != -1)
            {
                Console.WriteLine("Not last!");    
            }
            
            // build the result from the best path.
            var path = best.path.ToListAsVertices();
            var result = new List<(int track, int point, bool arrival, bool departure)>(path.Count - 2);
            for (var i = 1; i < path.Count - 1; i++)
            {
                var details = model.GetVertexDetails(path[i]);

                if (result.Count > 0 &&
                    result[result.Count - 1].track == details.track)
                { // this is probably a u-turn.
                    result[result.Count - 1] = (result[result.Count - 1].track, result[result.Count - 1].point,
                        result[result.Count - 1].arrival, details.forward);
                    continue;
                }

                result.Add((details.track, details.point, details.forward, details.forward));
            }

            return (result, best.track);
        }
    }
}