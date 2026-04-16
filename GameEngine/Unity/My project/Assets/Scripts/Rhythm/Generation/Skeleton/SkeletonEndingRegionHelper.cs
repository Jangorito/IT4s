using System;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal static class SkeletonEndingRegionHelper
    {
        public static bool[] BuildEndingRegionMap(int turnLengthSteps, int stepsPerQuarter)
        {
            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            if (stepsPerQuarter <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepsPerQuarter), "Steps per quarter must be greater than zero.");

            var endingRegion = new bool[turnLengthSteps];

            // V1 uses the final quarter-note window as the ending-sensitive region.
            int windowLength = Math.Min(turnLengthSteps, stepsPerQuarter);
            int windowStart = turnLengthSteps - windowLength;

            for (int i = windowStart; i < turnLengthSteps; i++)
                endingRegion[i] = true;

            return endingRegion;
        }
    }
}
