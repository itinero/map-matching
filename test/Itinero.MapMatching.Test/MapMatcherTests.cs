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
        public void MapMatcher_SingleEdge_ExactTrack_ShouldMatchEdge()
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
        public void MapMatcher_SingleEdge_ExactTrack_ShouldMatchEdgeInReverse()
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
        public void MapMatcher_SingleEdge_OffsetTrack_ShouldMatchEdge()
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
        
        [Fact]
        public void MapMatcher_SingleEdge_ExactTrack_ShouldMatchEdgePart()
        {
            // build tiny network.
            var routerDb = new RouterDb();
            routerDb.AddSupportedVehicle(Itinero.Osm.Vehicles.Vehicle.Bicycle);
            var v1 = routerDb.AddVertex(51.26779566943717f, 4.801357984542847f);
            var v2 = routerDb.AddVertex(51.26654370936813f, 4.801028072834015f);
            routerDb.AddEdge(v1, v2, new AttributeCollection(
                new Attribute("highway", "residential")));
            var router = new Router(routerDb);
            
            // build track.
            var track = new Track(new []
            {
                new TrackPoint(new Coordinate(51.26758589298387f, 4.801304340362548f)),
                new TrackPoint(new Coordinate(51.26692970605557f, 4.801129996776581f)) 
            });
            
            var matcher = new MapMatcher(router);
            var result = matcher.Match(track);

            Assert.NotNull(result);
            Assert.NotNull(result.ChosenProjectionPoints);
            Assert.Equal(2, result.ChosenProjectionPoints.Length);
            Assert.Equal(51.26758589298387, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801304340362548, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
            Assert.Equal(51.26692970605557, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801129996776581, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
        }
        
        [Fact]
        public void MapMatcher_SingleEdge_OffsetTrack_ShouldMatchEdgePart()
        {
            // build tiny network.
            var routerDb = new RouterDb();
            routerDb.AddSupportedVehicle(Itinero.Osm.Vehicles.Vehicle.Bicycle);
            var v1 = routerDb.AddVertex(51.26779566943717f, 4.801357984542847f);
            var v2 = routerDb.AddVertex(51.26654370936813f, 4.801028072834015f);
            routerDb.AddEdge(v1, v2, new AttributeCollection(
                new Attribute("highway", "residential")));
            var router = new Router(routerDb);
            
            // build track.
            var track = new Track(new []
            {
                new TrackPoint(new Coordinate(51.267574145474164f, 4.80153501033783f)),
                new TrackPoint(new Coordinate(51.26693641901275f, 4.800944924354553f)) 
            });
            
            var matcher = new MapMatcher(router);
            var result = matcher.Match(track);

            Assert.NotNull(result);
            Assert.NotNull(result.ChosenProjectionPoints);
            Assert.Equal(2, result.ChosenProjectionPoints.Length);
            Assert.Equal(51.26758589298387, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801304340362548, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
            Assert.Equal(51.26692970605557, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801129996776581, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
        }
        
        [Fact]
        public void MapMatcher_SingleEdge_OffsetTrack_BiggerThanEdge_ShouldMatchEdge()
        {
            // build tiny network.
            var routerDb = new RouterDb();
            routerDb.AddSupportedVehicle(Itinero.Osm.Vehicles.Vehicle.Bicycle);
            var v1 = routerDb.AddVertex(51.26779566943717f, 4.801357984542847f);
            var v2 = routerDb.AddVertex(51.26654370936813f, 4.801028072834015f);
            routerDb.AddEdge(v1, v2, new AttributeCollection(
                new Attribute("highway", "residential")));
            var router = new Router(routerDb);
            
            // build track.
            var track = new Track(new []
            {
                new TrackPoint(new Coordinate(51.26812124059636f, 4.801663756370544f)),
                new TrackPoint(new Coordinate(51.266372527190775f,  4.800816178321838f)) 
            });
            
            var matcher = new MapMatcher(router);
            var result = matcher.Match(track);

            Assert.NotNull(result);
            Assert.NotNull(result.ChosenProjectionPoints);
            Assert.Equal(2, result.ChosenProjectionPoints.Length);
            Assert.Equal(51.26779566943717f, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801357984542847f, result.ChosenProjectionPoints[0].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
            Assert.Equal(51.26654370936813f, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Latitude, 4);
            Assert.Equal(4.801028072834015f, result.ChosenProjectionPoints[1].RPoint.LocationOnNetwork(routerDb).Longitude, 4);
        }
    }
}
