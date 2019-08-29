using System;
using System.IO;
using System.Collections.Generic;
using Itinero;
using Itinero.Algorithms.Search;
using Itinero.IO.Osm;
using Itinero.IO.Json;
using Itinero.LocalGeo;
using System.Linq;
using System.Runtime.CompilerServices;
using Itinero.Algorithms.Weights;
using Itinero.Logging;
using Itinero.Profiles;
using Vehicle = Itinero.Osm.Vehicles.Vehicle;

[assembly: InternalsVisibleTo("Itinero.MapMatching.Test")]
namespace Itinero.MapMatching
{
    internal class MapMatcher
    {
        private readonly Router _router;
        private readonly Profile _profile;

        public MapMatcher(Router router, Profile profile)
        {
            if (profile.Metric != ProfileMetric.DistanceInMeters) { throw new ArgumentException(
                $"Only profiles supported with distance as metric.", $"{nameof(profile)}"); }
            
            _router = router;
            _profile = profile;
        }

        public MapMatcherResult Match(Track track)
        {
            return TryMatch(track).Value;
        }

        public Result<MapMatcherResult> TryMatch(Track track)
        {
            Itinero.Logging.Logger.Log($"{nameof(MapMatcher)}.{nameof(TryMatch)}", TraceEventType.Verbose,
                $"Snapping points to road network...");
            var projection = ProjectionOnRoads(track);
            Itinero.Logging.Logger.Log($"{nameof(MapMatcher)}.{nameof(TryMatch)}", TraceEventType.Verbose,
                $"Calculating emission probabilities...");
            var emitP = EmitProbabilities(track, projection);
            Itinero.Logging.Logger.Log($"{nameof(MapMatcher)}.{nameof(TryMatch)}", TraceEventType.Verbose,
                $"Calculating transition probabilities...");
            var transP = TransitionProbabilities(track, projection);
            var startP = emitP[0];

            Itinero.Logging.Logger.Log($"{nameof(MapMatcher)}.{nameof(TryMatch)}", TraceEventType.Verbose,
                $"Running Viterbi...");
            var (path, confidence) = Solver.ForwardViterbi(startP, transP, emitP);

            var routerPoints = new RouterPoint[path.Length];
            for (var i = 0; i < path.Length; i++)
            {
                routerPoints[i] = projection[i][path[i]];
            }

            var rpntLocProbs = new MapMatcherPoint[track.Points.Count][];
            for (var i = 0; i < rpntLocProbs.Length; i++)
            {
                rpntLocProbs[i] = new MapMatcherPoint[projection[i].Count];
                for (var j = 0; j < rpntLocProbs[i].Length; j++)
                {
                    var routerPoint = projection[i][j];
                    rpntLocProbs[i][j] = new MapMatcherPoint(
                            routerPoint,
                            routerPoint.LocationOnNetwork(_router.Db),
                            emitP[i][j]);
                }
            }

            // calculating a route should succeed, because a route has been calculated between
            // these points in the transition probability calculation phase.
            var route = RouteThrough(routerPoints).Value;

            return new Result<MapMatcherResult>(new MapMatcherResult(track, rpntLocProbs, path, route));
        }

        private Result<Route> RouteThrough(IReadOnlyList<RouterPoint> points)
        {
            var routes = new Result<Route>[points.Count - 1];
            for (var i = 0; i < points.Count - 1; i++)
            {
                routes[i] = _router.TryCalculate(_profile, points[i], points[i + 1]);
            }

            return routes.Concatenate();
        }

        private List<RouterPoint>[] ProjectionOnRoads(Track track)
        {
            /* track point, projected point */
            var projection = new List<RouterPoint>[track.Points.Count];

            var isAcceptable = _router.GetIsAcceptable(_profile);

            for (var id = 0; id < track.Points.Count; id++)
            {
                var resolve = new ResolveMultipleAlgorithm(
                    _router.Db.Network.GeometricGraph,
                        track.Points[id].Coord.Latitude, track.Points[id].Coord.Longitude,
                        Offset(track.Points[id].Coord, _router.Db.Network), 100f /* meters */,
                        isAcceptable, /* allow non-orthogonal projections */ true);
                resolve.Run();

                var points = resolve.HasSucceeded ? resolve.Results : new List<RouterPoint>();
                projection[id] = /*UniqueLocations(*/points/*)*/;
            }

            return projection;
        }

        private Dictionary<int, float>[] EmitProbabilities(Track track, IReadOnlyList<List<RouterPoint>> projection)
        {
            var emitP = /* key track point (observation) */ new Dictionary<int /* segment */, float>[projection.Count];

            // Assign probability according to zero-mean Gaussian distribution for each point

            // Gaussian distributions have two factors: the mean (here 0) and standard deviation
            const double measurementStandardDeviation = 5.0; // TODO
            // log(1/x) = -log(x), and we only need the logarithm
            var factor = (float) -Math.Log(Math.Sqrt(2 * Math.PI) * measurementStandardDeviation);

            for (var trackPointId = 0; trackPointId < projection.Count; trackPointId++)
            {
                emitP[trackPointId] = new Dictionary<int, float>();

                var routerPointId = 0;
                foreach (var resolvedPoint in projection[trackPointId])
                {
                    var origLoc     = resolvedPoint.Location();
                    var resolvedLoc = resolvedPoint.LocationOnNetwork(_router.Db);

                    // probability = log(1 / sqrt(2 * pi) / sigma * exp(-0.5 * distance(origLoc, resolvedLoc)))
                    var logProbability = factor - 0.5f * Coordinate.DistanceEstimateInMeter(origLoc, resolvedLoc);

                    emitP[trackPointId].Add(routerPointId, logProbability);

                    routerPointId++;
                }
            }

            return emitP;
        }

        private Dictionary<int, Dictionary<int, float>>[] TransitionProbabilities(
                Track track, IReadOnlyList<List<RouterPoint>> projection)
        {
            var transitP = new Dictionary<int, Dictionary<int, float>>[projection.Count];

            // Assign probability according to exponential distribution for each state transition

            // Exponential distributions have one factor: the mean (= 1/λ)
            const double mean = 30.0; // TODO
            // log(1/x) = -log(x), and we only need the logarithm
            var factor = (float) -Math.Log(mean);

            // These nested loops iterate over each track point in `projection` together with their successor

            for (var trackPointId = 0; trackPointId < projection.Count - 1; trackPointId++)
            {
                //Console.Write("\r{0}/{1}", trackPointId + 1, projection.Count - 1);

                transitP[trackPointId + 1] = new Dictionary<int, Dictionary<int, float>>();

                var sources = projection[trackPointId].ToArray();
                var targets = projection[trackPointId + 1].ToArray();
                var failedSources = new HashSet<int>();
                var failedTargets = new HashSet<int>();

                var weights = _router.TryCalculateWeight(
                        _profile, _router.GetDefaultWeightHandler(_profile),
                        sources, targets,
                        failedSources, failedTargets);

                if (weights.IsError)
                {
                    Itinero.Logging.Logger.Log($"{nameof(MapMatcher)}.{nameof(TransitionProbabilities)}", TraceEventType.Error,
                        $"Error while calculating routes: {weights.ErrorMessage}");
                    continue;
                }

                if (failedSources.Count > 0) Console.Error.WriteLine($"\n  Warning: Has {failedSources.Count}/{sources.Length} failed sources");
                else if (failedTargets.Count > 0) Console.Error.WriteLine();
                if (failedTargets.Count > 0) Console.Error.WriteLine($"  Warning: Has {failedTargets.Count}/{targets.Length} failed targets");

                var fromTrackPoint = track.Points[trackPointId].Coord;
                var toTrackPoint = track.Points[trackPointId + 1].Coord;

                for (var toRouterPointId = 0; toRouterPointId < projection[trackPointId + 1].Count; toRouterPointId++)
                {
                    transitP[trackPointId + 1].Add(toRouterPointId, new Dictionary<int, float>());

                    for (var fromRouterPointId = 0; fromRouterPointId < projection[trackPointId].Count; fromRouterPointId++)
                    {
                        var routeDistance = weights.Value[fromRouterPointId][toRouterPointId];
                        if (routeDistance >= float.MaxValue) continue;

                        var gcircDistance = Coordinate.DistanceEstimateInMeter(fromTrackPoint, toTrackPoint);

                        // why Abs? because routeDistance - gcircDistance may be negative if
                        // snapped points are closer than original points
                        var routeVsGcircDifference = Math.Abs(routeDistance - gcircDistance);

                /*
                        // sanity check: disregard routes that make large detours
                        // TODO just a guess, we should empirically test this
                        if (routeDistance > 50.0f + gcircDistance * 5.0f) continue;

                        // sanity check: disregard routes that require insane speeds
                        DateTime? fromTime = track.Points[trackPointId].Time;
                        DateTime? toTime = track.Points[trackPointId + 1].Time;
                        if (fromTime.HasValue && toTime.HasValue)
                        {
                            // FIXME hardcoded for bikes
                            // 100 km/h ≅ 27 m/s
                            // speed = dist/time > 27 m/s  ⇔  dist > time * 27 m/s
                            if (routeDistance > (toTime.Value - fromTime.Value).TotalSeconds * 27.77777f)
                                continue;
                        }
                */

                        // probability = 1 / beta * Math.Exp(-0.5 * routeVsGcircDifference)
                        var logProbability = factor - 0.5f * routeVsGcircDifference;

                        transitP[trackPointId + 1][toRouterPointId].Add(fromRouterPointId, logProbability);
                    }
                }
            }

            return transitP;
        }

        static float Offset(Coordinate centerLocation, Itinero.Data.Network.RoutingNetwork routingNetwork)
        {
            var offsetLocation = centerLocation.OffsetWithDistances(routingNetwork.MaxEdgeDistance / 2);
            var latitudeOffset = System.Math.Abs(centerLocation.Latitude - offsetLocation.Latitude);
            var longitudeOffset = System.Math.Abs(centerLocation.Longitude - offsetLocation.Longitude);

            return System.Math.Max(latitudeOffset, longitudeOffset);
        }

        private static List<RouterPoint> UniqueLocations(IReadOnlyList<RouterPoint> points)
        {
            var uniquePoints = new List<RouterPoint>();
            for (var i = 0; i < points.Count; i++)
            {
                bool CloseBy(RouterPoint seenPoint) => Math.Abs(points[i].Location().Latitude - seenPoint.Location().Latitude) <= 0.000001f && Math.Abs(points[i].Location().Longitude - seenPoint.Location().Longitude) <= 0.000001f;

                if (!Any(CloseBy, uniquePoints))
                {
                    uniquePoints.Add(points[i]);
                }
            }
            return uniquePoints;
        }

        private static bool Any<T>(Func<T, bool> f, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                if (f(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
