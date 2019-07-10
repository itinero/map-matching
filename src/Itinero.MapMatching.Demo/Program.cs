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
        static void Main(string[] args)
        {
            // load some routing data and create a router.
            var routerDb = new RouterDb();
            using (var stream = new FileInfo(@"./gent_2019-07-02.osm.pbf").OpenRead())
            {
                routerDb.LoadOsmData(stream, Vehicle.Bicycle);
            }
            var router = new Router(routerDb);

            // load some vehicle log data.
            List<Coordinate> track;
            using (var stream = new FileInfo(@"./fiets_gent_2018-12-19.tsv").OpenRead())
            {
                track = LoadTsv(stream);
            }

            // do map matching.
            var matcher = new MapMatcher(router);
            var matched = matcher.Match(track);
            var geoJson = matched.ToGeoJson();

            Console.WriteLine(geoJson);
        }

        static List<Coordinate> LoadTsv(FileStream stream)
        {
            List<Coordinate> track = new List<Coordinate>();

            var reader = new System.IO.StreamReader(stream);
            string line;
            while((line = reader.ReadLine()) != null)
            {
                string[] fields = line.Split("\t");
                float lat = float.Parse(fields[2]);
                float lon = float.Parse(fields[3]);
                track.Add(new Coordinate(lat, lon));
            }

            return track;
        }
    }
}
