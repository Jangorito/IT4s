using System;
using System.Collections.Generic;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;
using IT4s.Rhythm.TurnAnalysis.Utils;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class AnchorAnalyser : IAnalyser<AnchorFeatures>
    {
        private const float VELOCITY_WEIGHT = 0.40f;
        private const float ACCENT_WEIGHT = 0.30f;
        private const float ISOLATION_WEIGHT = 0.20f;
        private const float POSITION_WEIGHT = 0.10f;

        private const float ANCHOR_THRESHOLD = 0.55f;
        private const int MAX_VELOCITY = 127;
        private const int LOCAL_RADIUS = 2;
        private const int SUPPORT_RADIUS = 2;

        private const float STRONGEST_SUPPORT_WEIGHT = 1f;
        private const float SECONDARY_SUPPORT_WEIGHT = 0.7f;
        private const float WEAK_SUPPORT_WEIGHT = 0.35f;
        private const float MINIMAL_SUPPORT_WEIGHT = 0.15f;
        private const float SUPPORTED_WEAK_BONUS = 0.25f;

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
                    POSITION_WEIGHT * PositionalScore(i, firstActiveIndex, lastActiveIndex);

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
            SupportMetrics supportMetrics = ComputeSupportMetrics(pattern, stepCount);

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
                anchorCountsPerSegment,
                supportMetrics.AverageSupport,
                supportMetrics.StrongHitRatio,
                supportMetrics.SupportedWeakHitRatio,
                supportMetrics.UnsupportedWeakHitRatio);
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

            for (int i = stepIndex - LOCAL_RADIUS; i <= stepIndex + LOCAL_RADIUS; i++)
            {
                if (i == stepIndex)
                    continue;

                if (i < 0 || i >= stepCount)
                {
                    inactiveCount++;
                    continue;
                }

                if (pattern.velocity[i] <= 0)
                    inactiveCount++;
            }

            return inactiveCount / 4f;
        }

        private static float PositionalScore(int stepIndex, int firstActiveIndex, int lastActiveIndex)
        {
            if (firstActiveIndex < 0 || lastActiveIndex < 0)
                return 0f;

            return stepIndex == firstActiveIndex || stepIndex == lastActiveIndex
                ? 1f
                : 0f;
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

        private static SupportMetrics ComputeSupportMetrics(PatternTurn pattern, int stepCount)
        {
            int activeHitCount = 0;
            int strongHitCount = 0;
            int supportedWeakHitCount = 0;
            int unsupportedWeakHitCount = 0;
            float supportSum = 0f;

            for (int i = 0; i < stepCount; i++)
            {
                if (pattern.velocity[i] <= 0)
                    continue;

                activeHitCount++;

                float positionalWeight = GetPositionalSupportWeight(i, stepCount, pattern.stepsPerQuarter);
                if (IsStrongPosition(i, stepCount, pattern.stepsPerQuarter))
                {
                    strongHitCount++;
                    supportSum += positionalWeight;
                    continue;
                }

                bool hasSupport = HasNearbyStrongerActiveHit(pattern, i, stepCount, positionalWeight);
                if (hasSupport)
                    supportedWeakHitCount++;
                else
                    unsupportedWeakHitCount++;

                float supportScore = positionalWeight + (hasSupport ? SUPPORTED_WEAK_BONUS : 0f);
                supportSum += Clamp01(supportScore);
            }

            if (activeHitCount == 0)
                return new SupportMetrics(0f, 0f, 0f, 0f);

            float activeHitRatio = 1f / activeHitCount;
            return new SupportMetrics(
                supportSum * activeHitRatio,
                strongHitCount * activeHitRatio,
                supportedWeakHitCount * activeHitRatio,
                unsupportedWeakHitCount * activeHitRatio);
        }

        private static float GetPositionalSupportWeight(int stepIndex, int stepCount, int stepsPerQuarter)
        {
            if (stepCount <= 0)
                return 0f;

            int quarterSubdivision = GetQuarterSubdivision(stepCount, stepsPerQuarter);
            if (stepIndex == 0)
                return STRONGEST_SUPPORT_WEIGHT;

            if (quarterSubdivision <= 0)
                return MINIMAL_SUPPORT_WEIGHT;

            if (stepIndex % quarterSubdivision == 0)
                return SECONDARY_SUPPORT_WEIGHT;

            if (quarterSubdivision % 2 == 0)
            {
                int halfQuarterSubdivision = quarterSubdivision / 2;
                if (halfQuarterSubdivision > 0 && stepIndex % halfQuarterSubdivision == 0)
                    return WEAK_SUPPORT_WEIGHT;
            }

            return MINIMAL_SUPPORT_WEIGHT;
        }

        private static bool IsStrongPosition(int stepIndex, int stepCount, int stepsPerQuarter)
        {
            return GetPositionalSupportWeight(stepIndex, stepCount, stepsPerQuarter) >= SECONDARY_SUPPORT_WEIGHT;
        }

        private static bool HasNearbyStrongerActiveHit(
            PatternTurn pattern,
            int stepIndex,
            int stepCount,
            float currentWeight)
        {
            int start = Math.Max(0, stepIndex - SUPPORT_RADIUS);
            int end = Math.Min(stepCount - 1, stepIndex + SUPPORT_RADIUS);

            for (int i = start; i <= end; i++)
            {
                if (i == stepIndex || pattern.velocity[i] <= 0)
                    continue;

                float neighbourWeight = GetPositionalSupportWeight(i, stepCount, pattern.stepsPerQuarter);
                if (neighbourWeight > currentWeight)
                    return true;
            }

            return false;
        }

        private static int GetQuarterSubdivision(int stepCount, int stepsPerQuarter)
        {
            if (stepsPerQuarter > 0)
                return stepsPerQuarter;

            if (stepCount > 0 && stepCount % SegmentHelper.SegmentCount == 0)
                return Math.Max(1, stepCount / SegmentHelper.SegmentCount);

            return 0;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }

        private struct SupportMetrics
        {
            public readonly float AverageSupport;
            public readonly float StrongHitRatio;
            public readonly float SupportedWeakHitRatio;
            public readonly float UnsupportedWeakHitRatio;

            public SupportMetrics(
                float averageSupport,
                float strongHitRatio,
                float supportedWeakHitRatio,
                float unsupportedWeakHitRatio)
            {
                AverageSupport = averageSupport;
                StrongHitRatio = strongHitRatio;
                SupportedWeakHitRatio = supportedWeakHitRatio;
                UnsupportedWeakHitRatio = unsupportedWeakHitRatio;
            }
        }
    }
}
