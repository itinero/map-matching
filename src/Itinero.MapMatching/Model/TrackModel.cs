using System.Collections.Generic;
using Itinero.Graphs;
using Itinero.Graphs.Directed;

namespace Itinero.MapMatching.Model
{
    /// <summary>
    /// Represents the track model.
    /// </summary>
    /// <remarks>
    /// This is the HMM equivalent from the original paper this is based on. This has been changed/enhanced significantly because we focus on quality not quantity.
    /// </remarks>
    internal class TrackModel
    {
        private readonly List<(int track, int point, float prob)> _vertices = new List<(int track, int point, float prob)>(); 
        private readonly DirectedGraph _graph = new DirectedGraph(1);
        private readonly float _uTurnCost;
        
        // HOW IS THIS NETWORK FORMED:
        // 1 start vertex: '0'.
        // 2 end vertex  : '1'.
        // the model van be solved by finding a shortest path from 0-...->1.
        
        // locations:
        // each location has a (track, point) pair identifying the location
        // each location is split into three vertices:
        // AF: the forward arrival vertex.
        // AB: the backward arrival vertex.
        // DF: the forward departure vertex.
        // DB: the backward departure vertex.
        // the vertices are linked by:
        // AF->DF & AB->DB: the weight is the resolve probability.
        // AF->DB & DB->AF: the weight is the u-turn cost plus the resolve probability. 

        /// <summary>
        /// Creates a new track model.
        /// </summary>
        public TrackModel(float uTurnCost = 1F)
        {
            _uTurnCost = uTurnCost;
            
            // add the start vertex.
            _vertices.Add((-1, -1, 1));
            
            // add end vertex.
            _vertices.Add((-1, -1, 1));
        }

        private static uint ToEdgeWeight(float cost)
        {
            return (uint) (cost * 1000000);
        }

        internal static float FromEdgeWeight(uint weight)
        {
            return (weight / 1000000f);
        }

        private static uint ToVertex(int id, bool arrival, bool forward)
        {
            if (arrival)
            {
                if (forward)
                {
                    return (uint) ((id * 4) + 0);
                }

                return (uint) ((id * 4) + 1);
            }
            if (forward)
            {
                return (uint) ((id * 4) + 2);
            }

            return (uint) ((id * 4) + 3);
        }
        
        /// <summary>
        /// Adds a new location.
        /// </summary>
        /// <param name="location">The location identified by a track index and a resolved point.</param>
        /// <param name="prob">The cost associated with this location.</param>
        /// <returns>The location id.</returns>
        public int AddLocation((int trackIndex, int pointIndex) location, float prob)
        {
            var id = _vertices.Count;
            _vertices.Add((location.trackIndex, location.pointIndex, prob));
            
            // add one that represent forward, one that represents backward and one that links the resolve probabilities.
            var af = ToVertex(id, true, true);
            var ab = ToVertex(id, true, false);
            var df = ToVertex(id, false, true);
            var db = ToVertex(id, false, false);
            
            // add resolve probabilities.
            _graph.AddEdge(af, df, ToEdgeWeight(prob));
            _graph.AddEdge(ab, db, ToEdgeWeight(prob));
            
            // add u-turns.
            _graph.AddEdge(af, db, ToEdgeWeight(_uTurnCost + prob));
            _graph.AddEdge(ab, df, ToEdgeWeight(_uTurnCost + prob));

            return id;
        }

        /// <summary>
        /// Links the start and the end vertex to the rest of the network.
        /// </summary>
        public void Close()
        {
            var l = 2;
            var track = _vertices[l].track;
            while (_vertices[l].track == track)
            {
                _graph.AddEdge(0, ToVertex(l, true, true), 0);
                _graph.AddEdge(0, ToVertex(l, true, false), 0);
                l++;
            }
            
            l = _vertices.Count - 1;
            track = _vertices[l].track;
            while (_vertices[l].track == track)
            {
                _graph.AddEdge(ToVertex(l, false, true), 1,0);
                _graph.AddEdge(ToVertex(l, false, false), 1,0);
                l--;
            }
        }

        /// <summary>
        /// Adds a new transit, a move from one location to another. 
        /// </summary>
        /// <param name="location1"></param>
        /// <param name="location2"></param>
        /// <param name="forward1"></param>
        /// <param name="forward2"></param>
        /// <param name="cost">The cost of the transit.</param>
        public void AddTransit(int location1, int location2,
            bool forward1, bool forward2, float cost)
        {
            var v1 = ToVertex(location1, false, forward1);
            var v2 = ToVertex(location2, true, forward2);

            _graph.AddEdge(v1, v2, ToEdgeWeight(cost));
        }

        internal uint Start => 0;

        internal uint End => 1;

        internal DirectedGraph.EdgeEnumerator Enumerator => _graph.GetEdgeEnumerator();

        internal (int track, int point, bool forward) GetVertexDetails(uint vertex)
        {
            var id = vertex / 4;
            var offset = (vertex - (id * 4));

            var (track, point, _) = _vertices[(int)id];
            return (track, point, offset == 0 || offset == 2);
        }
    }
}