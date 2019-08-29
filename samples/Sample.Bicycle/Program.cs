using System;
using System.IO;
using Itinero;
using Itinero.LocalGeo;
using Itinero.MapMatching;

namespace Sample.Bicycle
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
            Track track;
            using (var inFile = File.OpenRead(args[1]))
            using (var reader = new StreamReader(inFile))
            {
                track = TrackLoader.FromTsv(reader);
            }

            // do map matching.
            var mapMatcherResult = router.Match(router.Db.GetSupportedProfile("bicycle"), track).Value;
            if (args.Length >= 2 && args[2].Length > 0 && !args[2].Equals("-"))
            {
                File.WriteAllText(args[2], mapMatcherResult.Route.ToGeoJson());
            }
            else
            {
                Console.WriteLine(mapMatcherResult.Route.ToGeoJson());
            }

            if (args.Length >= 3)
            {
                var projectionLines = mapMatcherResult.ChosenProjectionLines();
                using (var outFile = File.Create(args[3]))
                using (var writer = new StreamWriter(outFile))
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

        public static Line[] ProjectionLines(Track track, RouterPoint[] projection, RouterDb routerDb)
        {
            var projectionLines = new Line[track.Points.Count];
            int i = 0;
            foreach (var point in track.Points)
            {
                projectionLines[i] = new Line(point.Coord, projection[i].LocationOnNetwork(routerDb));
                i++;
            }
            return projectionLines;
        }
    }
}
