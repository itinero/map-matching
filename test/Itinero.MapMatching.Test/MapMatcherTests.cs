using Itinero.Attributes;
using Itinero.IO.Osm;
using Itinero.LocalGeo;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.MapMatching.Test
{
    public class MapMatcherTests
    {
        [Fact]
        public void MapMatcher_ExactTrackShouldMatchEdge()
        {
            // build tiny network.
            var routerDb = new RouterDb();
            routerDb.AddSupportedVehicle(Itinero.Osm.Vehicles.Vehicle.Bicycle);
            var v1 = routerDb.AddVertex(50.86697652157864f, 4.350167512893677f);
            var v2 = routerDb.AddVertex(50.86664473085768f, 4.351245760917664f);
            routerDb.AddEdge(v1, v2, new AttributeCollection(
                new Attribute("highway", "residential")));
            var router = new Router(routerDb);
            
            // build track.
            var track = new Track(new []
            {
                new TrackPoint(routerDb.Network.GetVertex(v1)), 
                new TrackPoint(routerDb.Network.GetVertex(v2))
            });
            
            var matcher = new MapMatcher(router);
            var result = matcher.Match(track);

            Assert.NotNull(result);
            Assert.NotNull(result.ChosenProjectionPoints);
            Assert.Equal(2, result.ChosenProjectionPoints.Length);
            Assert.Equal(50.86697652157864, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.350167512893677, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
            Assert.Equal(50.86664473085768, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.351245760917664, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
        }
        
        [Fact]
        public void MapMatcher_ExactEdgeShouldMatchEdgeInReverse()
        {
            // build tiny network.
            var routerDb = new RouterDb();
            routerDb.AddSupportedVehicle(Itinero.Osm.Vehicles.Vehicle.Bicycle);
            var v1 = routerDb.AddVertex(50.86697652157864f, 4.350167512893677f);
            var v2 = routerDb.AddVertex(50.86664473085768f, 4.351245760917664f);
            routerDb.AddEdge(v1, v2, new AttributeCollection(
                new Attribute("highway", "residential")));
            var router = new Router(routerDb);
            
            // build track.
            var track = new Track(new []
            {
                new TrackPoint(routerDb.Network.GetVertex(v2)),
                new TrackPoint(routerDb.Network.GetVertex(v1)), 
            });
            
            var matcher = new MapMatcher(router);
            var result = matcher.Match(track);

            Assert.NotNull(result);
            Assert.NotNull(result.ChosenProjectionPoints);
            Assert.Equal(2, result.ChosenProjectionPoints.Length);
            Assert.Equal(50.86664473085768, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.351245760917664, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
            Assert.Equal(50.86697652157864, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.350167512893677, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
        }
        
        [Fact]
        public void MapMatcher_ShouldMatchSingleEdge_Offset1()
        {
            // build tiny network.
            var routerDb = new RouterDb();
            routerDb.AddSupportedVehicle(Itinero.Osm.Vehicles.Vehicle.Bicycle);
            var v1 = routerDb.AddVertex(51.26779566943717f, 4.801357984542847f);
            var v2 = routerDb.AddVertex(51.26685586346179f, 4.801095128059387f);
            routerDb.AddEdge(v1, v2, new AttributeCollection(
                new Attribute("highway", "residential")));
            var router = new Router(routerDb);
            
            // build track.
            var track = new Track(new []
            {
                new TrackPoint(new Coordinate(51.26777888735614f, 4.801551103591918f)),
                new TrackPoint(new Coordinate(51.26690621069770f, 4.800923466682434f)) 
            });
            
            var matcher = new MapMatcher(router);
            var result = matcher.Match(track);

            Assert.NotNull(result);
            Assert.NotNull(result.ChosenProjectionPoints);
            Assert.Equal(2, result.ChosenProjectionPoints.Length);
            Assert.Equal(51.26779566943717, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801357984542847, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
            Assert.Equal(51.26685586346179, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801095128059387, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
        }
    }
}
