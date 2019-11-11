using Itinero.LocalGeo;
using System;
using System.Collections.Generic;
using Itinero.Algorithms;
using Itinero.Profiles;

namespace Itinero.MapMatching
{
    /// <summary>
    /// Represents the result of a matching.
    /// </summary>
    public class MapMatch
    {
        private readonly RouterDb _routerDb;
        private readonly IReadOnlyList<MapMatchPath> _paths;

        internal MapMatch(RouterDb routerDb, Track source, Profile profile, RouterPoint[] routerPoints, 
            IReadOnlyList<EdgePath<float>> rawPaths)
        {
            _routerDb = routerDb;
            
            Source = source;
            Profile = profile;

            // build matched paths.
            var paths = new List<MapMatchPath>();
            for (var i = 0; i < routerPoints.Length - 1; i++)
            {
                paths.Add(routerDb.ToMapMatchPath(rawPaths[i], routerPoints[i], routerPoints[i + 1]));
            }
            _paths = paths;
             
            this.Path = _paths.Merge();
            this.RouterPoints = routerPoints;
        }
        
        /// <summary>
        /// The source track.
        /// </summary>
        internal Track Source { get; }
        
        /// <summary>
        /// Gets the profile used for matching.
        /// </summary>
        public Profile Profile { get; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public MapMatchPath Path { get; }
        
        /// <summary>
        /// Gets the matched segments.
        /// </summary>
        public IReadOnlyList<MapMatchSegment> Segments { get; }

        /// <summary>
        /// Gets the number of routes in this result.
        /// </summary>
        public int Count => _paths.Count;
        
        /// <summary>
        /// Gets the matched route at the given index.
        /// </summary>
        /// <param name="i">The index.</param>
        public MapMatchPath this[int i] => _paths[i];
        
        /// <summary>
        /// The snapping points.
        /// </summary>
        public RouterPoint[] RouterPoints { get; }

//        /// <summary>
//        /// Gives lines that connect the original points with their chosen projection on the road network.
//        /// </summary>
//        public IEnumerable<Line> ChosenProjectionLines()
//        {
//            var lines = new Line[RouterPoints.Length];
//            for (var i = 0; i < RouterPoints.Length; i++)
//            {
//                lines[i] = new Line(RouterPoints[i].LocationOnNetwork(_routerDb), RouterPoints[i].Location());
//            }
//            return lines;
//        }
    }
}
