using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing;
using Itinero.Snapping;
using Reminiscence.Collections;

namespace Itinero.MapMatching.Model;

public class ModelBuilder
{
    private readonly RoutingNetwork _routingNetwork;

    public ModelBuilder(RoutingNetwork routingNetwork)
    {
        _routingNetwork = routingNetwork;
    }

    /// <summary>
    /// Builds a graph model for the given track.
    /// </summary>
    /// <param name="track">The track.</param>
    /// <returns>The graph model with probabilities as costs.</returns>
    public async Task<GraphModel> BuildModel(Track track, Profile profile)
    {
        var model = new GraphModel();

        // for these parameter see paper in the repo.

        // to normalize the edge costs.
        const double t = 5.0;
        // to normalize the node costs. the default search radius in meter to identity candidate snappings.
        const int d = 50;
        // ratio between node costs and transition costs
        // 1 = only transition and 0 only node weights.
        const double alpha = 0.65;

        const double beta = alpha / (1 - alpha) * (d / t);

        var previousLayer = new List<int>();
        var startNode = new GraphNode();
        previousLayer.Add(model.AddNode(startNode));

        for (var i = 0; i < track.Count; i++)
        {
            var trackPoint = track[i];
            var trackPointLocation = (trackPoint.Location.longitude, trackPoint.Location.latitude, (float?)null);
            var trackPointLayer = new List<int>();

            await foreach (var snapPoint in _routingNetwork.Snap()
                               .ToAllAsync(trackPointLocation))
            {
                var cost = trackPointLocation.DistanceEstimateInMeter(snapPoint.LocationOnNetwork(_routingNetwork));

                // add node.
                var node = new GraphNode()
                {
                    TrackPoint = i,
                    SnapPoint = snapPoint,
                    Cost = cost
                };
                var nodeId = model.AddNode(node);
                trackPointLayer.Add(nodeId);

                // add edges from previous layer.
                foreach (var previousNode in previousLayer)
                {
                    var previousSnapPoint = model.GetNode(previousNode).SnapPoint;
                    var hopCost = 0.0;
                    if (previousSnapPoint != null)
                    {
                        var routeDistance = await this.RouteDistanceAsync(previousSnapPoint.Value,
                            snapPoint, profile);
                        if (routeDistance == null) continue;

                        var distance = this.Distance(previousSnapPoint.Value, snapPoint);

                        var c = routeDistance.Value / distance;
                        if (c > t) continue;

                        hopCost = c * beta;
                    }
                    model.AddEdge(new GraphEdge()
                    {
                        Node1 = previousNode,
                        Node2 = nodeId,
                        Cost = hopCost
                    });
                }
            }

            // the previous layer is now the current layer.
            previousLayer = trackPointLayer;
        }

        var endNode = new GraphNode();
        var endNodeId = model.AddNode(endNode);

        // add edges from previous layer to last node.
        foreach (var previousNode in previousLayer)
        {
            model.AddEdge(new GraphEdge()
            {
                Node1 = previousNode,
                Node2 = endNodeId,
                Cost = 0 // last edges have cost 0.
            });
        }

        return model;
    }

    private double Distance(SnapPoint snapPoint1, SnapPoint snapPoint2)
    {
        return snapPoint1.LocationOnNetwork(_routingNetwork).DistanceEstimateInMeter(
            snapPoint2.LocationOnNetwork(_routingNetwork));
    }

    private async Task<double?> RouteDistanceAsync(SnapPoint snapPoint1, SnapPoint snapPoint2, Profile profile)
    {
        var route = await _routingNetwork.Route(profile).From(snapPoint1).To(snapPoint2).CalculateAsync();
        if (route.IsError) return null;

        return route.Value.TotalDistance;
    }
}
