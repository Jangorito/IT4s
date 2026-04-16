using System;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal enum MetricStrengthLevel
    {
        Strongest,
        Strong,
        Medium,
        Weak
    }

    internal sealed class SkeletonMetricMap
    {
        public float[] Salience { get; private set; }
        public bool[] StrongBeats { get; private set; }
        public MetricStrengthLevel[] StrengthLevels { get; private set; }

        public SkeletonMetricMap(
            float[] salience,
            bool[] strongBeats,
            MetricStrengthLevel[] strengthLevels)
        {
            Salience = salience;
            StrongBeats = strongBeats;
            StrengthLevels = strengthLevels;
        }
    }

    internal static class SkeletonMetricMapBuilder
    {
        public static SkeletonMetricMap Build(int turnLengthSteps, int stepsPerQuarter)
        {
            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            if (stepsPerQuarter <= 0)
                throw new ArgumentOutOfRangeException(nameof(stepsPerQuarter), "Steps per quarter must be greater than zero.");

            var salience = new float[turnLengthSteps];
            var strongBeats = new bool[turnLengthSteps];
            var strengthLevels = new MetricStrengthLevel[turnLengthSteps];

            for (int stepIndex = 0; stepIndex < turnLengthSteps; stepIndex++)
            {
                MetricStrengthLevel strengthLevel = GetStrengthLevel(stepIndex, stepsPerQuarter);
                strengthLevels[stepIndex] = strengthLevel;
                salience[stepIndex] = GetMetricalSalience(strengthLevel);
                strongBeats[stepIndex] = strengthLevel == MetricStrengthLevel.Strongest;
            }

            return new SkeletonMetricMap(salience, strongBeats, strengthLevels);
        }

        private static MetricStrengthLevel GetStrengthLevel(int stepIndex, int stepsPerQuarter)
        {
            if (stepIndex % stepsPerQuarter == 0)
                return MetricStrengthLevel.Strongest;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 2))
                return MetricStrengthLevel.Strong;

            if (IsSubdivision(stepIndex, stepsPerQuarter, 4))
                return MetricStrengthLevel.Medium;

            return MetricStrengthLevel.Weak;
        }

        private static float GetMetricalSalience(MetricStrengthLevel strengthLevel)
        {
            switch (strengthLevel)
            {
                case MetricStrengthLevel.Strongest: return 1.00f;
                case MetricStrengthLevel.Strong: return 0.75f;
                case MetricStrengthLevel.Medium: return 0.50f;
                case MetricStrengthLevel.Weak: return 0.20f;
                default: throw new ArgumentOutOfRangeException(nameof(strengthLevel), strengthLevel, "Unknown metric strength level.");
            }
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
