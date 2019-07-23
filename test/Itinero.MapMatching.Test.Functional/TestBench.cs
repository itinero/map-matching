using System;
using System.Collections.Generic;
using System.IO;
using Itinero;
using Itinero.Geo;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using Itinero.MapMatching;
using Itinero.MapMatching.Test.Functional.Domain;
using Itinero.Osm.Vehicles;
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
                if ("bicycle".Equals(test.Profile.File))
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

                Console.Write($" {test.Profile.Name}");

                // contract if needed.
                if (test.Profile.Contract)
                {
                    routerDb.AddContracted(routerDb.GetSupportedProfile(test.Profile.Name));
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
                var (route, points) = mapMatchResult.Value;
                var routeLineString = route.ToLineString();
                var expectedBuffered = BufferOp.Buffer(test.Expected, 0.00005);
                if (!expectedBuffered.Covers(routeLineString))
                {
                    File.WriteAllText(test.OsmDataFile + ".failed.geojson", routeLineString.ToGeoJson());
                    return (false, "Route outside of expected buffer.");
                }

                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.ToString());
            }
        }
    }
}
