using System;
using System.IO;
using System.Collections.Generic;
using Itinero;
using Itinero.Algorithms.Search;
using Itinero.IO.Osm;
using Itinero.IO.Json;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
using System.Linq;
using Itinero.Algorithms.Weights;

namespace Itinero.MapMatching
{
    public class MapMatcher
    {
        private readonly RouterDb _db;
        private readonly Router _router;

        public MapMatcher(Router router)
        {
            _db = router.Db;
            _router = router;
        }

        public MapMatcherResult Match(Track track)
        {
            return TryMatch(track).Value;
        }

        public Result<MapMatcherResult> TryMatch(Track track)
        {
            Console.WriteLine("Snapping points to road network…");
            var projection = ProjectionOnRoads(track);
            Console.WriteLine("Calculating emission probabilities…");
            var emitP = EmitProbabilities(track, projection);
            Console.WriteLine("Calculating transition probabilities…");
            var transP = TransitionProbabilities(track, projection);
            var startP = emitP[0];

            Console.WriteLine("Running Viterbi…");
            var (path, confidence) = Solver.ForwardViterbi(startP, transP, emitP);
            Console.WriteLine("Done!");

            var routerPoints = new RouterPoint[path.Length];
            for (uint i = 0; i < path.Length; i++)
            {
                routerPoints[i] = projection[i][(int)path[i]];
            }

            var rpntLocProbs = new MapMatcherPoint[track.Points.Count][];
            for (int i = 0; i < rpntLocProbs.Length; i++)
            {
                rpntLocProbs[i] = new MapMatcherPoint[projection[i].Count];
                for (int j = 0; j < rpntLocProbs[i].Length; j++)
                {
                    var routerPoint = projection[i][j];
                    rpntLocProbs[i][j] = new MapMatcherPoint(
                            routerPoint,
                            routerPoint.LocationOnNetwork(_db),
                            emitP[i][(uint)j]);
                }
            }

            // calculating a route should succeed, because a route has been calculated between
            // these points in the transition probability calculation phase.
            var route = RouteThrough(routerPoints).Value;

            return new Result<MapMatcherResult>(new MapMatcherResult(track, rpntLocProbs, path, route));
        }

        Result<Route> RouteThrough(RouterPoint[] points)
        {
            var routes = new Result<Route>[points.Length - 1];
            for (int i = 0; i < points.Length - 1; i++)
            {
                routes[i] = _router.TryCalculate(_db.GetSupportedProfile("bicycle"), points[i], points[i + 1]);
            }

            return routes.Concatenate();
        }

        List<RouterPoint>[] ProjectionOnRoads(Track track)
        {
            /* track point, projected point */
            var projection = new List<RouterPoint>[track.Points.Count];

            var isAcceptable = _router.GetIsAcceptable(_db.GetSupportedProfile("bicycle"));

            for (int id = 0; id < track.Points.Count; id++)
            {
                Console.Write("\r{0}/{1}", id + 1, track.Points.Count);

                var resolve = new ResolveMultipleAlgorithm(
                        _db.Network.GeometricGraph,
                        track.Points[id].Coord.Latitude, track.Points[id].Coord.Longitude,
                        Offset(track.Points[id].Coord, _db.Network), 100f /* meters */,
                        isAcceptable, /* allow non-orthogonal projections */ true);
                resolve.Run();

                var points = resolve.HasSucceeded ? resolve.Results : new List<RouterPoint>();
                projection[id] = /*UniqueLocations(*/points/*)*/;
            }
            Console.WriteLine();

            return projection;
        }

        Dictionary<uint, float>[] EmitProbabilities(Track track, List<RouterPoint>[] projection)
        {
            var emitP = /* key track point (observation) */ new Dictionary<uint /* segment */, float>[projection.Length];

            // Assign probability according to zero-mean Gaussian distribution for each point

            // Gaussian distributions have two factors: the mean (here 0) and standard deviation
            double measurement_standard_deviation = 5.0; // TODO
            // log(1/x) = -log(x), and we only need the logarithm
            float factor = (float) -Math.Log(Math.Sqrt(2 * Math.PI) * measurement_standard_deviation);

            for (uint trackPointId = 0; trackPointId < projection.Length; trackPointId++)
            {
                emitP[trackPointId] = new Dictionary<uint, float>();

                uint routerPointId = 0;
                foreach (RouterPoint resolvedPoint in projection[trackPointId])
                {
                    Coordinate origLoc     = resolvedPoint.Location();
                    Coordinate resolvedLoc = resolvedPoint.LocationOnNetwork(_db);

                    // probability = log(1 / sqrt(2 * pi) / sigma * exp(-0.5 * distance(origLoc, resolvedLoc)))
                    float logProbability = factor - 0.5f * Coordinate.DistanceEstimateInMeter(origLoc, resolvedLoc);

                    emitP[trackPointId].Add(routerPointId, logProbability);

                    routerPointId++;
                }
            }

            return emitP;
        }

        Dictionary<uint, Dictionary<uint, float>>[] TransitionProbabilities(
                Track track, List<RouterPoint>[] projection)
        {
            var transitP = new Dictionary<uint, Dictionary<uint, float>>[projection.Length];

            // Assign probability according to exponential distribution for each state transition

            // Exponential distributions have one factor: the mean (= 1/λ)
            double mean = 30.0; // TODO
            // log(1/x) = -log(x), and we only need the logarithm
            float factor = (float) -Math.Log(mean);

            // These nested loops iterate over each track point in `projection` together with their successor

            for (uint trackPointId = 0; trackPointId < projection.Length - 1; trackPointId++)
            {
                Console.Write("\r{0}/{1}", trackPointId + 1, projection.Length - 1);

                transitP[trackPointId + 1] = new Dictionary<uint, Dictionary<uint, float>>();

                var sources = projection[trackPointId].ToArray();
                var targets = projection[trackPointId + 1].ToArray();
                var profile = _db.GetSupportedProfile("bicycle");
                var failedSources = new HashSet<int>();
                var failedTargets = new HashSet<int>();

                Result<float[][]> weights = _router.TryCalculateWeight(
                        profile, _router.GetDefaultWeightHandler(profile),
                        sources, targets,
                        failedSources, failedTargets);

                if (weights.IsError)
                {
                    Console.WriteLine($" error while calculating routes: {weights.ErrorMessage}");
                    continue;
                }

                Console.Write(" ok");
                if (failedSources.Count > 0) Console.Error.WriteLine($"\n  Warning: Has {failedSources.Count}/{sources.Length} failed sources");
                else if (failedTargets.Count > 0) Console.Error.WriteLine();
                if (failedTargets.Count > 0) Console.Error.WriteLine($"  Warning: Has {failedTargets.Count}/{targets.Length} failed targets");

                for (uint toPointId = 0; toPointId < projection[trackPointId + 1].Count; toPointId++)
                {
                    RouterPoint toPoint = projection[trackPointId + 1][(int) toPointId];
                    Coordinate toTrackPoint = toPoint.Location();

                    transitP[trackPointId + 1].Add(toPointId, new Dictionary<uint, float>());

                    for (uint fromPointId = 0; fromPointId < projection[trackPointId].Count; fromPointId++)
                    {
                        RouterPoint fromPoint = projection[trackPointId][(int) fromPointId];
                        Coordinate fromTrackPoint = fromPoint.Location();

                        float gcircDistance = Coordinate.DistanceEstimateInMeter(fromTrackPoint, toTrackPoint);
                        float routeDistance = weights.Value[fromPointId][toPointId];

                        // why Abs? because routeDistance - gcircDistance may be negative if
                        // snapped points are closer than original points
                        float routeVsGcircDifference = Math.Abs(routeDistance - gcircDistance);

                /*
                        // sanity check: disregard routes that make large detours
                        // TODO just a guess, we should empirically test this
                        if (routeDistance > 50.0f + gcircDistance * 5.0f) continue;

                        // sanity check: disregard routes that require insane speeds
                        DateTime? fromTime = track.Points[(int) trackPointId].Time;
                        DateTime? toTime = track.Points[(int) trackPointId + 1].Time;
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
                        float logProbability = factor - 0.5f * routeVsGcircDifference;

                        transitP[trackPointId + 1][toPointId].Add(fromPointId, logProbability);
                    }
                }
            }

            Console.WriteLine();

            return transitP;
        }

        static float Offset(Coordinate centerLocation, Itinero.Data.Network.RoutingNetwork routingNetwork)
        {
            var offsetLocation = centerLocation.OffsetWithDistances(routingNetwork.MaxEdgeDistance / 2);
            var latitudeOffset = System.Math.Abs(centerLocation.Latitude - offsetLocation.Latitude);
            var longitudeOffset = System.Math.Abs(centerLocation.Longitude - offsetLocation.Longitude);

            return System.Math.Max(latitudeOffset, longitudeOffset);
        }


        private static void Write2DArray<T>(T[][] array)
        {
            foreach (var row in array)
            {
                foreach (var cell in row)
                {
                    Console.Write($"{cell} ");
                }
                Console.WriteLine();
            }
        }

        private static List<RouterPoint> UniqueLocations(List<RouterPoint> points)
        {
            var uniquePoints = new List<RouterPoint>();
            for (int i = 0; i < points.Count; i++)
            {
                Func<RouterPoint, bool> closeBy = seenPoint =>
                    Math.Abs(points[i].Location().Latitude  - seenPoint.Location().Latitude)  <= 0.000001f &&
                    Math.Abs(points[i].Location().Longitude - seenPoint.Location().Longitude) <= 0.000001f;
                if (!Any(closeBy, uniquePoints))
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
