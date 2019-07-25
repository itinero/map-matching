using System.Collections;
using System.Collections.Generic;
using Itinero.Attributes;
using Itinero.Data.Network.Edges;
using Itinero.Graphs.Geometric.Shapes;
using Itinero.LocalGeo;

namespace Itinero.MapMatching.Test
{
    internal static class RouterDbExtensions
    {
        internal static uint AddVertex(this RouterDb routerDb, float latitude, float longitude)
        {
            var id = routerDb.Network.VertexCount;
            routerDb.Network.AddVertex(id, latitude, longitude);
            return id;
        }

        internal static uint AddEdge(this RouterDb routerDb, uint vertex1, uint vertex2, IAttributeCollection edgeProfileAttributes,
            IEnumerable<Coordinate> shape = null)
        {
            ShapeEnumerable shapeEnumerable = null;
            if (shape != null) shapeEnumerable = new ShapeEnumerable(shape);
            
            // calculate distance here.
            var distance = 0f;
            var previous = routerDb.Network.GetVertex(vertex1);
            if (shapeEnumerable != null)
            {
                foreach (var s in shapeEnumerable)
                {
                    distance += Coordinate.DistanceEstimateInMeter(previous, s);
                    previous = s;
                }
            }
            distance += Coordinate.DistanceEstimateInMeter(previous, routerDb.Network.GetVertex(vertex2));
            
            var profileId = routerDb.EdgeProfiles.Add(edgeProfileAttributes);
            var metaId = routerDb.EdgeMeta.Add(null);

            return routerDb.Network.AddEdge(vertex1, vertex2, new EdgeData()
            {
                Distance = distance,
                MetaId = metaId,
                Profile = (ushort) profileId
            }, shapeEnumerable);
        }
    }
}