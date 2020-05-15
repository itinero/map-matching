using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Geo;
using Itinero.IO.Osm;
using Itinero.MapMatching.Test.Functional.Domain;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Buffer;
using Newtonsoft.Json;
using OsmSharp.Streams;

namespace Itinero.MapMatching.Test.Functional
{
    internal static class TestBench
    {
        private static readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>();

        private static Profile LoadProfile(string vehicleFile)
        {
            if (Profiles.TryGetValue(vehicleFile, out var vehicle)) return vehicle;

            vehicle = LuaProfile.Load(File.ReadAllText(vehicleFile));
            Profiles[vehicleFile] = vehicle;
            return vehicle;
        }

        public static (bool success, string message) Run(this TestData test)
        {
            try
            {
                // load profile.
                var profile = LoadProfile(test.Profile.File);

                // load data using profile.
                var routerDb = new RouterDb();
                using (var stream = File.OpenRead(test.OsmDataFile))
                {
                    if (test.OsmDataFile.EndsWith("osm.pbf"))
                    {
                        routerDb.UseOsmData(new PBFOsmStreamSource(stream));
                    }
                    else
                    {
                        var osmStream = new XmlOsmStreamSource(stream);
                        routerDb.UseOsmData(osmStream);
                    }
                }

#if DEBUG
                //File.WriteAllText(test.OsmDataFile + ".geojson", routerDb.GetGeoJson());
#endif
                
                // test route.
                Track track;
                if (test.TrackFile.EndsWith(".tsv"))
                {
                    using var stream = File.OpenRead(test.TrackFile);
                    track = FromTsv(new StreamReader(stream));
                }
                else
                {
                    using var stream = File.OpenRead(test.TrackFile);
                    track = FromGeoJson(new StreamReader(stream));
                }

                var matcher = routerDb.Matcher(profile);
                var match = matcher.Match(track);
                if (match.IsError)
                {
                    return (false, match.ErrorMessage);
                }

                // check route.
                var result = match.Value;
                var route = matcher.Routes(result).Value.First();
                var routeLineString = route.ToLineString();
                var expectedBuffered = BufferOp.Buffer(test.Expected, 0.00005);
                if (!expectedBuffered.Covers(routeLineString))
                {
                    File.WriteAllText(test.TrackFile + ".failed.geojson",
                        BuildErrorOutput(route, expectedBuffered, track).ToGeoJson());
                    return (false, "Route outside of expected buffer.");
                }
#if DEBUG
                else
                {
                    File.WriteAllText(test.TrackFile + ".expected.geojson",
                        BuildErrorOutput(route, expectedBuffered, track).ToGeoJson());
                }
#endif

                return (true, string.Empty);
            }
            catch (Exception e)
            {
                return (false, e.ToString());
            }
        }

        private static FeatureCollection BuildErrorOutput(Route route, Geometry buffer, Track track)
        {
            var features = new FeatureCollection();

            features.Add(new Feature(route.ToLineString(), 
                new AttributesTable {{"type", "route"}}));

            foreach (var feature in track.ToFeatures()) features.Add(feature);

            features.Add(new Feature(buffer, new AttributesTable{{"type", "buffer"}}));

            return features;
        }
        
        private static Track FromTsv(TextReader reader)
        {
            var track = new List<((double lon, double lat) coordinate, DateTime?, double?)>();

            string line;
            while((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }
                var fields = line.Split("\t");

                var time = fields[0].Equals("None") ? (DateTime?) null : DateTime.Parse(fields[0]);

                var lat = double.Parse(fields[1]);
                var lon = double.Parse(fields[2]);

                double? maybeHdop = null;
                if (!fields[3].Equals("None") && double.TryParse(fields[3], out var hdop))
                    maybeHdop = hdop;

                track.Add(((lat, lon), time, maybeHdop));
            }

            return new Track(track);
        }
        
        private static Track FromGeoJson(TextReader reader)
        {
            var track = new List<((double longitude, double latitude) coordinate, DateTime?, double?)>();
            var jsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
            var featureCollection = jsonSerializer.Deserialize<FeatureCollection>(new JsonTextReader(reader));
            foreach (var feature in featureCollection)
            {
                if (!(feature.Geometry is LineString lineString)) continue;
                
                for (var i = 0; i < lineString.Coordinates.Length; i++)
                {
                    if (track.Count > 0 && i == 0) continue;

                    var c = lineString.Coordinates[i];
                        
                    track.Add(((c.X, c.Y), null, null));
                }
            }

            if (track.Count == 0) throw new Exception("No track found.");
            
            return new Track(track);
        }
    }
}
