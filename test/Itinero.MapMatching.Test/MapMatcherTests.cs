// using Itinero.Geo;
// using Itinero.Profiles.Lua.Osm;
// using Xunit;
//
// namespace Itinero.MapMatching.Test
// {
//     public class MapMatcherTests
//     {
//         [Fact]
//         public void MapMatcher_OneEdge_ExactTrack_ShouldMatchEdge()
//         {
//             // build tiny network.
//             var routerDb = new RouterDb();
//             var v1 = routerDb.AddVertex(4.350167512893677, 50.86697652157864);
//             var v2 = routerDb.AddVertex(4.351245760917664, 50.86664473085768);
//             var edge = routerDb.AddEdge(v1, v2, attributes: new[] { ("highway", "residential") });
//
//             var edgeEnumerator = routerDb.GetEdgeEnumerator();
//             edgeEnumerator.MoveTo(v1);
//             edgeEnumerator.MoveNext();
//             
//             // build track.
//             var track = new Track(new []
//             {
//                 new TrackPoint(routerDb.GetVertex(v1)), 
//                 new TrackPoint(routerDb.GetVertex(v2))
//             });
//             
//             // match track.
//             var result = routerDb.Matcher(OsmProfiles.Bicycle)
//                 .Match(track);
//
//             Assert.NotNull(result);
//             Assert.Single(result.Value);
//             var path = result.Value[0];
//             Assert.NotNull(path);
//             Assert.Equal(edge, path.First.edge);
//             Assert.True(path.First.direction);
//             Assert.Equal(0, path.Offset1);
//             Assert.Equal(ushort.MaxValue, path.Offset2);
//         }
//         
//         [Fact]
//         public void MapMatcher_OneEdge_ExactTrackBackward_ShouldMatchEdge()
//         {
//             // build tiny network.
//             var routerDb = new RouterDb();
//             var v1 = routerDb.AddVertex(4.350167512893677, 50.86697652157864);
//             var v2 = routerDb.AddVertex(4.351245760917664, 50.86664473085768);
//             var edge = routerDb.AddEdge(v1, v2, attributes: new[] { ("highway", "residential") });
//
//             var edgeEnumerator = routerDb.GetEdgeEnumerator();
//             edgeEnumerator.MoveTo(v1);
//             edgeEnumerator.MoveNext();
//             
//             // build track.
//             var track = new Track(new []
//             {
//                 new TrackPoint(routerDb.GetVertex(v2)),
//                 new TrackPoint(routerDb.GetVertex(v1)), 
//             });
//             
//             // match track.
//             var result = routerDb.Matcher(OsmProfiles.Bicycle)
//                 .Match(track);
//
//             Assert.NotNull(result);
//             Assert.Single(result.Value);
//             var path = result.Value[0];
//             Assert.NotNull(path);
//             Assert.Equal(edge, path.First.edge);
//             Assert.False(path.First.direction);
//             Assert.Equal(0, path.Offset1);
//             Assert.Equal(ushort.MaxValue, path.Offset2);
//         }
//         
//         [Fact]
//         public void MapMatcher_OneEdge_OffsetTrack_ShouldMatchEdge()
//         {
//             // build tiny network.
//             var routerDb = new RouterDb();
//             var v1 = routerDb.AddVertex(4.801357984542847, 51.26779500000000);
//             var v2 = routerDb.AddVertex(4.801095128059387, 51.26685586346179);
//             var edge = routerDb.AddEdge(v1, v2, attributes: new [] { ("highway", "residential") });
//             
//             // build track.
//             var track = new Track(new []
//             {
//                 new TrackPoint(routerDb.GetVertex(v1).OffsetWithDistanceX(5).OffsetWithDistanceY(-5)),
//                 new TrackPoint(routerDb.GetVertex(v2).OffsetWithDistanceX(-5).OffsetWithDistanceY(5)),
//             });
//
//             // match track.
//             var result = routerDb.Matcher(OsmProfiles.Bicycle)
//                 .Match(track);
//
//             Assert.NotNull(result);
//             Assert.Single(result.Value);
//             var path = result.Value[0];
//             Assert.NotNull(path);
//             Assert.Equal(edge, path.First.edge);
//             Assert.True(path.First.direction);
//             Assert.NotEqual(0, path.Offset1);
//             Assert.NotEqual(ushort.MaxValue, path.Offset2);
//         }
//         
//         [Fact]
//         public void MapMatcher_OneEdge_OffsetTrack_BiggerThanEdge_ShouldMatchEdge()
//         {
//             // build tiny network.
//             var routerDb = new RouterDb();
//             var v1 = routerDb.AddVertex(4.801357984542847, 51.26779566943717);
//             var v2 = routerDb.AddVertex(4.801028072834015, 51.26654370936813);
//             var edge = routerDb.AddEdge(v1, v2, attributes: new []  { ("highway", "residential") });
//             
//             // build track.
//             var track = new Track(new []
//             {
//                 new TrackPoint(routerDb.GetVertex(v1).OffsetWithDistanceY(5)),
//                 new TrackPoint(routerDb.GetVertex(v2).OffsetWithDistanceY(-5)),
//             });
//             
//             // match track.
//             var result = routerDb.Matcher(Profiles.Lua.Osm.OsmProfiles.Bicycle)
//                 .Match(track);
//
//             Assert.NotNull(result);
//             Assert.Single(result.Value);
//             var path = result.Value[0];
//             Assert.NotNull(path);
//             Assert.Equal(edge, path.First.edge);
//             Assert.True(path.First.direction);
//             Assert.Equal(0, path.Offset1);
//             Assert.Equal(ushort.MaxValue, path.Offset2);
//         }
//     }
// }
