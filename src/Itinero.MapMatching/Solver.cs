using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Itinero.MapMatching
{
    class Solver
    {
        public static (uint[], float) ForwardViterbi(
                Dictionary<uint /* state */, float> startProbs,
                /* key: observation */ Dictionary<uint /* to state */, Dictionary<uint /* from state */, float>>[] transProbs,
                /* key: observation */ Dictionary<uint /* state */, float>[] emitProbs)
        {
            uint numObs = (uint)emitProbs.LongLength;

            //                               ↓ observation    ↓ state
            var probs      = new Dictionary<uint, Dictionary<uint, float>>();
            var prevStates = new Dictionary<uint, Dictionary<uint, uint >>();

            probs.Add(0, new Dictionary<uint, float>());
            prevStates.Add(0, new Dictionary<uint, uint>());
            foreach (var item in startProbs)
            {
                var state     = item.Key;
                var startProb = item.Value;

                probs[0][state] = startProb;
            }

            // skip first observation because it has no predecessor
            for (uint observation = 1; observation < numObs; observation++)
            {
                probs.Add(observation, new Dictionary<uint, float>());
                prevStates.Add(observation, new Dictionary<uint, uint>());
                foreach (var item in emitProbs[observation])
                {
                    uint state = item.Key;
                    float emitProb = item.Value;

                    Console.WriteLine("obs {0,4}   state {1,4}", observation, state);

                    uint argmax = 0; // to determine: best predecessor…
                    float max = float.NegativeInfinity; // …and probability of the sequence if we choose that predecessor

                    var stateToStateToProb = transProbs[observation];
                    var stateToProb = stateToStateToProb[state];
                    foreach (var item2 in stateToProb)
                    {
                        uint prevState = item2.Key;
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
            uint lastObs = numObs - 1;
            uint mostProbableEndpoint = 0;
            float mostProbableProb = float.NegativeInfinity;
            foreach (var item in probs[lastObs])
            {
                uint state = item.Key;
                float prob = item.Value;

                if (prob > mostProbableProb)
                {
                    mostProbableProb = prob;
                    mostProbableEndpoint = state;
                }
            }

            // backtrack: build most probable path
            var path = new uint[numObs];
            path[lastObs] = mostProbableEndpoint;
            for (uint observation = lastObs; observation > 0; observation--)
            {
                path[observation - 1] = prevStates[observation][path[observation]];
            }

            return (path, mostProbableProb);
        }
    }
}
