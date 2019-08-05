using System.Collections.Generic;
using System.IO;
using Itinero;
using Itinero.LocalGeo;

namespace Sample.Bicycle
{
    public static class GeoJsonExtensions
    {

        public static void WriteGeoJson(this IEnumerable<Line> lines, TextWriter writer)
        {
            var jsonWriter = new Itinero.IO.Json.JsonWriter(writer);
            jsonWriter.WriteOpen();
            jsonWriter.WriteProperty("type", "FeatureCollection", true, false);
            jsonWriter.WritePropertyName("features", false);
            jsonWriter.WriteArrayOpen();

            foreach (Line line in lines)
            {
                jsonWriter.WriteOpen();
                jsonWriter.WriteProperty("type", "Feature", true, false);
                jsonWriter.WritePropertyName("geometry", false);
                jsonWriter.WriteOpen();

                jsonWriter.WriteProperty("type", "LineString", true, false);

                jsonWriter.WritePropertyName("coordinates", false);
                jsonWriter.WriteArrayOpen();

                jsonWriter.WriteArrayOpen();
                jsonWriter.WriteArrayValue(line.Coordinate1.Longitude.ToInvariantString());
                jsonWriter.WriteArrayValue(line.Coordinate1.Latitude.ToInvariantString());
                jsonWriter.WriteArrayClose();

                jsonWriter.WriteArrayOpen();
                jsonWriter.WriteArrayValue(line.Coordinate2.Longitude.ToInvariantString());
                jsonWriter.WriteArrayValue(line.Coordinate2.Latitude.ToInvariantString());
                jsonWriter.WriteArrayClose();

                jsonWriter.WriteArrayClose();

                jsonWriter.WriteClose();
                jsonWriter.WriteClose(); // closes the feature.
            }

            jsonWriter.WriteArrayClose(); // closes the feature array.
            jsonWriter.WriteClose(); // closes the feature collection.
        }

    }
}
