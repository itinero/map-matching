using System.Collections.Generic;

namespace Itinero.MapMatching.Model
{
    internal static class TrackModelRouting
    {
        /// <summary>
        /// Calculates the best path through the track model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The track with the lowest cost.</returns>
        public static IReadOnlyList<(int track, int point, bool arrival, bool departure)> BestPath(this TrackModel model)
        {
            // search the shortest path from the start to end.
            
            
            return null;
        }
    }
}