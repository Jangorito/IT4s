using System;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;
using IT4s.Rhythm.TurnAnalysis.Utils;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class EnergyAnalyser : IAnalyser<EnergyFeatures>
    {
        private readonly EnergyThresholds thresholds;

        public EnergyAnalyser(EnergyThresholds thresholds)
        {
            this.thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }

        public EnergyFeatures Analyze(PatternTurn pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            int stepCount = pattern.StepCount;
            int activeCount = 0;
            int peakVelocity = 0;
            float sum = 0f;

            for (int i = 0; i < stepCount; i++)
            {
                int velocity = pattern.velocity[i];
                if (velocity <= 0)
                    continue;

                activeCount++;
                sum += velocity;

                if (velocity > peakVelocity)
                    peakVelocity = velocity;
            }

            float meanVelocity = activeCount > 0
                ? sum / activeCount
                : 0f;

            float velocityVariance = CalculateVelocityVariance(pattern, stepCount, activeCount, meanVelocity);
            float[] segmentMeanVelocities = CalculateSegmentMeanVelocities(pattern, stepCount);

            if (activeCount == 0)
            {
                return new EnergyFeatures(
                    meanVelocity,
                    peakVelocity,
                    velocityVariance,
                    segmentMeanVelocities,
                    false,
                    true,
                    false,
                    false,
                    false,
                    false);
            }

            bool isHighEnergy = meanVelocity >= thresholds.HighEnergyMeanVelocity;
            bool isLowEnergy = meanVelocity <= thresholds.LowEnergyMeanVelocity;
            bool isFlatEnergy = velocityVariance <= thresholds.FlatVarianceThreshold;
            bool isAccented = (peakVelocity - meanVelocity) >= thresholds.AccentPeakOverMeanThreshold;

            float segmentDelta = segmentMeanVelocities[SegmentHelper.SegmentCount - 1] - segmentMeanVelocities[0];
            bool isCrescendo = segmentDelta >= thresholds.CrescendoMinDelta;
            bool isDecrescendo = segmentDelta <= -thresholds.DecrescendoMinDelta;

            return new EnergyFeatures(
                meanVelocity,
                peakVelocity,
                velocityVariance,
                segmentMeanVelocities,
                isHighEnergy,
                isLowEnergy,
                isFlatEnergy,
                isAccented,
                isCrescendo,
                isDecrescendo);
        }

        private static float CalculateVelocityVariance(
            PatternTurn pattern,
            int stepCount,
            int activeCount,
            float meanVelocity)
        {
            if (activeCount == 0)
                return 0f;

            float varianceSum = 0f;

            for (int i = 0; i < stepCount; i++)
            {
                int velocity = pattern.velocity[i];
                if (velocity <= 0)
                    continue;

                float delta = velocity - meanVelocity;
                varianceSum += delta * delta;
            }

            return varianceSum / activeCount;
        }

        private static float[] CalculateSegmentMeanVelocities(PatternTurn pattern, int stepCount)
        {
            var segmentMeanVelocities = new float[SegmentHelper.SegmentCount];

            for (int segmentIndex = 0; segmentIndex < SegmentHelper.SegmentCount; segmentIndex++)
            {
                SegmentHelper.GetSegmentBounds(
                    stepCount,
                    segmentIndex,
                    out int startInclusive,
                    out int endExclusive);

                float segmentSum = 0f;
                int segmentActiveCount = 0;

                for (int i = startInclusive; i < endExclusive; i++)
                {
                    int velocity = pattern.velocity[i];
                    if (velocity <= 0)
                        continue;

                    segmentSum += velocity;
                    segmentActiveCount++;
                }

                segmentMeanVelocities[segmentIndex] = segmentActiveCount > 0
                    ? segmentSum / segmentActiveCount
                    : 0f;
            }

            return segmentMeanVelocities;
        }
    }
}
