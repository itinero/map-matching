using System;
using System.Collections.Generic;
using System.IO;
using GeoAPI.Geometries;
using Itinero.Geo;
using Itinero.IO.Osm;
using Itinero.MapMatching.Test.Functional.Domain;
using Itinero.Profiles;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using Newtonsoft.Json;
using OsmSharp.Streams;
using Vehicle = Itinero.Osm.Vehicles.Vehicle;

namespace Itinero.MapMatching.Test.Functional
{
    internal static class TestBench
    {
        private static Dictionary<string, DynamicVehicle> Vehicles = new Dictionary<string, DynamicVehicle>();
        
        private static DynamicVehicle LoadVehicle(string vehicleFile)
        {
            if (Vehicles.TryGetValue(vehicleFile, out var vehicle)) return vehicle;
            
            using (var vehicleFileStream = File.OpenRead(vehicleFile))
            {
                vehicle = DynamicVehicle.LoadFromStream(vehicleFileStream);
            }

            Vehicles[vehicleFile] = vehicle;
            return vehicle;
        }
        
        public static (bool success, string message) Run(this TestData test)
        {
            try
            {
                // load vehicle.
                var vehicle = LoadVehicle(test.Profile.File);

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
                var mapMatchResult = router.Match(track, new MapMatcherSettings()
                {
                    Profile = vehicle.Shortest().FullName
                });
                if (mapMatchResult.IsError)
                {
                    return (false, mapMatchResult.ErrorMessage);
                }

                // check route.
                var result = mapMatchResult.Value;
                var route = router.ToRoute(result);
                var routeLineString = route.ToLineString();
                var expectedBuffered = BufferOp.Buffer(test.Expected, 0.00005);
                if (!expectedBuffered.Covers(routeLineString))
                {
                    File.WriteAllText(test.TrackFile + ".failed.geojson",
                        BuildErrorOutput(router, route, result.RouterPoints, expectedBuffered, track).ToGeoJson());
                    return (false, "Route outside of expected buffer.");
                }
#if DEBUG
                else
                {
                    File.WriteAllText(test.TrackFile + ".expected.geojson",
                        BuildErrorOutput(router, route, result.RouterPoints, expectedBuffered, track).ToGeoJson());
                }
#endif

                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.ToString());
            }
        }

        private static FeatureCollection BuildErrorOutput(Router router, Route route, IEnumerable<RouterPoint> points, IGeometry buffer,
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
                    new Coordinate(point.Location().Longitude, 
                        point.Location().Latitude),
                    new Coordinate(point.LocationOnNetwork(router.Db).Longitude,
                        point.LocationOnNetwork(router.Db).Latitude), 
                }), new AttributesTable{{"type", "snapline"}}));
                coordinates.Add(new Coordinate(point.Location().Longitude, 
                    point.Location().Latitude));
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
            var track = new List<(Itinero.LocalGeo.Coordinate, DateTime?, float?)>();
            var jsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
            var featureCollection = jsonSerializer.Deserialize<FeatureCollection>(new JsonTextReader(reader));
            foreach (var feature in featureCollection.Features)
            {
                if (feature.Geometry is LineString lineString)
                {
                    for (var i = 0; i < lineString.Coordinates.Length; i++)
                    {
                        if (track.Count > 0 && i == 0) continue;

                        var c = lineString.Coordinates[i];
                        
                        track.Add((new Itinero.LocalGeo.Coordinate((float)c.Y, (float)c.X), null, null));
                    }
                }
            }

            if (track.Count == 0) throw new Exception("No track found.");
            
            return new Track(track);
        }
    }
}
