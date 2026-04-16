using System;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal sealed class SkeletonMetricMap
    {
        public float[] Salience { get; private set; }
        public bool[] StrongBeats { get; private set; }

        public SkeletonMetricMap(float[] salience, bool[] strongBeats)
        {
            Salience = salience;
            StrongBeats = strongBeats;
        }
    }

    internal static class SkeletonMetricMapBuilder
    {
        private const int BeatsPerBar = 4;

        public static SkeletonMetricMap Build(int turnLengthSteps, int stepsPerQuarter)
        {
            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            if (stepsPerQuarter <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepsPerQuarter), "Steps per quarter must be greater than zero.");

            var salience = new float[turnLengthSteps];
            var strongBeats = new bool[turnLengthSteps];

            for (int stepIndex = 0; stepIndex < turnLengthSteps; stepIndex++)
            {
                salience[stepIndex] = GetMetricalSalience(stepIndex, stepsPerQuarter);
                strongBeats[stepIndex] = IsStrongBeat(stepIndex, stepsPerQuarter);
            }

            return new SkeletonMetricMap(salience, strongBeats);
        }

        private static float GetMetricalSalience(int stepIndex, int stepsPerQuarter)
        {
            int stepsPerBar = stepsPerQuarter * BeatsPerBar;

            if (stepIndex % stepsPerBar == 0)
                return 1.00f;

            if (stepIndex % stepsPerQuarter == 0)
                return 0.85f;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 2))
                return 0.65f;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 3))
                return 0.55f;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 4))
                return 0.45f;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 6))
                return 0.35f;

            return 0.20f;
        }

        private static bool IsStrongBeat(int stepIndex, int stepsPerQuarter)
        {
            return stepIndex % stepsPerQuarter == 0;
        }

        private static bool IsSubdivision(int stepIndex, int stepsPerQuarter, int divisor)
        {
            if (stepsPerQuarter % divisor != 0)
                return false;

            int subdivision = stepsPerQuarter / divisor;
            return subdivision > 0 && stepIndex % subdivision == 0;
        }
    }
}
