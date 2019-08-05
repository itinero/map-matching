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
using Newtonsoft.Json;
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
                if (test.TrackFile.EndsWith(".tsv"))
                {
                    using (var stream = File.OpenRead(test.TrackFile))
                    {
                        track = FromTsv(new StreamReader(stream));
                    }
                }
                else
                {
                    using (var stream = File.OpenRead(test.TrackFile))
                    {
                        track = FromGeoJson(new StreamReader(stream));
                    }
                }

                var router = new Router(routerDb);
                var matcher = new MapMatcher(router, router.Db.GetSupportedProfile("bicycle"));

                var mapMatchResult = matcher.TryMatch(track);
                if (mapMatchResult.IsError)
                {
                    return (false, mapMatchResult.ErrorMessage);
                }

                // check route.
                var result = mapMatchResult.Value;
                var routeLineString = result.Route.ToLineString();
                var expectedBuffered = BufferOp.Buffer(test.Expected, 0.00005);
                if (!expectedBuffered.Covers(routeLineString))
                {
                    File.WriteAllText(test.TrackFile + ".failed.geojson",
                        BuildErrorOutput(router, result.Route, result.ChosenProjectionPoints, expectedBuffered, track).ToGeoJson());
                    return (false, "Route outside of expected buffer.");
                }
#if DEBUG
                else
                {
                    File.WriteAllText(test.TrackFile + ".expected.geojson",
                        BuildErrorOutput(router, result.Route, result.ChosenProjectionPoints, expectedBuffered, track).ToGeoJson());
                }
#endif

                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.ToString());
            }
        }

        private static FeatureCollection BuildErrorOutput(Router router, Route route, IEnumerable<MapMatcherPoint> points, IGeometry buffer,
            Track track)
        {
            var features = new FeatureCollection();

            features.Add(new Feature(route.ToLineString(), 
                new AttributesTable {{"type", "route"}}));

            var coordinates = new List<Coordinate>();
            foreach (var point in points)
            {
                features.Add(new Feature(new LineString(new []
                {
                    new Coordinate(point.RPoint.Location().Longitude, 
                        point.RPoint.Location().Latitude),
                    new Coordinate(point.RPoint.LocationOnNetwork(router.Db).Longitude,
                        point.RPoint.LocationOnNetwork(router.Db).Latitude), 
                }), new AttributesTable{{"type", "snapline"}}));
                coordinates.Add(new Coordinate(point.RPoint.Location().Longitude, 
                    point.RPoint.Location().Latitude));
            }
            var lineString = new LineString(coordinates.ToArray());
            features.Add(new Feature(lineString, new AttributesTable{{ "type", "track"}}));

            features.Add(new Feature(buffer, new AttributesTable{{"type", "buffer"}}));

            return features;
        }
        
        private static Track FromTsv(TextReader reader)
        {
            var track = new List<(Itinero.LocalGeo.Coordinate, DateTime?, float?)>();

            string line;
            while((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }
                var fields = line.Split("\t");

                var time = fields[0].Equals("None") ? (DateTime?) null : DateTime.Parse(fields[0]);

                var lat = float.Parse(fields[1]);
                var lon = float.Parse(fields[2]);

                float? maybeHdop = null;
                if (!fields[3].Equals("None") && float.TryParse(fields[3], out var hdop))
                    maybeHdop = hdop;

                track.Add((new Itinero.LocalGeo.Coordinate(lat, lon), time, maybeHdop));
            }

            return new Track(track);
        }
        
        private static Track FromGeoJson(TextReader reader)
        {
            var jsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
            var featureCollection = jsonSerializer.Deserialize<FeatureCollection>(new JsonTextReader(reader));
            foreach (var feature in featureCollection.Features)
            {
                if (feature.Geometry is LineString lineString)
                {
                    var track = new List<(Itinero.LocalGeo.Coordinate, DateTime?, float?)>();

                    foreach (var c in lineString.Coordinates)
                    {
                        track.Add((new Itinero.LocalGeo.Coordinate((float)c.Y, (float)c.X), null, null));
                    }

                    return new Track(track);
                }
            }
            
            throw new Exception("Track not found.");
        }
    }
}
