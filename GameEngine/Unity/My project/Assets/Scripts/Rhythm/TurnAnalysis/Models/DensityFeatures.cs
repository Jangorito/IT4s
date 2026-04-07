using System;
using System.Collections.Generic;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class DensityFeatures
    {
        public int StepCount { get; private set; }
        public int ActiveStepCount { get; private set; }
        public int InactiveStepCount { get; private set; }
        public float StepDensity { get; private set; }
        public IReadOnlyList<float> SegmentDensities { get; private set; }

        public DensityFeatures()
            : this(0, 0, 0, 0f, null)
        {
        }

        public DensityFeatures(
            int stepCount,
            int activeStepCount,
            int inactiveStepCount,
            float stepDensity,
            IReadOnlyList<float> segmentDensities)
        {
            StepCount = stepCount;
            ActiveStepCount = activeStepCount;
            InactiveStepCount = inactiveStepCount;
            StepDensity = stepDensity;
            SegmentDensities = CopyOrDefault(segmentDensities);
        }

        private static IReadOnlyList<float> CopyOrDefault(IReadOnlyList<float> values)
        {
            var copy = new float[4];
            if (values == null)
                return Array.AsReadOnly(copy);

            int count = Math.Min(copy.Length, values.Count);
            for (int i = 0; i < count; i++)
                copy[i] = values[i];

            return Array.AsReadOnly(copy);
        }
    }
}
