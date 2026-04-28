namespace IT4s.Rhythm.ResponsePlanning
{
    public sealed class ResponsePlannerConfig
    {
        public float SparseDensityThreshold { get; set; } = 0.25f;
        public float VerySparseDensityThreshold { get; set; } = 0.18f;
        public float BusyDensityThreshold { get; set; } = 0.40f;
        public float ConversationalSpaceDensityThreshold { get; set; } = 0.45f;
        public float CongestedDensityThreshold { get; set; } = 0.35f;

        public float LowEnergyThreshold { get; set; } = 0.35f;
        public float HighEnergyThreshold { get; set; } = 0.70f;
        public float PressurisedDensityThreshold { get; set; } = 0.30f;

        public int MeaningfulAnchorCountThreshold { get; set; } = 1;
        public float MeaningfulAnchorScoreThreshold { get; set; } = 0.65f;

        public float StrongEndingDensityThreshold { get; set; } = 0.50f;
        public float StrongEndingEnergyThreshold { get; set; } = 0.60f;
        public float StrongEndingAccentThreshold { get; set; } = 0.75f;

        public float OpenEndingDensityThreshold { get; set; } = 0.20f;
        public float OpenEndingEnergyThreshold { get; set; } = 0.30f;
        public float OpenEndingAccentThreshold { get; set; } = 0.35f;

        public float MinTargetDensity { get; set; } = 0.10f;
        public float MaxTargetDensity { get; set; } = 0.80f;
        public float MaxTargetDensityDelta { get; set; } = 0.22f;

        public float DensityPrimaryScore { get; set; } = 2.5f;
        public float EnergyPrimaryScore { get; set; } = 2.0f;
        public float AnchorRefinementScore { get; set; } = 1.0f;
        public float EndingRefinementScore { get; set; } = 1.25f;
        public float ProfileRefinementScore { get; set; } = 0.75f;
        public float ConversationalSpaceScore { get; set; } = 1.0f;
        public float CongestionScore { get; set; } = 1.0f;
        public float PressurisedScore { get; set; } = 1.5f;
        public float PredictableProfileScore { get; set; } = 1.0f;

        public float DensityContextAdjustment { get; set; } = 0.05f;
        public float ComplementarityAdjustment { get; set; } = 0.05f;
        public float ScoreTieMargin { get; set; } = 0.25f;
    }
}
