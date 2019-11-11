using System;
using System.Collections.Generic;
using System.Text;
using Itinero.Algorithms;
using Itinero.Data.Network;
using Itinero.MapMatching.Routing;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Represents a map matched route, a sequence of adjacent edges with a start and end offset.
    /// </summary>
    public class MapMatchPath
    {
        private readonly IReadOnlyList<DirectedEdgeId> _edges;

        internal MapMatchPath(IReadOnlyList<DirectedEdgeId> edges, ushort startOffset, ushort endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
            _edges = edges;
        }

        /// <summary>
        /// Gets the edge at the given index.
        /// </summary>
        /// <param name="i">The index</param>
        public DirectedEdgeId this[int i] => _edges[i];

        /// <summary>
        /// Gets the number of edges.
        /// </summary>
        public int Count => _edges.Count;

        /// <summary>
        /// Gets the start offset.
        /// </summary>
        public ushort StartOffset { get; }

        /// <summary>
        /// Gets the end offset.
        /// </summary>
        public ushort EndOffset { get; }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            var text = new StringBuilder();
            text.Append($"{this.StartOffset/(double)ushort.MaxValue*100:F2}%@");
            for (var i = 0; i < this.Count; i++)
            {
                text.Append($"{this[i]}");
                if (i < this.Count - 1)
                {
                    text.Append("->");
                }
            }
            text.Append($"@{this.EndOffset/(double)ushort.MaxValue*100:F2}%");
            return text.ToString();
        }
    }
    
    /// <summary>
    /// Contains extension methods for the map matched route.
    /// </summary>
    public static class MapMatchedRouteExtensions
    {
        internal static MapMatchPath ToMapMatchPath(this RouterDb routerDb, EdgePath<float> edgePath,
            RouterPoint from, RouterPoint to)
        {
            if (routerDb == null) throw new ArgumentNullException(nameof(routerDb));
            if (edgePath == null) throw new ArgumentNullException(nameof(edgePath));
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            
            // build the edges list.
            var edges = new List<DirectedEdgeId>();
            var endVertex = edgePath.Vertex;
            var originalPath = edgePath;
            while (true)
            {
                if (edgePath.From  == null) break;
                
                edges.Insert(0, new DirectedEdgeId(edgePath.Edge));
                edgePath = edgePath.From;
            }

            // check the start and get the offset.
            var startVertex = edgePath.Vertex;
            var start = from.Offset;
            if (startVertex != Itinero.Constants.NO_VERTEX &&
                from.IsVertex(routerDb, startVertex))
            {
                start = 0;
                if (!edges[0].Forward)
                {
                    start = ushort.MaxValue;
                }
            }
            else
            {
                if (edges[0].EdgeId != from.EdgeId) throw new ArgumentException($"Cannot construct a {nameof(MapMatchPath)}: " +
                                                                                $"First edge does not match edge in from {nameof(RouterPoint)}");
                if (!edges[0].Forward) start = (ushort) (ushort.MaxValue - start);
            }
            
            // check the end and get the offset.
            var end = to.Offset;
            if (endVertex != Itinero.Constants.NO_VERTEX &&
                to.IsVertex(routerDb, endVertex))
            { // the end matched the vertex.
                end = ushort.MaxValue;
                if (!edges[edges.Count - 1].Forward)
                {
                    end = 0;
                }
            }
            else
            { // the end is a point on an edge.
                if (edges[edges.Count - 1].EdgeId != to.EdgeId) throw new ArgumentException($"Cannot construct a {nameof(MapMatchPath)}: " +
                                                                                            $"Last edge does not match edge in to {nameof(RouterPoint)}");
                if (!edges[edges.Count - 1].Forward) end = (ushort) (ushort.MaxValue - end);
            }

            return new MapMatchPath(edges, start, end);
        }

        internal static EdgePath<float> ToEdgePath(this RouterDb routerDb, MapMatchPath path)
        {
            if (routerDb == null) throw new ArgumentNullException(nameof(routerDb));
            if (path == null) throw new ArgumentNullException(nameof(path));
            
            var enumerator = routerDb.Network.GetEdgeEnumerator();
            
            if (path.Count < 1) throw new ArgumentException($"Cannot build {nameof(EdgePath<float>)}: {nameof(path)} has no edges.");
            if (path.Count == 1)
            {
                return new EdgePath<float>(Constants.NO_VERTEX, float.MaxValue, 
                    path[0].EdgeId, new EdgePath<float>(Constants.NO_VERTEX));
            }

            var edge = path[0];
            enumerator.MoveToEdge(edge.SignedDirectedId);
            var edgePath = new EdgePath<float>(edge.Forward ? enumerator.To : enumerator.From, float.MaxValue, edge.SignedDirectedId, 
                new EdgePath<float>(Constants.NO_VERTEX));
            for (var i = 1; i < path.Count; i++)
            {
                edge = path[i];

                if (i == path.Count - 1)
                {
                    edgePath = new EdgePath<float>(Constants.NO_VERTEX, float.MaxValue, edge.SignedDirectedId, 
                        edgePath);
                    break;
                }
                enumerator.MoveToEdge(edge.SignedDirectedId);
                edgePath = new EdgePath<float>(edge.Forward ? enumerator.To : enumerator.From, float.MaxValue, edge.SignedDirectedId, 
                    edgePath);
            }

            return edgePath;
        }

        internal static MapMatchPath Merge(this MapMatchPath path1, MapMatchPath path2)
        {
            var edges = new List<DirectedEdgeId>();

            // add edges from path1.
            for (var i = 0; i < path1.Count; i++)
            {
                edges.Add(path1[i]);
            }

            // add first edge from path2 but only if different from last of path1.
            if (edges[edges.Count - 1].EdgeId != path2[0].EdgeId)
            {
                edges.Add(path2[0]);
            }
            
            // add edges from path2.
            for (var i = 1; i < path2.Count; i++)
            {
                edges.Add(path2[i]);
            }
            
            return new MapMatchPath(edges, path1.StartOffset, path2.EndOffset);
        }

        internal static MapMatchPath Merge(this IReadOnlyList<MapMatchPath> paths)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            if (paths.Count == 0)
                throw new ArgumentException($"Cannot merge:{nameof(paths)} empty.");
            if (paths.Count == 1) return paths[0];

            var path = paths[0].Merge(paths[1]);
            for (var i = 2; i < paths.Count; i++)
            {
                path = path.Merge(paths[i]);
            }

            return path;
        }
    }
}