using System;
using System.Collections.Generic;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;
using IT4s.Rhythm.TurnAnalysis.Utils;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class AnchorAnalyser : IAnalyser<AnchorFeatures>
    {
        private const float VELOCITY_WEIGHT = 0.30f;
        private const float ACCENT_WEIGHT = 0.20f;
        private const float ISOLATION_WEIGHT = 0.15f;
        private const float METRICAL_WEIGHT = 0.25f;
        private const float PHRASE_ROLE_WEIGHT = 0.10f;

        private const float ANCHOR_THRESHOLD = 0.55f;
        private const int MAX_VELOCITY = 127;
        private const int LOCAL_RADIUS = 2;

        public AnchorFeatures Analyze(PatternTurn pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            int stepCount = pattern.StepCount;
            var salienceScores = new float[stepCount];
            var anchorFlags = new bool[stepCount];
            var anchorIndices = new List<int>();

            FindActiveBoundaries(pattern, stepCount, out int firstActiveIndex, out int lastActiveIndex);

            int? strongestAnchorIndex = null;
            float strongestAnchorScore = 0f;

            for (int i = 0; i < stepCount; i++)
            {
                int velocity = pattern.velocity[i];
                if (velocity <= 0)
                    continue;

                float salience =
                    VELOCITY_WEIGHT * VelocityScore(velocity) +
                    ACCENT_WEIGHT * LocalAccentScore(pattern, stepCount, i) +
                    ISOLATION_WEIGHT * IsolationScore(pattern, stepCount, i) +
                    METRICAL_WEIGHT * MetricalWeightScore(i, stepCount) +
                    PHRASE_ROLE_WEIGHT * PhraseRoleScore(i, stepCount, firstActiveIndex, lastActiveIndex);

                salienceScores[i] = salience;

                if (salience < ANCHOR_THRESHOLD)
                    continue;

                anchorFlags[i] = true;
                anchorIndices.Add(i);

                if (!strongestAnchorIndex.HasValue || salience > strongestAnchorScore)
                {
                    strongestAnchorIndex = i;
                    strongestAnchorScore = salience;
                }
            }

            int[] anchorCountsPerSegment = CountAnchorsPerSegment(anchorFlags, stepCount);

            return new AnchorFeatures(
                stepCount,
                salienceScores,
                anchorFlags,
                anchorIndices.Count,
                anchorIndices,
                strongestAnchorIndex,
                strongestAnchorIndex.HasValue ? strongestAnchorScore : 0f,
                firstActiveIndex >= 0 && anchorFlags[firstActiveIndex],
                lastActiveIndex >= 0 && anchorFlags[lastActiveIndex],
                anchorCountsPerSegment);
        }

        private static void FindActiveBoundaries(
            PatternTurn pattern,
            int stepCount,
            out int firstActiveIndex,
            out int lastActiveIndex)
        {
            firstActiveIndex = -1;
            lastActiveIndex = -1;

            for (int i = 0; i < stepCount; i++)
            {
                if (pattern.velocity[i] <= 0)
                    continue;

                if (firstActiveIndex < 0)
                    firstActiveIndex = i;

                lastActiveIndex = i;
            }
        }

        private static float VelocityScore(int velocity)
        {
            return Clamp01((float)velocity / MAX_VELOCITY);
        }

        private static float LocalAccentScore(PatternTurn pattern, int stepCount, int stepIndex)
        {
            float sum = 0f;
            int count = 0;

            for (int i = stepIndex - LOCAL_RADIUS; i <= stepIndex + LOCAL_RADIUS; i++)
            {
                if (i == stepIndex)
                    continue;

                if (i < 0 || i >= stepCount)
                    continue;

                int velocity = pattern.velocity[i];
                if (velocity > 0)
                {
                    sum += velocity;
                    count++;
                }
            }

            if (count == 0)
                return 0f;

            float localMean = sum / count;
            float score = (pattern.velocity[stepIndex] - localMean) / MAX_VELOCITY;
            return Clamp01(score);
        }

        private static float IsolationScore(PatternTurn pattern, int stepCount, int stepIndex)
        {
            int inactiveCount = 0;
            int totalNeighbourSlots = 0;

            for (int i = stepIndex - LOCAL_RADIUS; i <= stepIndex + LOCAL_RADIUS; i++)
            {
                if (i == stepIndex)
                    continue;

                totalNeighbourSlots++;

                if (i < 0 || i >= stepCount)
                {
                    inactiveCount++;
                    continue;
                }

                if (pattern.velocity[i] <= 0)
                    inactiveCount++;
            }

            if (totalNeighbourSlots == 0)
                return 0f;

            return inactiveCount / (float)totalNeighbourSlots;
        }

        private static float MetricalWeightScore(int stepIndex, int stepCount)
        {
            int stepsPerBar = stepCount == 96 ? 48 : stepCount;
            int stepsPerBeat = stepsPerBar / 4;

            if (stepIndex % stepsPerBar == 0)
                return 1.0f;

            if (stepIndex % stepsPerBeat == 0)
                return 0.75f;

            if (stepsPerBeat % 2 == 0 && stepIndex % (stepsPerBeat / 2) == 0)
                return 0.45f;

            return 0.20f;
        }

        private static float PhraseRoleScore(int stepIndex, int stepCount, int firstActiveIndex, int lastActiveIndex)
        {
            if (stepIndex == firstActiveIndex || stepIndex == lastActiveIndex)
                return 1.0f;

            bool isOneBar = stepCount == 48;
            bool isTwoBar = stepCount == 96;

            if (firstActiveIndex >= 0)
            {
                int openingStart = Math.Max(firstActiveIndex, 0);
                int openingEnd = Math.Min(firstActiveIndex + 11, stepCount - 1);
                if (stepIndex >= openingStart && stepIndex <= openingEnd)
                    return 0.75f;
            }

            if (lastActiveIndex >= 0)
            {
                int closingStart = Math.Max(lastActiveIndex - 11, 0);
                int closingEnd = Math.Min(lastActiveIndex, stepCount - 1);
                if (stepIndex >= closingStart && stepIndex <= closingEnd)
                    return 0.75f;
            }

            if (isOneBar && stepIndex >= 24 && stepIndex <= 35)
                return 0.45f;

            if (isTwoBar && stepIndex >= 48 && stepIndex <= 59)
                return 0.45f;

            return 0.0f;
        }

        private static int[] CountAnchorsPerSegment(bool[] anchorFlags, int stepCount)
        {
            var anchorCounts = new int[SegmentHelper.SegmentCount];

            for (int segmentIndex = 0; segmentIndex < SegmentHelper.SegmentCount; segmentIndex++)
            {
                SegmentHelper.GetSegmentBounds(
                    stepCount,
                    segmentIndex,
                    out int startInclusive,
                    out int endExclusive);

                int segmentAnchors = 0;
                for (int i = startInclusive; i < endExclusive; i++)
                {
                    if (anchorFlags[i])
                        segmentAnchors++;
                }

                anchorCounts[segmentIndex] = segmentAnchors;
            }

            return anchorCounts;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }
}
