using System;
using System.Collections.Generic;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class EnergyFeatures
    {
        public float MeanVelocity { get; private set; }
        public int PeakVelocity { get; private set; }
        public float VelocityVariance { get; private set; }
        public IReadOnlyList<float> SegmentMeanVelocities { get; private set; }
        public bool IsHighEnergy { get; private set; }
        public bool IsLowEnergy { get; private set; }
        public bool IsFlatEnergy { get; private set; }
        public bool IsAccented { get; private set; }
        public bool IsCrescendo { get; private set; }
        public bool IsDecrescendo { get; private set; }

        public EnergyFeatures()
            : this(0f, 0, 0f, null, false, true, false, false, false, false)
        {
        }

        public EnergyFeatures(
            float meanVelocity,
            int peakVelocity,
            float velocityVariance,
            IReadOnlyList<float> segmentMeanVelocities,
            bool isHighEnergy,
            bool isLowEnergy,
            bool isFlatEnergy,
            bool isAccented,
            bool isCrescendo,
            bool isDecrescendo)
        {
            MeanVelocity = meanVelocity;
            PeakVelocity = peakVelocity;
            VelocityVariance = velocityVariance;
            SegmentMeanVelocities = CopyOrDefault(segmentMeanVelocities);
            IsHighEnergy = isHighEnergy;
            IsLowEnergy = isLowEnergy;
            IsFlatEnergy = isFlatEnergy;
            IsAccented = isAccented;
            IsCrescendo = isCrescendo;
            IsDecrescendo = isDecrescendo;
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
