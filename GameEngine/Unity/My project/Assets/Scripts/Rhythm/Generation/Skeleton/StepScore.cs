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
        public bool SourceOccupied { get; set; }
        public bool AdjacentToSource { get; set; }
        public bool NearSource { get; set; }
        public bool InterstitialSourceGap { get; set; }
        public int DistanceToNearestSource { get; set; }
        public int LocalSourceDensity { get; set; }
        public float SegmentTargetWeight { get; set; } = 1f;
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
