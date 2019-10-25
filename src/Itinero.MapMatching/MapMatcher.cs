using System;
using System.Collections.Generic;
using Itinero.Algorithms.Search;
using Itinero.LocalGeo;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Algorithms;
using Itinero.MapMatching.Model;
using Itinero.MapMatching.Routing;
using Itinero.Profiles;

[assembly: InternalsVisibleTo("Itinero.MapMatching.Test")]
[assembly: InternalsVisibleTo("Itinero.MapMatching.Test.Functional")]
namespace Itinero.MapMatching
{
    internal class MapMatcher
    {
        private readonly Router _router;
        private readonly Profile _profile;
        private readonly MapMatcherSettings _settings;

        public MapMatcher(Router router, MapMatcherSettings settings = null)
        {
            if (settings == null) settings = MapMatcherSettings.Default;
            _settings = settings;
            
            var profile = router.Db.GetSupportedProfile(settings.Profile);
            if (profile.Metric != ProfileMetric.DistanceInMeters) { throw new ArgumentException(
                $"Only profiles supported with distance as metric.", $"{nameof(profile)}"); }
            
            _router = router;
            _profile = profile;
        }

        public Result<MapMatcherResult> TryMatch(Track track)
        {
            // build track model.
            var trackModel = new TrackModel();
            
            // build location (vertices) of the model.
            var locations = new List<(List<(RouterPoint rp, int modelId)> points, int trackIdx)>(track.Count);
            var isAcceptable = _router.GetIsAcceptable(_profile);
            var maxDistance = _settings.SnappingMaxDistance;
            for (var trackIdx = 0; trackIdx < track.Count; trackIdx++)
            {
                // resolve to multiple possible locations.
                var resolve = new ResolveMultipleAlgorithm(
                    _router.Db.Network.GeometricGraph,
                    track[trackIdx].Location.Latitude, track[trackIdx].Location.Longitude,
                    Offset(track[trackIdx].Location, _router.Db.Network), maxDistance,
                    isAcceptable, /* allow non-orthogonal projections */ true);
                resolve.Run();

                var points = resolve.HasSucceeded ? resolve.Results : new List<RouterPoint>();

                if (points.Count <= 0) continue; // no points found!
                
                var pointAndModel = new List<(RouterPoint rp, int modelId)>(points.Count);
                // add each one as a vertex.
                for (var p = 0; p < points.Count; p++)
                {
                    var prob = ResolveProbability(points[p]);

                    var modelId = trackModel.AddLocation((trackIdx, p), prob);
                        
                    pointAndModel.Add((points[p], modelId));
                }
                    
                locations.Add((pointAndModel, trackIdx));
            }
            
            // close the model, as in 'no more vertices'.
            trackModel.Close();
            
            // build transit probabilities.
            for (var trackIdx = 1; trackIdx < locations.Count; trackIdx++)
            {
                var sourceL = locations[trackIdx - 1];
                var targetL = locations[trackIdx - 0];

                var distance = Coordinate.DistanceEstimateInMeter(
                    track[sourceL.trackIdx].Location,
                    track[targetL.trackIdx].Location);
                var maxSearchDistance = distance * _settings.RouteDistanceFactor;
                if (maxSearchDistance < _settings.MinRouteDistance) maxSearchDistance = _settings.MinRouteDistance;

                var sources = locations[trackIdx - 1].points.Select(x => x.rp).ToList();
                var targets = locations[trackIdx - 0].points.Select(x => x.rp).ToList();

                var weights = _router.CalculateManyToManyDirected(_profile, sources,
                    targets, maxSearchDistance);

                var hasRoute = false;
                for (var s = 0; s < sources.Count; s++)
                for (var t = 0; t < targets.Count; t++)
                {
                    var sourceModelId = locations[trackIdx - 1].points[s].modelId;
                    var targetModelId = locations[trackIdx - 0].points[t].modelId;

                    var w = weights[s * 2 + 0][t * 2 + 0];
                    if (w < float.MaxValue)
                    {
                        hasRoute = true;
                        w = RouteProbability(w, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, true, true, w);
                    }

                    w = weights[s * 2 + 0][t * 2 + 1];
                    if (w < float.MaxValue)
                    {
                        hasRoute = true;
                        w = RouteProbability(w, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, true, false, w);
                    }

                    w = weights[s * 2 + 1][t * 2 + 0];
                    if (w < float.MaxValue)
                    {
                        hasRoute = true;
                        w = RouteProbability(w, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, false, true, w);
                    }

                    w = weights[s * 2 + 1][t * 2 + 1];
                    if (w < float.MaxValue)
                    {
                        hasRoute = true;
                        w = RouteProbability(w, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, false, false, w);
                    }
                }

                if (!hasRoute)
                {
                    return new Result<MapMatcherResult>(
                        $"Could not find a path between {sourceL.trackIdx} and {targetL.trackIdx}");
                }
            }

            // calculate the most efficient 'route' through the model.
            var bestMatch = trackModel.BestPath();
            if (bestMatch == null) return new Result<MapMatcherResult>("Could not match the track.");
            
            // build the paths.
            var weightHandler = _router.GetDefaultWeightHandler(_profile);
            var settings = new RoutingSettings<float>()
            {
                DirectionAbsolute = true,
            };

            var rawPaths = new List<EdgePath<float>>();
            var chosenPoints = new List<RouterPoint>();
            for (var l = 1; l < bestMatch.Count; l++)
            {
                var source = bestMatch[l - 1];
                var sourceRp = locations[source.track].points[source.point].rp;
                var target = bestMatch[l - 0];
                var targetRp = locations[target.track].points[target.point].rp;
                
                var maxSearchDistance = Coordinate.DistanceEstimateInMeter(
                                            sourceRp.Location(),
                                            targetRp.Location()) * _settings.RouteDistanceFactor;
                settings.SetMaxSearch(_profile.FullName, maxSearchDistance);
                if (chosenPoints.Count == 0)
                {
                    chosenPoints.Add(sourceRp);
                }
                chosenPoints.Add(targetRp);
                
                var rawPath = _router.TryCalculateRaw(_profile, weightHandler, sourceRp, source.departure,
                    targetRp, target.arrival, settings);
                if (rawPath.IsError)
                {
                    throw new Exception("Raw path calculation failed, it shouldn't fail at this point because it succeeded on the same path before.");
                }
                rawPaths.Add(rawPath.Value);
            }

            return new Result<MapMatcherResult>(
                new MapMatcherResult(_router.Db, track, _profile, chosenPoints.ToArray(), rawPaths));
        }
        
        private double GPSMeasurementProbability(float distance)
        {
            // TODO this is estimated in the paper, do we do the same here based on the data.
            // now we just use the max snapping distance.
            double measurementStandardDeviation = (_settings.SnappingMaxDistance / 5);

            var factor = 1; // (Math.Sqrt(2 * Math.PI) * measurementStandardDeviation);

            var exp = Math.Exp(-0.5 * System.Math.Pow(distance / measurementStandardDeviation, 2));
            
            return factor * exp;
        }

        private float ResolveProbability(RouterPoint resolvedPoint)
        {
            var origLoc = resolvedPoint.Location();
            var resolvedLoc = resolvedPoint.LocationOnNetwork(_router.Db);
            
            var greatCircle = System.Math.Abs(Coordinate.DistanceEstimateInMeter(origLoc, resolvedLoc));

            var prob = (float)GPSMeasurementProbability(greatCircle);
            //Console.WriteLine($"{greatCircle}: {prob} ({1-prob})");
            return 1 - prob;
        }

        private double TransitionProbability(float routeDistance, float locationDistance)
        {
            // TODO this is estimated in the paper, do we do the same here based on the data.
            // now we just use the max snapping distance.
            var beta = _settings.SnappingMaxDistance; 

            var factor = 1;// / beta;

            var exp = Math.Exp(-System.Math.Abs(routeDistance - locationDistance) / beta);

            return factor * exp;
        }

        private float RouteProbability(float routeDistance, float locationDistance, float maxDistance)
        {
            // TODO: improve, for now too simple.
            if (System.Math.Abs(routeDistance) > locationDistance * _settings.RouteDistanceFactor)
            {
                return 1;
            }
            var prob = (float)TransitionProbability(routeDistance, locationDistance);
            //Console.WriteLine($"{routeDistance},{locationDistance} -> {System.Math.Abs(routeDistance - locationDistance)}: {prob} ({1-prob})");
            return 1-prob;
//            return (((System.Math.Abs(routeDistance - locationDistance) / maxDistance)));
        }

        static float Offset(Coordinate centerLocation, Itinero.Data.Network.RoutingNetwork routingNetwork)
        {
            var offsetLocation = centerLocation.OffsetWithDistances(routingNetwork.MaxEdgeDistance / 2);
            var latitudeOffset = System.Math.Abs(centerLocation.Latitude - offsetLocation.Latitude);
            var longitudeOffset = System.Math.Abs(centerLocation.Longitude - offsetLocation.Longitude);

            return System.Math.Max(latitudeOffset, longitudeOffset);
        }
    }
}
