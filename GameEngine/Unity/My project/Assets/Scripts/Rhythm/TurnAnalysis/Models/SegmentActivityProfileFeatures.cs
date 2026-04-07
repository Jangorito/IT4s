using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class SegmentActivityProfileFeatures
    {
        public ActivityShape DensityShape { get; private set; }
        public ActivityShape EnergyShape { get; private set; }

        public SegmentActivityProfileFeatures()
            : this(ActivityShape.Flat, ActivityShape.Flat)
        {
        }

        public SegmentActivityProfileFeatures(
            ActivityShape densityShape,
            ActivityShape energyShape)
        {
            DensityShape = densityShape;
            EnergyShape = energyShape;
        }
    }
}
