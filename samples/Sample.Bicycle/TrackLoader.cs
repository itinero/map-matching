using System;
using System.Collections.Generic;
using System.IO;
using Itinero.MapMatching;

namespace Sample.Bicycle;

public class TrackLoader
{
    // public static Track FromTsv(StreamReader reader)
    // {
    //     var track = new List<(Coordinate, DateTime?, float?)>();
    //
    //     string line;
    //     while((line = reader.ReadLine()) != null)
    //     {
    //         if (line.Length == 0 || line[0] == '#')
    //         {
    //             continue;
    //         }
    //         string[] fields = line.Split("\t");
    //
    //         DateTime? time = fields[0].Equals("None") ? (DateTime?) null : DateTime.Parse(fields[0]);
    //
    //         float lat = float.Parse(fields[1]);
    //         float lon = float.Parse(fields[2]);
    //
    //         float? maybe_hdop = null;
    //         float hdop;
    //         if (!fields[3].Equals("None") && float.TryParse(fields[3], out hdop))
    //             maybe_hdop = hdop;
    //
    //         track.Add((new Coordinate(lat, lon), time, maybe_hdop));
    //     }
    //
    //     return new Track(track);
    // }
}
