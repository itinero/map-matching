using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Itinero.MapMatching
{
    internal static class Solver
    {
        public static (int[], float) ForwardViterbi(
                Dictionary<int /* state */, float> startProbs,
                /* key: observation */ Dictionary<int /* to state */, Dictionary<int /* from state */, float>>[] transProbs,
                /* key: observation */ Dictionary<int /* state */, float>[] emitProbs)
        {
            var numObs = emitProbs.Length;

            //                               ↓ observation   ↓ state
            var probs      = new Dictionary<int, Dictionary<int, float>>();
            var prevStates = new Dictionary<int, Dictionary<int, int>>();

            probs.Add(0, new Dictionary<int, float>());
            prevStates.Add(0, new Dictionary<int, int>());
            foreach (var (state, startProb) in startProbs)
            {
                probs[0][state] = startProb;
            }

            // skip first observation because it has no predecessor
            for (var observation = 1; observation < numObs; observation++)
            {
                probs.Add(observation, new Dictionary<int, float>());
                prevStates.Add(observation, new Dictionary<int, int>());
                foreach (var (state, emitProb) in emitProbs[observation])
                {
                    //Console.WriteLine("obs {0,4}   state {1,4}", observation, state);

                    var argmax = 0; // to determine: best predecessor…
                    var max = float.NegativeInfinity; // …and probability of the sequence if we choose that predecessor

                    var stateToStateToProb = transProbs[observation];
                    var stateToProb = stateToStateToProb[state];
                    foreach (var (prevState, transProb) in stateToProb)
                    {
                        var prevStateProb = probs[observation - 1][prevState];
                        var prob = prevStateProb + transProb + emitProb;

                        //Console.WriteLine("  prev state {0,4} ({1})   transition prob {2}  emit prob {3}", prevState, prevStateProb, transProb, emitProb);

                        if (!(prob > max)) continue;
                        
                        max = prob;
                        argmax = prevState;
                    }

                    probs[observation][state] = max;
                    prevStates[observation][state] = argmax;
                }
            }

            // determine most probable path
            var lastObs = numObs - 1;
            var mostProbableEndpoint = 0;
            var mostProbableProb = float.NegativeInfinity;
            foreach (var (state, prob) in probs[lastObs])
            {
                if (!(prob > mostProbableProb)) continue; 
                
                mostProbableProb = prob;
                mostProbableEndpoint = state;
            }

            // backtrack: build most probable path
            var path = new int[numObs];
            path[lastObs] = mostProbableEndpoint;
            for (var observation = lastObs; observation > 0; observation--)
            {
                path[observation - 1] = prevStates[observation][path[observation]];
            }

            return (path, mostProbableProb);
        }
    }
}
