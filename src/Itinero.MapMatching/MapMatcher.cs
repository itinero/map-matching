using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Algorithms;
using Itinero.Geo;
using Itinero.MapMatching.Model;
using Itinero.Profiles;
using Itinero.Routers;
using Path = Itinero.Algorithms.DataStructures.Path;

[assembly: InternalsVisibleTo("Itinero.MapMatching.Test")]
[assembly: InternalsVisibleTo("Itinero.MapMatching.Test.Functional")]
namespace Itinero.MapMatching
{
    internal class MapMatcher : IMapMatcher
    {
        private readonly RouterDb _routerDb;
        private readonly Profile _profile;
        private readonly MapMatcherSettings _settings;

        public MapMatcher(RouterDb routerDb, MapMatcherSettings settings = null)
        {
            settings ??= MapMatcherSettings.Default;
            
            _settings = settings;
            _routerDb = routerDb;
            _profile = settings.Profile;
        }
        
        public IMapMatcherSettings Settings => _settings;
        
        public RouterDb RouterDb => _routerDb;

        public Result<MapMatch> Match(Track track)
        {
            // build track model.
            var trackModel = new TrackModel();
            
            // build location (vertices) of the model.
            var locations = new List<(List<(SnapPoint rp, int modelId)> points, int trackIdx)>(track.Count);
            var snapPointSettings = new SnapPointSettings()
            {
                Profiles = new []{_profile},
                MaxOffsetInMeter = _settings.SnappingMaxDistance
            };
            for (var trackIdx = 0; trackIdx < track.Count; trackIdx++)
            {
                var location = track[trackIdx].Location;
                
                var snapPointsResult = _routerDb.SnapAll(track[trackIdx].Location, snapPointSettings);
                if (snapPointsResult.IsError) continue; // TODO: what to do with snapping fails here!

                var points = snapPointsResult.Value.ToList();
                if (points.Count <= 0) continue; // no points found!
                
                // add each resolved location as a vertex in the model.
                var pointAndModel = new List<(SnapPoint rp, int modelId)>(points.Count);
                foreach (var point in snapPointsResult.Value)
                {
                    var prob = ResolveProbability(location, point);

                    var pIndex = pointAndModel.Count;
                    var modelId = trackModel.AddLocation((trackIdx, pIndex), prob);
                        
                    pointAndModel.Add((point, modelId));
                }
                    
                locations.Add((pointAndModel, trackIdx));
            }
            
            // close the model, as in 'no more vertices'.
            trackModel.Close();
            
            // build transit probabilities.
            var router = _routerDb.Route(new RoutingSettings()
            {
                Profile = _settings.Profile,
                MaxDistance = _settings.RouteDistanceFactor
            });
            for (var trackIdx = 1; trackIdx < locations.Count; trackIdx++)
            {
                var sourceL = locations[trackIdx - 1];
                var targetL = locations[trackIdx - 0];

                var distance = track[sourceL.trackIdx].Location.DistanceEstimateInMeter(
                    track[targetL.trackIdx].Location);
                var maxSearchDistance = distance * _settings.RouteDistanceFactor;
                if (maxSearchDistance < _settings.MinRouteDistance) maxSearchDistance = _settings.MinRouteDistance;

                var sources = new List<(SnapPoint sp, bool? direction)>();
                foreach (var (sp, _) in locations[trackIdx - 1].points)
                {
                    sources.Add((sp, true));
                    sources.Add((sp, false));
                }
                var targets = new List<(SnapPoint sp, bool? direction)>();
                foreach (var (sp, _) in locations[trackIdx - 0].points)
                {
                    targets.Add((sp, true));
                    targets.Add((sp, false));
                }

                var weightResult = router.From(sources).To(targets).Weights().Calculate();
                if (weightResult.IsError) throw new Exception("Could not calculate weights.");

                var weights = weightResult.Value;
                var hasRoute = false;
                for (var s = 0; s < sources.Count / 2; s++)
                for (var t = 0; t < targets.Count / 2; t++)
                {
                    var sourceModelId = locations[trackIdx - 1].points[s].modelId;
                    var targetModelId = locations[trackIdx - 0].points[t].modelId;

                    var w = weights[s * 2 + 0][t * 2 + 0];
                    if (w != null)
                    {
                        hasRoute = true;
                        w = RouteProbability(w.Value, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, true, true, w.Value);
                    }

                    w = weights[s * 2 + 0][t * 2 + 1];
                    if (w != null)
                    {
                        hasRoute = true;
                        w = RouteProbability(w.Value, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, true, false, w.Value);
                    }

                    w = weights[s * 2 + 1][t * 2 + 0];
                    if (w != null)
                    {
                        hasRoute = true;
                        w = RouteProbability(w.Value, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, false, true, w.Value);
                    }

                    w = weights[s * 2 + 1][t * 2 + 1];
                    if (w != null)
                    {
                        hasRoute = true;
                        w = RouteProbability(w.Value, distance, maxSearchDistance);
                        trackModel.AddTransit(sourceModelId, targetModelId, false, false, w.Value);
                    }
                }

                if (!hasRoute)
                {
                    return new Result<MapMatch>(
                        $"Could not find a path between {sourceL.trackIdx} and {targetL.trackIdx}");
                }
            }

            // calculate the most efficient 'route' through the model.
            var bestMatch = trackModel.BestPath();
            if (bestMatch == null) return new Result<MapMatch>("Could not match the track.");
            
            // calculate the paths between each matched point pair.
            var rawPaths = new List<Path>();
            router = _routerDb.Route(_profile);
            for (var l = 1; l < bestMatch.Count; l++)
            {
                var source = bestMatch[l - 1];
                var sourceRp = locations[source.track].points[source.point].rp;
                var target = bestMatch[l - 0];
                var targetRp = locations[target.track].points[target.point].rp;

                var path = router.From(sourceRp).To(targetRp).Path();
                if (path.IsError) throw new Exception("Raw path calculation failed, it shouldn't fail at this point because it succeeded on the same path before.");
                rawPaths.Add(path);
            }

            return new Result<MapMatch>(
                new MapMatch(track, _profile, rawPaths));
        }
        
        private double GPSMeasurementProbability(double distance)
        {
            // TODO this is estimated in the paper, do we do the same here based on the data.
            // now we just use the max snapping distance.
            double measurementStandardDeviation = (_settings.SnappingMaxDistance / 5);

            var factor = 1; // (Math.Sqrt(2 * Math.PI) * measurementStandardDeviation);

            var exp = Math.Exp(-0.5 * System.Math.Pow(distance / measurementStandardDeviation, 2));
            
            return factor * exp;
        }

        private double ResolveProbability((double longitude, double latitude) location, SnapPoint snapPoint)
        {
            var resolvedLoc = snapPoint.LocationOnNetwork(_routerDb);

            var greatCircle = location.DistanceEstimateInMeter(resolvedLoc);

            var prob = GPSMeasurementProbability(greatCircle);
            //Console.WriteLine($"{greatCircle}: {prob} ({1-prob})");
            return 1 - prob;
        }

        private double TransitionProbability(double routeDistance, double locationDistance)
        {
            // TODO this is estimated in the paper, do we do the same here based on the data.
            // now we just use the max snapping distance.
            var beta = _settings.SnappingMaxDistance; 

            var factor = 1;// / beta;

            var exp = Math.Exp(-System.Math.Abs(routeDistance - locationDistance) / beta);

            return factor * exp;
        }

        private double RouteProbability(double routeDistance, double locationDistance, double maxDistance)
        {
            // TODO: improve, for now too simple.
            if (System.Math.Abs(routeDistance) > locationDistance * _settings.RouteDistanceFactor)
            {
                return 1;
            }
            var prob = TransitionProbability(routeDistance, locationDistance);
            //Console.WriteLine($"{routeDistance},{locationDistance} -> {System.Math.Abs(routeDistance - locationDistance)}: {prob} ({1-prob})");
            return 1-prob;
//            return (((System.Math.Abs(routeDistance - locationDistance) / maxDistance)));
        }
    }
}
