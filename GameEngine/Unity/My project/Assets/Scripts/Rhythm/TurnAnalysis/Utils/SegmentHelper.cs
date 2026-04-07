using System;

namespace IT4s.Rhythm.TurnAnalysis.Utils
{
    public static class SegmentHelper
    {
        public const int SegmentCount = 4;

        public static void GetSegmentBounds(
            int stepCount,
            int segmentIndex,
            out int startInclusive,
            out int endExclusive)
        {
            if (stepCount < 0)
                throw new ArgumentOutOfRangeException(nameof(stepCount), "Step count cannot be negative.");

            if (segmentIndex < 0 || segmentIndex >= SegmentCount)
                throw new ArgumentOutOfRangeException(nameof(segmentIndex), "Segment index must be between 0 and 3.");

            int baseSize = stepCount / SegmentCount;
            int remainder = stepCount % SegmentCount;

            startInclusive = 0;
            for (int i = 0; i < segmentIndex; i++)
            {
                startInclusive += baseSize + (i < remainder ? 1 : 0);
            }

            int segmentLength = baseSize + (segmentIndex < remainder ? 1 : 0);
            endExclusive = startInclusive + segmentLength;
        }
    }
}
