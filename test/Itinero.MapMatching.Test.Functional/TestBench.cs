using System;
using System.Collections.Generic;
using System.IO;
using GeoAPI.Geometries;
using Itinero;
using Itinero.Geo;
using Itinero.IO.Osm;
using Itinero.MapMatching;
using Itinero.MapMatching.Test.Functional.Domain;
using Itinero.Osm.Vehicles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Valid;
using OsmSharp.Streams;

namespace Itinero.MapMatching.Test.Functional
{
    internal static class TestBench
    {
        public static (bool success, string message) Run(this TestData test)
        {
            try
            {
                if (!"bicycle".Equals(test.Profile.Name))
                {
                    return (false, "Currently only the bicycle profile is supported in the test bench");
                }
                var vehicle = Vehicle.Bicycle;

                // load data using vehicle.
                var routerDb = new RouterDb();
                using (var stream = File.OpenRead(test.OsmDataFile))
                {
                    if (test.OsmDataFile.EndsWith("osm.pbf"))
                    {
                        routerDb.LoadOsmData(stream, vehicle);
                    }
                    else
                    {
                        var osmStream = new XmlOsmStreamSource(stream);
                        routerDb.LoadOsmData(osmStream, vehicle);
                    }
                }

#if DEBUG
                File.WriteAllText(test.OsmDataFile + ".geojson", routerDb.GetGeoJson());
#endif

                // contract if needed.
                if (test.Profile.Contract)
                {
                    routerDb.AddContracted(Vehicle.Bicycle.Shortest());
                }

                // test route.
                Track track;
                using (var stream = File.OpenRead(test.TrackFile))
                {
                    track = Itinero.MapMatching.Demo.TrackLoader.FromTsv(new StreamReader(stream));
                }

                var router = new Router(routerDb);
                var matcher = new MapMatcher(router);

                var mapMatchResult = matcher.TryMatch(track);
                if (mapMatchResult.IsError)
                {
                    return (false, mapMatchResult.ErrorMessage);
                }

                // check route.
                MapMatcherResult result = mapMatchResult.Value;
                var routeLineString = result.Route.ToLineString();
                var expectedBuffered = BufferOp.Buffer(test.Expected, 0.000005);
                if (!expectedBuffered.Covers(routeLineString))
                {
                    File.WriteAllText(test.TrackFile + ".failed.geojson",
                        BuildErrorOutput(result.Route, result.ChosenProjectionPoints, expectedBuffered, track).ToGeoJson());
                    return (false, "Route outside of expected buffer.");
                }
#if DEBUG
                else
                {
                    File.WriteAllText(test.TrackFile + ".expected.geojson",
                        BuildErrorOutput(result.Route, result.ChosenProjectionPoints, expectedBuffered, track).ToGeoJson());
                }
#endif

                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.ToString());
            }
        }

        private static FeatureCollection BuildErrorOutput(Route route, IEnumerable<MapMatcherPoint> points, IGeometry buffer,
            Track track)
        {
            var features = new FeatureCollection();

            features.Add(new Feature(route.ToLineString(), 
                new AttributesTable {{"type", "route"}}));

            var coordinates = new List<Coordinate>();
            foreach (var point in points)
            {
                coordinates.Add(new Coordinate(point.Coord.Longitude, point.Coord.Latitude));
            }
            var lineString = new LineString(coordinates.ToArray());
            features.Add(new Feature(lineString, new AttributesTable{{ "type", "track"}}));

            features.Add(new Feature(buffer, new AttributesTable{{"type", "buffer"}}));

            return features;
        }
    }
}
