using System;
using System.Collections.Generic;
using System.IO;
using Itinero;
using Itinero.LocalGeo;
using Itinero.MapMatching;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;

namespace Itinero.MapMatching.Demo
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintHelp(Console.Error);
                return 1;
            }

            var routerDb = RouterDb.Deserialize(new FileInfo(args[0]).OpenRead());
            var router = new Router(routerDb);

            // load some vehicle log data.
            List<Coordinate> track;
            using (var stream = new FileInfo(args[1]).OpenRead())
            {
                track = LoadTsv(stream);
            }

            // do map matching.
            var matcher = new MapMatcher(router);
            var (matched, projection) = matcher.Match(track);

            if (args.Length >= 2 && args[2].Length > 0 && !args[2].Equals("-"))
            {
                File.WriteAllText(args[2], matched.ToGeoJson());
            }
            else
            {
                Console.WriteLine(matched.ToGeoJson());
            }

            if (args.Length >= 3)
            {
                var projectionLines = new Line[track.Count];
                for (int i = 0; i < track.Count; i++)
                {
                    projectionLines[i] = new Line(track[i], projection[i].LocationOnNetwork(routerDb));
                }
                using (var outFile = File.Create(args[3]))
                using (var writer = new System.IO.StreamWriter(outFile))
                {
                    projectionLines.WriteGeoJson(writer);
                }
            }

            return 0;
        }

        static void PrintHelp(TextWriter writer)
        {
            writer.WriteLine("Usage:\n  dotnet run -- <routerdb> <tsv gps log> [matched route output file] [projection to points output file]");
        }

        static List<Coordinate> LoadTsv(FileStream stream)
        {
            List<Coordinate> track = new List<Coordinate>();

            using (var reader = new System.IO.StreamReader(stream))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    if (line[0] == '#')
                    {
                        continue;
                    }
                    string[] fields = line.Split("\t");
                    float lat = float.Parse(fields[2]);
                    float lon = float.Parse(fields[3]);
                    track.Add(new Coordinate(lat, lon));
                }
            }

            return track;
        }
    }
}
