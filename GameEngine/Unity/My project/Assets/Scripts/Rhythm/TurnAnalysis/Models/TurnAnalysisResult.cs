using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class TurnAnalysisResult
    {
        public DensityFeatures Density = new DensityFeatures();
        public EnergyFeatures Energy = new EnergyFeatures();
        public AnchorFeatures Anchor = new AnchorFeatures();
        public EndActivityFeatures EndActivity = new EndActivityFeatures();
        public ActivityShape DensityShape = ActivityShape.Flat;
        public ActivityShape EnergyShape = ActivityShape.Flat;
    }
}
