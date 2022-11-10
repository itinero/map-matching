using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Itinero.IO.Osm;
using Itinero.MapMatching.Test.Functional.Domain;
using Itinero.Profiles;
using Itinero.Profiles.Lua;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using OsmSharp.Streams;

namespace Itinero.MapMatching.Test.Functional;

internal static class TestBench
{
    private static readonly Dictionary<string, Profile> Profiles = new();

    private static Profile LoadProfile(string vehicleFile)
    {
        if (Profiles.TryGetValue(vehicleFile, out var vehicle)) return vehicle;

        vehicle = LuaProfile.Load(File.ReadAllText(vehicleFile));
        Profiles[vehicleFile] = vehicle;
        return vehicle;
    }

    public static async Task<(bool success, string message)> RunAsync(this TestData test)
    {
        try
        {
            // load profile.
            var profile = LoadProfile(test.Profile.File);

            // load data using profile.
            var routerDb = new RouterDb();
            await using (var stream = File.OpenRead(test.OsmDataFile))
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

            var routingNetwork = routerDb.Latest;

#if DEBUG
            //File.WriteAllText(test.OsmDataFile + ".geojson", routerDb.GetGeoJson());
#endif

            // test route.
            Track track;
            if (test.TrackFile.EndsWith(".tsv"))
            {
                await using var stream = File.OpenRead(test.TrackFile);
                track = FromTsv(new StreamReader(stream));
            }
            else
            {
                await using var stream = File.OpenRead(test.TrackFile);
                track = FromGeoJson(new StreamReader(stream));
            }

            var matcher = routingNetwork.Matcher(profile);
            var match = await matcher.MatchAsync(track);
            if (match.IsError) return (false, match.ErrorMessage);

            // // check route.
            // var result = match.Value;
            // var route = matcher.Routes(result).Value.First();
            // var routeLineString = route.ToLineString();
            // var expectedBuffered = BufferOp.Buffer(test.Expected, 0.00005);
            // if (!expectedBuffered.Covers(routeLineString))
            // {
            //     File.WriteAllText(test.TrackFile + ".failed.geojson",
            //         BuildErrorOutput(route, expectedBuffered, track).ToGeoJson());
            //     return (false, "Route outside of expected buffer.");
            // }
            // #if DEBUG
            //                 else
            //                 {
            //                     File.WriteAllText(test.TrackFile + ".expected.geojson",
            //                         BuildErrorOutput(route, expectedBuffered, track).ToGeoJson());
            //                 }
            // #endif

            return (true, string.Empty);
        }
        catch (Exception e)
        {
            return (false, e.ToString());
        }
    }

    // private static FeatureCollection BuildErrorOutput(Route route, Geometry buffer, Track track)
    // {
    //     var features = new FeatureCollection();
    //
    //     features.Add(new Feature(route.ToLineString(), 
    //         new AttributesTable {{"type", "route"}}));
    //
    //     foreach (var feature in track.ToFeatures()) features.Add(feature);
    //
    //     features.Add(new Feature(buffer, new AttributesTable{{"type", "buffer"}}));
    //
    //     return features;
    // }
    //
    private static Track FromTsv(TextReader reader)
    {
        var track = new List<TrackPoint>();

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#') continue;

            var fields = line.Split("\t");

            var lat = double.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
            var lon = double.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);


            track.Add(new TrackPoint(lat, lon));
        }

        return new Track(track, ArraySegment<TrackSegment>.Empty);
    }

    private static Track FromGeoJson(TextReader reader)
    {
        var track = new List<TrackPoint>();
        var jsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
        var featureCollection = jsonSerializer.Deserialize<FeatureCollection>(new JsonTextReader(reader));
        foreach (var feature in featureCollection)
        {
            if (feature.Geometry is not LineString lineString) continue;

            for (var i = 0; i < lineString.Coordinates.Length; i++)
            {
                if (track.Count > 0 && i == 0) continue;

                var c = lineString.Coordinates[i];

                track.Add(new TrackPoint(c.X, c.Y));
            }
        }

        if (track.Count == 0) throw new Exception("No track found.");

        return new Track(track, ArraySegment<TrackSegment>.Empty);
    }
}
