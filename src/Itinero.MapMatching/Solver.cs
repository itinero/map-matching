using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Itinero.MapMatching
{
    public delegate float[][] TransitionProbabilities(int fromObservation, int toObservation);

    class Solver
    {
        public static (int[], float) ForwardViterbi(
                int numObs,
                float[] startProbs,
                float[][] emitProbs,
                TransitionProbability transProbs)
        {
            if (numObs == 0)
            {
                return (new int[], 0.0f);
            }

            //                               ↓ observation   ↓ state
            var probs      = new Dictionary<int, Dictionary<int, float>>();
            var prevStates = new Dictionary<int, Dictionary<int, int>>();

            probs.Add(0, new Dictionary<int, float>());
            prevStates.Add(0, new Dictionary<int, int>());
            probs[0] = startProbs;

            // skip first observation because it has no predecessor
            for (int observation = 1; observation < numObs; observation++)
            {
                Console.WriteLine("obs {0,4}", observation, state);

                probs.Add(observation, new Dictionary<int, float>());
                prevStates.Add(observation, new Dictionary<int, int>());

                float transProb = transProbs(observation, observation - 1);

                for (int toState = 0; toState < emitProbs[observation].Length; i++)
                {
                    float emitProb = emitProbs[observation][toState];

                    //Console.WriteLine("obs {0,4}   state {1,4}", observation, state);

                    int argmax = 0; // to determine: best predecessor…
                    float max = float.NegativeInfinity; // …and probability of the sequence if we choose that predecessor

                    for (int fromState = 0; fromState < emitProbs[observation - 1].Length; i++)
                    {

                        float prevStateProb = probs[observation - 1][prevState];

                        float prob = prevStateProb + transProb + emitProb;

                        //Console.WriteLine("  prev state {0,4} ({1})   transition prob {2}  emit prob {3}", prevState, prevStateProb, transProb, emitProb);

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
