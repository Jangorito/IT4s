namespace IT4s.Rhythm.Generation.Skeleton
{
    public sealed class SkeletonBuilderConfig
    {
        public float MetricStrengthWeight { get; set; } = 1.00f;
        public float MirrorWeight { get; set; } = 1.00f;
        public float ComplementWeight { get; set; } = 1.00f;
        public float AnchorInfluenceWeight { get; set; } = 1.00f;
        public float EndingInfluenceWeight { get; set; } = 1.00f;
        public float StrongBeatPreferenceWeight { get; set; } = 1.00f;
        public int MinimumStepSpacing { get; set; } = 1;
        public int MaximumClusterSize { get; set; } = 3;
        public float DensityTolerance { get; set; } = 0.05f;
        public float SelectionJitter { get; set; } = 0.00f;
        public bool AllowOffbeatClusters { get; set; } = false;
        public bool RebalanceAcrossSegments { get; set; } = true;
    }
}
