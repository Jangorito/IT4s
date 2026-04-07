using System;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;
using IT4s.Rhythm.TurnAnalysis.Utils;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class DensityAnalyser : IAnalyser<DensityFeatures>
    {
        public DensityFeatures Analyze(PatternTurn pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            int stepCount = pattern.StepCount;
            int activeStepCount = 0;

            for (int i = 0; i < stepCount; i++)
            {
                if (pattern.velocity[i] > 0)
                    activeStepCount++;
            }

            int inactiveStepCount = stepCount - activeStepCount;
            float stepDensity = stepCount > 0
                ? (float)activeStepCount / stepCount
                : 0f;

            var segmentDensities = new float[SegmentHelper.SegmentCount];

            for (int segmentIndex = 0; segmentIndex < SegmentHelper.SegmentCount; segmentIndex++)
            {
                SegmentHelper.GetSegmentBounds(
                    stepCount,
                    segmentIndex,
                    out int startInclusive,
                    out int endExclusive);

                int segmentActive = 0;
                for (int i = startInclusive; i < endExclusive; i++)
                {
                    if (pattern.velocity[i] > 0)
                        segmentActive++;
                }

                int segmentLength = endExclusive - startInclusive;
                segmentDensities[segmentIndex] = segmentLength > 0
                    ? (float)segmentActive / segmentLength
                    : 0f;
            }

            return new DensityFeatures(
                stepCount,
                activeStepCount,
                inactiveStepCount,
                stepDensity,
                segmentDensities);
        }
    }
}
