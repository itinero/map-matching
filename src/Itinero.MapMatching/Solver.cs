using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Itinero.MapMatching
{
    class Solver
    {
        public static (int[], float) ForwardViterbi(
                Dictionary<int /* state */, float> startProbs,
                /* key: observation */ Dictionary<int /* to state */, Dictionary<int /* from state */, float>>[] transProbs,
                /* key: observation */ Dictionary<int /* state */, float>[] emitProbs)
        {
            int numObs = emitProbs.Length;

            //                               ↓ observation   ↓ state
            var probs      = new Dictionary<int, Dictionary<int, float>>();
            var prevStates = new Dictionary<int, Dictionary<int, int>>();

            probs.Add(0, new Dictionary<int, float>());
            prevStates.Add(0, new Dictionary<int, int>());
            foreach (var item in startProbs)
            {
                var state     = item.Key;
                var startProb = item.Value;

                probs[0][state] = startProb;
            }

            // skip first observation because it has no predecessor
            for (int observation = 1; observation < numObs; observation++)
            {
                probs.Add(observation, new Dictionary<int, float>());
                prevStates.Add(observation, new Dictionary<int, int>());
                foreach (var item in emitProbs[observation])
                {
                    int state = item.Key;
                    float emitProb = item.Value;

                    Console.WriteLine("obs {0,4}   state {1,4}", observation, state);

                    int argmax = 0; // to determine: best predecessor…
                    float max = float.NegativeInfinity; // …and probability of the sequence if we choose that predecessor

                    var stateToStateToProb = transProbs[observation];
                    var stateToProb = stateToStateToProb[state];
                    foreach (var item2 in stateToProb)
                    {
                        int prevState = item2.Key;
                        float transProb = item2.Value;

                        float prevStateProb = probs[observation - 1][prevState];

                        float prob = prevStateProb + transProb + emitProb;

                        Console.WriteLine("  prev state {0,4} ({1})   transition prob {2}  emit prob {3}", prevState, prevStateProb, transProb, emitProb);

                        if (prob > max) {
                            max = prob;
                            argmax = prevState;
                        }
                    }

                    probs[observation][state] = max;
                    prevStates[observation][state] = argmax;
                }
            }

            // determine most probable path
            int lastObs = numObs - 1;
            int mostProbableEndpoint = 0;
            float mostProbableProb = float.NegativeInfinity;
            foreach (var item in probs[lastObs])
            {
                int state = item.Key;
                float prob = item.Value;

                if (prob > mostProbableProb)
                {
                    mostProbableProb = prob;
                    mostProbableEndpoint = state;
                }
            }

            // backtrack: build most probable path
            var path = new int[numObs];
            path[lastObs] = mostProbableEndpoint;
            for (int observation = lastObs; observation > 0; observation--)
            {
                path[observation - 1] = prevStates[observation][path[observation]];
            }

            return (path, mostProbableProb);
        }
    }
}
