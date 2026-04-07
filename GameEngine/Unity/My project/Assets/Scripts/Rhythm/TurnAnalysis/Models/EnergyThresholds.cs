using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class EnergyThresholds
    {
        public float HighEnergyMeanVelocity { get; private set; }
        public float LowEnergyMeanVelocity { get; private set; }
        public float FlatVarianceThreshold { get; private set; }
        public float AccentPeakOverMeanThreshold { get; private set; }
        public float CrescendoMinDelta { get; private set; }
        public float DecrescendoMinDelta { get; private set; }

        public EnergyThresholds(
            float highEnergyMeanVelocity,
            float lowEnergyMeanVelocity,
            float flatVarianceThreshold,
            float accentPeakOverMeanThreshold,
            float crescendoMinDelta,
            float decrescendoMinDelta)
        {
            HighEnergyMeanVelocity = highEnergyMeanVelocity;
            LowEnergyMeanVelocity = lowEnergyMeanVelocity;
            FlatVarianceThreshold = flatVarianceThreshold;
            AccentPeakOverMeanThreshold = accentPeakOverMeanThreshold;
            CrescendoMinDelta = crescendoMinDelta;
            DecrescendoMinDelta = decrescendoMinDelta;
        }
    }
}
