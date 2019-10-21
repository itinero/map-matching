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
    public class MapMatcherResult
    {
        private readonly RouterDb _routerDb;
        
        internal MapMatcherResult(RouterDb routerDb, Track source, Profile profile, RouterPoint[] routerPoints, 
            IReadOnlyList<EdgePath<float>> rawPaths)
        {
            _routerDb = routerDb;
            
            Source = source;
            Profile = profile;
            RawPaths = rawPaths;

            this.RouterPoints = routerPoints;
        }
        
        /// <summary>
        /// The source track.
        /// </summary>
        public Track Source { get; }
        
        /// <summary>
        /// Gets the profile used for matching.
        /// </summary>
        public Profile Profile { get; }
        
        /// <summary>
        /// Gets the raw paths.
        /// </summary>
        public IReadOnlyList<EdgePath<float>> RawPaths { get; }
        
        /// <summary>
        /// The chosen projection points.
        /// </summary>
        public RouterPoint[] RouterPoints { get; }

        /// <summary>
        /// Gives lines that connect the original points with their chosen projection on the road network.
        /// </summary>
        public IEnumerable<Line> ChosenProjectionLines()
        {
            var lines = new Line[RouterPoints.Length];
            for (var i = 0; i < RouterPoints.Length; i++)
            {
                lines[i] = new Line(RouterPoints[i].LocationOnNetwork(_routerDb), RouterPoints[i].Location());
            }
            return lines;
        }
    }
}
