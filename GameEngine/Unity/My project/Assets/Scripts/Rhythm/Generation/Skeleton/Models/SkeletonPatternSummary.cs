using System;

namespace IT4s.Rhythm.Generation.Skeleton.Models
{
    [Serializable]
    public sealed class SkeletonPatternSummary
    {
        public int ActiveCount { get; private set; }
        public float AchievedDensity { get; private set; }
        public int SourceOverlapCount { get; private set; }
        public int AnchorAlignedCount { get; private set; }
        public bool DensityTargetMet { get; private set; }
        public bool UsedStochasticTieBreak { get; private set; }

        public SkeletonPatternSummary(
            int activeCount,
            float achievedDensity,
            int sourceOverlapCount,
            int anchorAlignedCount,
            bool densityTargetMet,
            bool usedStochasticTieBreak)
        {
            ActiveCount = activeCount;
            AchievedDensity = achievedDensity;
            SourceOverlapCount = sourceOverlapCount;
            AnchorAlignedCount = anchorAlignedCount;
            DensityTargetMet = densityTargetMet;
            UsedStochasticTieBreak = usedStochasticTieBreak;
        }
    }
}
