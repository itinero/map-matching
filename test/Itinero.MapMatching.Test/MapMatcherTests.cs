using Itinero.Attributes;
using Itinero.IO.Osm;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.MapMatching.Test
{
    public class MapMatcherTests
    {
        [Fact]
        public void MapMatcher_ExactEdgeShouldMatchEdge()
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
    }
}
