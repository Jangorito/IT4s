using System;
using IT4s.Rhythm.TurnAnalysis.Utils;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal static class SkeletonSegmentMapBuilder
    {
        public static int[] BuildStepToSegmentMap(int turnLengthSteps)
        {
            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            var stepToSegment = new int[turnLengthSteps];

            // Keep v1 coarse and compatible with existing turn-analysis summaries:
            // four near-even segments from the shared SegmentHelper.
            for (int segmentIndex = 0; segmentIndex < SegmentHelper.SegmentCount; segmentIndex++)
            {
                SegmentHelper.GetSegmentBounds(
                    turnLengthSteps,
                    segmentIndex,
                    out int startInclusive,
                    out int endExclusive);

                for (int stepIndex = startInclusive; stepIndex < endExclusive; stepIndex++)
                    stepToSegment[stepIndex] = segmentIndex;
            }

            return stepToSegment;
        }
    }
}
