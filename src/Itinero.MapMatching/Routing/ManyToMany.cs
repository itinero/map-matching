using System.Collections.Generic;
using Itinero.Algorithms.Weights;
using Itinero.Profiles;

namespace Itinero.MapMatching.Routing
{
    internal static class ManyToMany
    {
        /// <summary>
        /// Calculates many to many directed routes between the given source and targets.
        /// </summary>
        /// <param name="router">The router.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="sources">The sources.</param>
        /// <param name="targets">The targets.</param>
        /// <param name="maxSearchDistance">The maximum search distance.</param>
        /// <returns>A weight matrix of directed weights.</returns>
        public static float[][] CalculateManyToManyDirected(this Router router, Profile profile, IReadOnlyList<RouterPoint> sources, IReadOnlyList<RouterPoint> targets,
            float maxSearchDistance)
        {
            var weights = new float[sources.Count * 2][];
            
            var weightHandler = router.GetDefaultWeightHandler(profile);
            var settings = new RoutingSettings<float>()
            {
                DirectionAbsolute = true,
            };
            settings.SetMaxSearch(profile.FullName, maxSearchDistance);
            
            for (var s = 0; s < sources.Count; s++)
            {
                var source = sources[s];
                
                weights[s * 2 + 0] = new float[targets.Count * 2];
                weights[s * 2 + 1] = new float[targets.Count * 2];
                for (var t = 0; t < targets.Count; t++)
                {
                    var target = targets[t];
                    
                    weights[s * 2 + 0][t * 2 + 0] = router.CalculateRawWeight(profile, weightHandler, source, true, target, true, settings);
                    weights[s * 2 + 0][t * 2 + 1] = router.CalculateRawWeight(profile, weightHandler, source, true, target, false, settings);
                    weights[s * 2 + 1][t * 2 + 0] = router.CalculateRawWeight(profile, weightHandler, source, false, target, true, settings);
                    weights[s * 2 + 1][t * 2 + 1] = router.CalculateRawWeight(profile, weightHandler, source, false, target, false, settings);
                }
            }

            return weights;
        }

        private static float CalculateRawWeight(this Router router, IProfileInstance profileInstance, WeightHandler<float> weightHandler,
            RouterPoint source, bool? sourceForward, RouterPoint target, bool? targetForward, RoutingSettings<float> settings)
        {     
            var ff = router.TryCalculateRaw(profileInstance, weightHandler, source, sourceForward, target, targetForward, settings);
            var ffWeight = float.MaxValue;
            if (!ff.IsError)
            {
                ffWeight = ff.Value.Weight;
            }

            return ffWeight;
        }
    }
}