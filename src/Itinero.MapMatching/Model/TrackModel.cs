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

        private static uint LocationIdToVertex(int id, bool forward)
        {
            if (forward)
            {
                return (uint) ((id * 2) + 0);
            }

            return (uint) ((id * 2) + 1);
        }
        
        /// <summary>
        /// Adds a new location.
        /// </summary>
        /// <param name="location">The location identified by a track index and a resolved point.</param>
        /// <param name="cost">The cost associated with this location.</param>
        /// <returns>The location id.</returns>
        public int AddLocation((int track, int point) location, float cost)
        {
            var id = _vertices.Count;
            _vertices.Add((location.track, location.point, cost));
            
            // add one that represent forward, one that represents backward.
            var vf = LocationIdToVertex(id, true);
            var vb = LocationIdToVertex(id, false);
            
            // add u-turns.
            _graph.AddEdge(vf, vb, ToEdgeWeight(_uTurnCost));

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
                _graph.AddEdge(0, (uint) (l * 2 + 0), 0);
                _graph.AddEdge(0, (uint) (l * 2 + 1), 0);
                l++;
            }
            
            l = _vertices.Count - 1;
            track = _vertices[l].track;
            while (_vertices[l].track == track)
            {
                _graph.AddEdge( (uint) (l * 2 + 0), 1, 0);
                _graph.AddEdge((uint) (l * 2 + 1), 1,0);
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
            var v1 = LocationIdToVertex(location1, forward1);
            var v2 = LocationIdToVertex(location2, forward2);

            _graph.AddEdge(v1, v2, ToEdgeWeight(cost));
        }

        internal uint Start => 0;

        internal uint End => 1;

        internal DirectedGraph.EdgeEnumerator Enumerator => _graph.GetEdgeEnumerator();

        internal (int track, int point, bool forward) GetVertexDetails(uint vertex)
        {
            var id = vertex / 2;
            var offset = (vertex - (id * 2));

            var vDetails = _vertices[(int)id];
            return (vDetails.track, vDetails.point, offset == 0);
        }
    }
}