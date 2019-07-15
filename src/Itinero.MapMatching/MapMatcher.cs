﻿using System;
using System.IO;
using System.Collections.Generic;
using Itinero;
using Itinero.Algorithms.Search;
using Itinero.IO.Osm;
using Itinero.IO.Json;
using Itinero.LocalGeo;
using Itinero.Osm.Vehicles;
using System.Linq;

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

        public (Route, RouterPoint[]) Match(List<Coordinate> track)
        {
            return TryMatch(track).Value;
        }

        public Result<(Route, RouterPoint[])> TryMatch(List<Coordinate> track)
        {
            Console.WriteLine("Snapping points to road network…");
            var projection = ProjectionOnRoads(track);
            Console.WriteLine("Calculating emission probabilities…");
            var emitP = EmitProbabilities(projection);
            Console.WriteLine("Calculating transition probabilities…");
            var transP = TransitionProbabilities(projection);
            var startP = emitP[0];

            Console.WriteLine("Running Viterbi…");
            var (path, confidence) = Solver.ForwardViterbi(startP, transP, emitP);

            Console.WriteLine("Done!");
            var routerPoints = new RouterPoint[path.Length];
            for (uint i = 0; i < path.Length; i++)
            {
                routerPoints[i] = projection[i][(int)path[i]];
            }

            var routes = new Result<Route>[routerPoints.Length - 1];
            for (int i = 0; i < routerPoints.Length - 1; i++)
            {
                // calculating a route should succeed, because a route has been calculated between
                // these points in the transition probability calculation phase.
                // TODO TryCalculate is used because Concatenate is only defined for IEnumerable<Result<Route>>, not IEnumerable<Route>
                routes[i] = _router.TryCalculate(_db.GetSupportedProfile("bicycle"), routerPoints[i], routerPoints[i + 1]);
            }

            return new Result<(Route, RouterPoint[])>((routes.Concatenate().Value, routerPoints));
        }

        List<RouterPoint>[] ProjectionOnRoads(List<Coordinate> track)
        {
            /* track point, projected point */
            var projection = new List<RouterPoint>[track.Count];

            var isAcceptable = _router.GetIsAcceptable(_db.GetSupportedProfile("bicycle"));

            uint id = 0;
            foreach (Coordinate track_point in track)
            {
                Console.Write("\r{0}/{1}", id + 1, track.Count);

                var resolve = new ResolveMultipleAlgorithm(
                        _db.Network.GeometricGraph,
                        track_point.Latitude, track_point.Longitude,
                        Offset(track_point, _db.Network), 200f /* meters */,
                        isAcceptable);
                resolve.Run();
                projection[id] = resolve.Results;

                id++;
            }
            Console.WriteLine();

            return projection;
        }

        Dictionary<uint, float>[] EmitProbabilities(List<RouterPoint>[] projection)
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

        Dictionary<uint, Dictionary<uint, float>>[] TransitionProbabilities(List<RouterPoint>[] projection)
        {
            var transitP = new Dictionary<uint, Dictionary<uint, float>>[projection.Length];

            // Assign probability according to exponential distribution for each state transition

            // Exponential distributions have one factor: the mean (= 1/λ)
            double mean = 5; // TODO
            // log(1/x) = -log(x), and we only need the logarithm
            float factor = (float) -Math.Log(mean);

            // These nested loops iterate over each track point in `projection` together with their successor
            // TODO Find a cleaner way to do this

            for (uint trackPointId = 0; trackPointId < projection.Length - 1; trackPointId++)
            {
                Console.Write("\r{0}/{1}         ", trackPointId + 1, projection.Length - 1);

                transitP[trackPointId + 1] = new Dictionary<uint, Dictionary<uint, float>>();

                for (uint toPointId = 0; toPointId < projection[trackPointId + 1].Count; toPointId++)
                {
                    RouterPoint toPoint = projection[trackPointId + 1][(int) toPointId];
                    Coordinate toTrackPoint = toPoint.Location();

                    transitP[trackPointId + 1].Add(toPointId, new Dictionary<uint, float>());

                    for (uint fromPointId = 0; fromPointId < projection[trackPointId].Count; fromPointId++)
                    {
                        Console.Write("\r{0}/{1}  {2,2} → {3,2}", trackPointId + 1, projection.Length - 1, fromPointId, toPointId);

                        RouterPoint fromPoint = projection[trackPointId][(int) fromPointId];
                        Coordinate fromTrackPoint = fromPoint.Location();

                        Result<Route> route = _router.TryCalculate(_db.GetSupportedProfile("bicycle"), fromPoint, toPoint);
                        if (route.IsError) continue;

                        float routeVsGcircDifference = Math.Abs(
                            route.Value.TotalDistance
                            - Coordinate.DistanceEstimateInMeter(fromTrackPoint, toTrackPoint)
                        );

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

    }
}