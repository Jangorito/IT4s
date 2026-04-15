namespace IT4s.Rhythm.ResponsePlanning
{
    public sealed class ResponsePlannerConfig
    {
        public float SparseDensityThreshold { get; set; } = 0.35f;
        public float BusyDensityThreshold { get; set; } = 0.70f;
        public float ConversationalSpaceDensityThreshold { get; set; } = 0.60f;
        public float CongestedDensityThreshold { get; set; } = 0.75f;

        public float LowEnergyThreshold { get; set; } = 0.35f;
        public float HighEnergyThreshold { get; set; } = 0.70f;

        public int MeaningfulAnchorCountThreshold { get; set; } = 1;
        public float MeaningfulAnchorScoreThreshold { get; set; } = 0.60f;

        public float StrongEndingDensityThreshold { get; set; } = 0.50f;
        public float StrongEndingEnergyThreshold { get; set; } = 0.60f;
        public float StrongEndingAccentThreshold { get; set; } = 0.75f;

        public float OpenEndingDensityThreshold { get; set; } = 0.20f;
        public float OpenEndingEnergyThreshold { get; set; } = 0.30f;
        public float OpenEndingAccentThreshold { get; set; } = 0.35f;

        public float MinTargetDensity { get; set; } = 0.10f;
        public float MaxTargetDensity { get; set; } = 0.80f;

        public float DensityContextAdjustment { get; set; } = 0.05f;
        public float ComplementarityAdjustment { get; set; } = 0.05f;
        public float ScoreTieMargin { get; set; } = 0.001f;
    }
}
