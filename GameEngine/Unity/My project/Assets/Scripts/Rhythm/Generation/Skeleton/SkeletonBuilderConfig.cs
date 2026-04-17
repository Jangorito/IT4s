namespace IT4s.Rhythm.Generation.Skeleton
{
    public sealed class SkeletonBuilderConfig
    {
        public float MetricStrengthWeight { get; set; } = 0.88f;
        public float MirrorWeight { get; set; } = 1.00f;
        public float ComplementWeight { get; set; } = 1.00f;
        public float AnchorInfluenceWeight { get; set; } = 1.00f;
        public float EndingInfluenceWeight { get; set; } = 1.00f;
        public float StrongBeatPreferenceWeight { get; set; } = 0.85f;
        public float SourceRelationScale { get; set; } = 0.32f;
        public float SourceRelationMaxMagnitude { get; set; } = 0.40f;
        public float HighDensityWeakBoost { get; set; } = 0.08f;
        public float HighDensityMediumBoost { get; set; } = 0.06f;
        public int MinimumStepSpacing { get; set; } = 1;
        public int MaximumClusterSize { get; set; } = 3;
        public float DensityTolerance { get; set; } = 0.05f;
        public float SelectionJitter { get; set; } = 0.00f;
        public bool AllowOffbeatClusters { get; set; } = false;
        public bool RebalanceAcrossSegments { get; set; } = true;
    }
}
