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
        public SegmentActivityProfileFeatures SegmentActivityProfile { get; private set; }

        public TurnAnalysisResult()
            : this(
                new DensityFeatures(),
                new EnergyFeatures(),
                new AnchorFeatures(),
                new EndActivityFeatures(),
                new SegmentActivityProfileFeatures())
        {
        }

        public TurnAnalysisResult(
            DensityFeatures density,
            EnergyFeatures energy,
            AnchorFeatures anchor,
            EndActivityFeatures endActivity,
            SegmentActivityProfileFeatures segmentActivityProfile)
        {
            Density = density ?? new DensityFeatures();
            Energy = energy ?? new EnergyFeatures();
            Anchor = anchor ?? new AnchorFeatures();
            EndActivity = endActivity ?? new EndActivityFeatures();
            SegmentActivityProfile = segmentActivityProfile ?? new SegmentActivityProfileFeatures();
        }
    }
}
