using System;
using IT4s.Rhythm.Generation.Skeleton.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    [Serializable]
    public sealed class StepScore
    {
        public int StepIndex { get; set; }
        public float Score { get; set; }
        public SkeletonReasonFlags Flags { get; set; }
        public float MetricalWeight { get; set; }
        public float SourceRelationScore { get; set; }
        public bool IsExplicitAnchor { get; set; }
        public bool IsFallbackAnchor { get; set; }
        public bool InEndingRegion { get; set; }
        public bool IsWeakMetrical { get; set; }
        public int SegmentIndex { get; set; } = -1;

        public int stepIndex
        {
            get { return StepIndex; }
            set { StepIndex = value; }
        }

        public float score
        {
            get { return Score; }
            set { Score = value; }
        }

        public SkeletonReasonFlags flags
        {
            get { return Flags; }
            set { Flags = value; }
        }

        public float sourceRelationScore
        {
            get { return SourceRelationScore; }
            set { SourceRelationScore = value; }
        }
    }
}
