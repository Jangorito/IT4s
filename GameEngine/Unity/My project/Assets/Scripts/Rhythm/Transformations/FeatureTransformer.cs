using System;
using System.Collections.Generic;
using UnityEngine;
using IT4s.Data;
using IT4s.Diagnostics;

namespace IT4s.Rhythm.Transformations
{
    /// <summary>
    /// Rhythmic transformation collaborator used to derive a machine response from a human pattern.
    /// Its public API is instance-based so higher-level controllers can treat it as a real dependency
    /// rather than a static utility. Private helper methods remain static where that keeps the
    /// transformation logic simple, but callers now work with a FeatureTransformer object that can
    /// later be injected by the orchestration layer without renaming or redesigning this class.
    /// </summary>
    public class FeatureTransformer
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

            // Gap detection fields
            public List<int> gapAfterOnset;
            public int maxGap;
            public float meanGap;
        }

        /// <summary>
        /// Derives a transformed pattern from the source material using the requested mode.
        /// Making this instance-based supports the TurnLoopController architecture, where the
        /// transformer should appear as an explicit collaborator in orchestration.
        /// </summary>
        public PatternTurn Transform(PatternTurn src, Mode mode = Mode.Auto)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src), "FeatureTransformer requires a compiled source PatternTurn.");

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

        /// <summary>
        /// Extracts lightweight rhythmic features used by the transformation rules.
        /// This is also instance-based so callers can depend on a transformer object consistently.
        /// </summary>
        public PatternFeatures Analyse(PatternTurn pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern), "FeatureTransformer cannot analyse a null PatternTurn.");

            if (pattern.velocity == null)
                throw new ArgumentException("PatternTurn.velocity must be populated before transformation.", nameof(pattern));

            if (pattern.offsetSamples == null)
                throw new ArgumentException("PatternTurn.offsetSamples must be populated before transformation.", nameof(pattern));

            var onsets = new List<int>();   // list of step indices where hits occur
            var gaps = new List<int>();     // list of gap lengths between hits

            int active = 0;                 // number of hits
            int maxVel = 0;
            int velSum = 0;
            int maxGap = 0;
            int gapSum = 0;

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

            // Calculate gap lengths
            for (int i = 0; i < onsets.Count - 1; i++)
            {
                int gap = onsets[i + 1] - onsets[i] - 1;
                gaps.Add(Mathf.Max(0, gap));
            }

            if (onsets.Count > 0)
            {
                int trailingGap = pattern.StepCount - 1 - onsets[onsets.Count - 1];
                gaps.Add(Mathf.Max(0, trailingGap));
            }

            // calculate density and mean velocity without dividing by zero
            float density = pattern.StepCount > 0
                ? (float)active / pattern.StepCount
                : 0f;
            float meanVelocity = active > 0
                ? (float)velSum / active
                : 0f;

            foreach (int gap in gaps)
            {
                gapSum += gap;
                if (gap > maxGap)
                    maxGap = gap;
            }

            float meanGap = gaps.Count > 0
                ? (float)gapSum / gaps.Count
                : 0f;

            return new PatternFeatures
            {
                stepCount = pattern.StepCount,
                activeSteps = active,
                density = density,
                maxVelocity = maxVel,
                meanVelocity = meanVelocity,
                onsetIndices = onsets,
                gapAfterOnset = gaps,
                maxGap = maxGap,
                meanGap = meanGap
            };
        }

        private static PatternTurn AutoTransform(PatternTurn pattern, PatternFeatures features)
        {
            if (features.activeSteps == 0){
                RuntimeDebugLog.Log("[FeatureTransformer] No active steps detected; returning original pattern.");
                return ClonePattern(pattern);
            }
            if (features.maxGap < 6){
                RuntimeDebugLog.Log("[FeatureTransformer] Detected dense pattern; applying sparse ornamentation.");
                return SparseOrnament(pattern, features);
            }
            if (features.meanGap < 3f){
                RuntimeDebugLog.Log("[FeatureTransformer] Detected sparse pattern; applying echo accent transformation.");
                return EchoAccent(pattern, features);
        
            }           
            
            RuntimeDebugLog.Log("[FeatureTransformer] Detected moderate density; applying end fill transformation."); 
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

            for (int i = 0; i < features.onsetIndices.Count; i++)
            {
                int onset = features.onsetIndices[i];
                int gapAfter = i < features.gapAfterOnset.Count ? features.gapAfterOnset[i] : 0;

                // Skip dense hits
                if (gapAfter < 2)
                    continue;

                int ornamentStep = onset + 1;
                if (ornamentStep >= pattern.StepCount)
                    continue;

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
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            var clone = new PatternTurn
            {
                turnId = src.turnId,
                bpm = src.bpm,
                stepsPerQuarter = src.stepsPerQuarter,
                sampleRate = src.sampleRate,
                startSamples = src.startSamples,
                endSamples = src.endSamples,
                velocity = src.velocity != null ? new int[src.velocity.Length] : null,
                offsetSamples = src.offsetSamples != null ? new int[src.offsetSamples.Length] : null
            };

            if (src.velocity != null)
            {
                Array.Copy(src.velocity, clone.velocity, src.velocity.Length);
            }

            if (src.offsetSamples != null)
            {
                Array.Copy(src.offsetSamples, clone.offsetSamples, src.offsetSamples.Length);
            }

            return clone;
        }
    }
}
