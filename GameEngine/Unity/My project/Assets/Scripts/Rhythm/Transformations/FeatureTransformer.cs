using System;
using System.Collections.Generic;
using UnityEngine;
using IT4s.Data;

namespace IT4s.Rhythm.Transformations
{
    public static class FeatureTransformer
    {
        public enum Mode
        {
            Auto,
            EchoAccent,
            EndFill,
            SparseOrnament
        }

        public struct PatternFeatures
        {
            public int stepCount;
            public int activeSteps;
            public float density;
            public int maxVelocity;
            public float meanVelocity;
            public List<int> onsetIndices;
        }

        public static PatternTurn Transform(PatternTurn src, Mode mode = Mode.Auto)
        {
            PatternFeatures features = Analyse(src);

            return mode switch
            {
                Mode.EchoAccent => EchoAccent(src, features),
                Mode.EndFill => EndFill(src, features),
                Mode.SparseOrnament => SparseOrnament(src, features),
                Mode.Auto => AutoTransform(src, features),
                _ => src
            };
        }

        public static PatternFeatures Analyse(PatternTurn pattern)
        {
            var onsets = new List<int>();   // list of step indices where hits occur
            int active = 0;                 // number of hits
            int maxVel = 0;                 
            int velSum = 0;

        
            for (int i = 0; i < pattern.StepCount; i++)
            {
                int vel = pattern.velocity[i];
                if (vel > 0)
                {
                    active++;
                    onsets.Add(i);
                    velSum += vel;

                    if (vel > maxVel)
                        maxVel = vel;
                }
            }

            // calculate density and mean velocity without dividing by zero
            float density = pattern.StepCount > 0
                ? (float)active / pattern.StepCount
                : 0f;            
            float meanVelocity = active > 0
                ? (float)velSum / active
                : 0f;


            return new PatternFeatures
            {
                stepCount = pattern.StepCount,
                activeSteps = active,
                density = density,
                maxVelocity = maxVel,
                meanVelocity = meanVelocity,
                onsetIndices = onsets
            };
        }

        private static PatternTurn AutoTransform(PatternTurn pattern, PatternFeatures features)
        {
            if (features.activeSteps == 0)
                return pattern;

            if (features.density < 0.12f)
                return SparseOrnament(pattern, features);

            if (features.density < 0.30f)
                return EchoAccent(pattern, features);

            return EndFill(pattern, features);
        }

        private static PatternTurn EchoAccent(PatternTurn pattern, PatternFeatures features)
        {
            var output = ClonePattern(pattern);

            int accentThreshold = Mathf.RoundToInt(features.maxVelocity * 0.8f);

            for (int i = 0; i < pattern.StepCount; i++)
            {
                int vel = pattern.velocity[i];
                if (vel <= 0) continue;
                if (vel < accentThreshold) continue;

                int echoIndex = i + 3;
                if (echoIndex >= pattern.StepCount) continue;

                if (output.velocity[echoIndex] == 0)
                {
                    output.velocity[echoIndex] = Mathf.RoundToInt(vel * 0.7f);
                    output.offsetSamples[echoIndex] = 0;
                }
            }

            return output;
        }

        private static PatternTurn EndFill(PatternTurn pattern, PatternFeatures features)
        {
            var output = ClonePattern(pattern);

            int start = Mathf.FloorToInt(pattern.StepCount * 0.75f);
            int activeInEnd = 0;

            for (int i = start; i < pattern.StepCount; i++)
            {
                if (output.velocity[i] > 0)
                    activeInEnd++;
            }

            if (activeInEnd <= 2)
            {
                int[] fillSteps = { pattern.StepCount - 6, pattern.StepCount - 3, pattern.StepCount - 1 };

                foreach (int step in fillSteps)
                {
                    if (step >= 0 && step < pattern.StepCount && output.velocity[step] == 0)
                    {
                        output.velocity[step] = Mathf.RoundToInt(Mathf.Max(60f, features.meanVelocity));
                        output.offsetSamples[step] = 0;
                    }
                }
            }

            return output;
        }

        private static PatternTurn SparseOrnament(PatternTurn pattern, PatternFeatures features)
        {
            var output = ClonePattern(pattern);

            foreach (int onset in features.onsetIndices)
            {
                int ornamentStep = onset + 1;
                if (ornamentStep >= pattern.StepCount) continue;

                if (output.velocity[ornamentStep] == 0)
                {
                    output.velocity[ornamentStep] = Mathf.RoundToInt(pattern.velocity[onset] * 0.6f);
                    output.offsetSamples[ornamentStep] = 0;
                }
            }

            return output;
        }

        private static PatternTurn ClonePattern(PatternTurn src)
        {
            PatternTurn clone = src;
            clone.velocity = new int[src.velocity.Length];
            clone.offsetSamples = new int[src.offsetSamples.Length];

            Array.Copy(src.velocity, clone.velocity, src.velocity.Length);
            Array.Copy(src.offsetSamples, clone.offsetSamples, src.offsetSamples.Length);

            return clone;
        }
    }
}