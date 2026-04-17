using System;

namespace IT4s.Rhythm.Generation.Skeleton.Models
{
    [Serializable]
    public sealed class SkeletonStepMeta
    {
        public int StepIndex { get; set; }
        public int SegmentIndex { get; set; }
        public bool IsStrongBeat { get; set; }
        public int StepsFromEnd { get; set; }
        public float MetricScore { get; set; }
        public float SourceRelationScore { get; set; }
        public float AnchorScore { get; set; }
        public float EndingScore { get; set; }
        public float PhraseBalanceScore { get; set; }
        public float DensityShapingScore { get; set; }
        public float SpacingPenalty { get; set; }
        public float JitterOffset { get; set; }
        public float RawScore { get; set; }
        public float FinalScore { get; set; }
        public bool Selected { get; set; }
        public bool SourceOccupied { get; set; }
        public bool SourceGap { get; set; }
        public bool SourceAnchor { get; set; }
        public bool IsExplicitAnchor { get; set; }
        public bool IsFallbackAnchor { get; set; }
        public bool InEndingRegion { get; set; }
        public bool AdjacentToSource { get; set; }
        public bool NearSource { get; set; }
        public bool InterstitialSourceGap { get; set; }
        public int DistanceToNearestSourceHit { get; set; }
        public int PreviousSourceHitDistance { get; set; }
        public int NextSourceHitDistance { get; set; }
        public int LocalSourceDensity { get; set; }
        public int MetricStrengthLevel { get; set; }
        public float SegmentSourceWeight { get; set; }
        public bool Protected { get; set; }
        public SkeletonReasonFlags ReasonFlags { get; set; }
    }
}
