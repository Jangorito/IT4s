using System;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal sealed class SkeletonEndingMap
    {
        public bool[] EndingRegion { get; private set; }
        public int[] StepsFromEnd { get; private set; }

        public SkeletonEndingMap(bool[] endingRegion, int[] stepsFromEnd)
        {
            EndingRegion = endingRegion;
            StepsFromEnd = stepsFromEnd;
        }
    }

    internal static class SkeletonEndingRegionHelper
    {
        public static SkeletonEndingMap BuildEndingMap(int turnLengthSteps, int stepsPerQuarter)
        {
            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            if (stepsPerQuarter <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepsPerQuarter), "Steps per quarter must be greater than zero.");

            var endingRegion = new bool[turnLengthSteps];
            var stepsFromEnd = new int[turnLengthSteps];

            // V1 uses the final quarter-note window as the ending-sensitive region.
            int windowLength = Math.Min(turnLengthSteps, stepsPerQuarter);
            int windowStart = turnLengthSteps - windowLength;

            for (int i = 0; i < turnLengthSteps; i++)
                stepsFromEnd[i] = turnLengthSteps - 1 - i;

            for (int i = windowStart; i < turnLengthSteps; i++)
                endingRegion[i] = true;

            return new SkeletonEndingMap(endingRegion, stepsFromEnd);
        }
    }
}
