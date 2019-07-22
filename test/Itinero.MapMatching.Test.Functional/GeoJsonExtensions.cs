using System.IO;
using GeoAPI.Geometries;
using NetTopologySuite.Features;

namespace Itinero.MapMatching.Test.Functional
{
    public static class GeoJsonExtensions
    {
        /// <summary>
        /// Converts the given feature collection to geojson.
        /// </summary>
        public static string ToGeoJson(this IGeometry geometry)
        {
            var features = new FeatureCollection();
            features.Add(new Feature(geometry, new AttributesTable()));
            return features.ToGeoJson();
        }

        /// <summary>
        /// Converts the given feature collection to geojson.
        /// </summary>
        public static string ToGeoJson(this FeatureCollection featureCollection)
        {
            var jsonSerializer = NetTopologySuite.IO.GeoJsonSerializer.Create();
            var jsonStream = new StringWriter();
            jsonSerializer.Serialize(jsonStream, featureCollection);
            var json = jsonStream.ToString();
            return json;
        }
    }
}
