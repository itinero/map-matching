using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Itinero.Geo;
using Itinero.Network;
using Itinero.Profiles;
using Itinero.Routing;
using Itinero.Snapping;

namespace Itinero.MapMatching.Model;

public class ModelBuilder
{
    private readonly RoutingNetwork _routingNetwork;

    public ModelBuilder(RoutingNetwork routingNetwork)
    {
        _routingNetwork = routingNetwork;
    }

    /// <summary>
    /// Builds one or more graph models for the given track.
    /// </summary>
    /// <param name="track">The track.</param>
    /// <param name="profile">The profile to match with.</param>
    /// <returns>One or more graph models with probabilities as costs.</returns>
    public async Task<IEnumerable<GraphModel>> BuildModels(Track track, Profile profile)
    {
        var models = new List<GraphModel>();
        
        var start = 0;
        while (start < track.Count - 1)
        {
            var (model, lastUsed) = await this.BuildModel(track, profile, start);

            if (lastUsed == start)
            {
                // nothing consumed, move next.
                start++;
                continue;
            }
            
            models.Add(model);
            start = lastUsed;
        }

        return models;
    }

    private async Task<(GraphModel model, int index)> BuildModel(Track track, Profile profile, int start)
    {
        var model = new GraphModel(track);

        // for these parameter see paper in the repo.

        // to normalize the edge costs.
        const double t = 5.0;
        // to normalize the node costs. the default search radius in meter to identity candidate snappings.
        const int d = 50;
        // ratio between node costs and transition costs
        // 1 = only transition and 0 only node weights.
        const double alpha = 0.5;

        const double beta = alpha / (1 - alpha) * (d / t);

        var previousLayer = new List<int>();
        var startNode = new GraphNode();
        previousLayer.Add(model.AddNode(startNode));

        var lastPoint = start;
        for (var i = start; i < track.Count; i++)
        {
            var trackPoint = track[i];
            var trackPointLocation = (trackPoint.Location.longitude, trackPoint.Location.latitude, (float?)null);
            var trackPointLayer = new List<int>();

            var isConnected = false;
            await foreach (var snapPoint in _routingNetwork.Snap().Using(profile, s =>
                               {
                                   s.OffsetInMeter = 1000;
                                   s.OffsetInMeterMax = 2000;
                               })
                               .ToAllAsync(trackPointLocation))
            {
                var cost = trackPointLocation.DistanceEstimateInMeter(snapPoint.LocationOnNetwork(_routingNetwork));
                if (cost > d) continue;

                // add node.
                var node = new GraphNode() { TrackPoint = i, SnapPoint = snapPoint, Cost = cost };
                var nodeId = model.AddNode(node);

                var nodeIsConnected = false;

                var distance = 0.0;
                if (i > 0)
                {
                    var previousTrackPoint = track[i - 1];
                    var previousTrackPointLocation = (previousTrackPoint.Location.longitude,
                        previousTrackPoint.Location.latitude, (float?)null);
                    distance = previousTrackPointLocation.DistanceEstimateInMeter(trackPointLocation);
                }

                // add edges from previous layer.
                foreach (var previousNode in previousLayer)
                {
                    var previousSnapPoint = model.GetNode(previousNode).SnapPoint;
                    var hopCost = 0.0;
                    var attributes = new List<(string key, string value)>();
                    if (previousSnapPoint != null)
                    {
                        double? routeDistance = 0.0;

                        var c = 0.0;
                        if (previousSnapPoint.Value.EdgeId == snapPoint.EdgeId &&
                            previousSnapPoint.Value.Offset == snapPoint.Offset)
                        {
                            c = 0;
                        }
                        else
                        {
                            routeDistance = await this.RouteDistanceAsync(previousSnapPoint.Value,
                                snapPoint, profile, System.Math.Min(distance * t, 100));
                            if (routeDistance == null) continue;

                            c = routeDistance.Value / distance;
                        }

                        if (c > t) continue;

                        attributes.Add(("distance", distance.ToString(CultureInfo.InvariantCulture)));
                        attributes.Add(("c", c.ToString(CultureInfo.InvariantCulture)));
                        attributes.Add(("route_distance", routeDistance.Value.ToString(CultureInfo.InvariantCulture)));

                        hopCost = c * beta;
                    }

                    model.AddEdge(new GraphEdge()
                    {
                        Node1 = previousNode,
                        Node2 = nodeId,
                        Cost = hopCost,
                        Attributes = attributes
                    });
                    isConnected = true;
                    nodeIsConnected = true;
                }

                if (nodeIsConnected)
                {
                    trackPointLayer.Add(nodeId);
                }
            }

            // the previous layer is now the current layer.
            if (!isConnected)
            {
                break;
            }

            previousLayer = trackPointLayer;
            lastPoint = i;
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

        return (model, lastPoint);
    }

    private async Task<double?> RouteDistanceAsync(SnapPoint snapPoint1, SnapPoint snapPoint2, Profile profile,
        double maxDistance)
    {
        var route = await _routingNetwork.Route(new RoutingSettings() { MaxDistance = maxDistance, Profile = profile })
            .From(snapPoint1).To(snapPoint2).CalculateAsync();
        if (route.IsError) return null;

        return route.Value.TotalDistance;
    }
}
