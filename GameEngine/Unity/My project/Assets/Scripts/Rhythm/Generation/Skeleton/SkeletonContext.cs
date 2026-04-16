using System;

namespace IT4s.Rhythm.Generation.Skeleton
{
    [Serializable]
    public sealed class SkeletonContext
    {
        public float TargetDensity { get; set; }
        public int TotalSteps { get; set; }
        public int MinSpacingSteps { get; set; }
        public bool PreserveAnchors { get; set; }
        public bool RequireStrongEnding { get; set; }
        public int EndingWindowSteps { get; set; }
        public bool RebalanceAcrossSegments { get; set; }
        public float DensityTolerance { get; set; }

        public bool[] ExplicitAnchors { get; set; }
        public bool[] FallbackAnchors { get; set; }
        public bool[] EndingSteps { get; set; }
        public bool[] WeakMetricalSteps { get; set; }
        public float[] MetricalWeights { get; set; }
        public int[] SegmentByStep { get; set; }

        public float targetDensity
        {
            get { return TargetDensity; }
            set { TargetDensity = value; }
        }

        public int totalSteps
        {
            get { return TotalSteps; }
            set { TotalSteps = value; }
        }

        public int minSpacingSteps
        {
            get { return MinSpacingSteps; }
            set { MinSpacingSteps = value; }
        }

        public bool preserveAnchors
        {
            get { return PreserveAnchors; }
            set { PreserveAnchors = value; }
        }

        public bool requireStrongEnding
        {
            get { return RequireStrongEnding; }
            set { RequireStrongEnding = value; }
        }
    }
}
