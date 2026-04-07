using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class TurnAnalysisResult
    {
        public DensityFeatures Density { get; private set; }
        public EnergyFeatures Energy { get; private set; }
        public AnchorFeatures Anchor { get; private set; }
        public EndActivityFeatures EndActivity { get; private set; }
        public ActivityShape DensityShape { get; private set; }
        public ActivityShape EnergyShape { get; private set; }

        public TurnAnalysisResult()
            : this(
                new DensityFeatures(),
                new EnergyFeatures(),
                new AnchorFeatures(),
                new EndActivityFeatures(),
                ActivityShape.Flat,
                ActivityShape.Flat)
        {
        }

        public TurnAnalysisResult(
            DensityFeatures density,
            EnergyFeatures energy,
            AnchorFeatures anchor,
            EndActivityFeatures endActivity,
            ActivityShape densityShape,
            ActivityShape energyShape)
        {
            Density = density ?? new DensityFeatures();
            Energy = energy ?? new EnergyFeatures();
            Anchor = anchor ?? new AnchorFeatures();
            EndActivity = endActivity ?? new EndActivityFeatures();
            DensityShape = densityShape;
            EnergyShape = energyShape;
        }
    }
}
