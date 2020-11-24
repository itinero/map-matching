using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Itinero.MapMatching.Test.Functional
{
    public static class GeoJsonExtensions
    {
        /// <summary>
        /// Converts the given feature collection to geojson.
        /// </summary>
        public static string ToGeoJson(this Geometry geometry)
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

        /// <summary>
        /// Converts the given router points and all associated info to a feature collection.
        /// </summary>
        /// <param name="point">The router point.</param>
        /// <param name="routerDb">The router db.</param>
        public static FeatureCollection ToFeatures(this RouterPoint point, RouterDb routerDb)
        {
            var features = new FeatureCollection();

            var location = point.Location();
            features.Add(new Feature(new Point(new Coordinate(location.Longitude, location.Latitude)), 
                new AttributesTable()));

            var locationOnNetwork = point.LocationOnNetwork(routerDb);
            
            features.Add(new Feature(new Point(new Coordinate(locationOnNetwork.Longitude, locationOnNetwork.Latitude)), 
                new AttributesTable()));
            
            features.Add(new Feature(new LineString(
                new Coordinate[] { 
                    new Coordinate(locationOnNetwork.Longitude, locationOnNetwork.Latitude),
                    new Coordinate(location.Longitude, location.Latitude)
                }), 
                new AttributesTable()));

            return features;
        }

        /// <summary>
        /// Converts the given router points and all associated info to a feature collection.
        /// </summary>
        /// <param name="points">The router points.</param>
        /// <param name="routerDb">The router db.</param>
        public static FeatureCollection ToFeatures(this IEnumerable<RouterPoint> points, RouterDb routerDb)
        {
            var features = new FeatureCollection();

            foreach (var point in points)
            {
                var pointFeatures = point.ToFeatures(routerDb);
                foreach (var feature in pointFeatures)
                {
                    features.Add(feature);
                }
            }

            return features;
        }
    }
}
