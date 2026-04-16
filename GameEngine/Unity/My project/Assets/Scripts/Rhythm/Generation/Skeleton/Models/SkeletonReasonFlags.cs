using System;

namespace IT4s.Rhythm.Generation.Skeleton.Models
{
    [Flags]
    public enum SkeletonReasonFlags
    {
        None = 0,
        MetricStrong = 1 << 0,
        MetricWeak = 1 << 1,
        MirrorBoosted = 1 << 2,
        ComplementBoosted = 1 << 3,
        AnchorBoosted = 1 << 4,
        EndingBoosted = 1 << 5,
        SpacingSuppressed = 1 << 6,
        SelectedByTieBreak = 1 << 7,
        ProtectedAnchor = 1 << 8
    }
}
