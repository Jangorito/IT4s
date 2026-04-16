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
        public float SpacingPenalty { get; set; }
        public float JitterOffset { get; set; }
        public float FinalScore { get; set; }
        public bool Selected { get; set; }
        public bool SourceOccupied { get; set; }
        public bool SourceAnchor { get; set; }
        public bool IsExplicitAnchor { get; set; }
        public bool IsFallbackAnchor { get; set; }
        public bool InEndingRegion { get; set; }
        public bool Protected { get; set; }
        public SkeletonReasonFlags ReasonFlags { get; set; }
    }
}
