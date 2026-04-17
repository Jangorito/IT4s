using System;
using System.Collections.Generic;
using IT4s.Rhythm.ResponsePlanning.Models;

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
        public ResponseType ResponseType { get; set; }

        public bool[] ExplicitAnchors { get; set; }
        public bool[] FallbackAnchors { get; set; }
        public bool[] EndingSteps { get; set; }
        public bool[] WeakMetricalSteps { get; set; }
        public float[] MetricalWeights { get; set; }
        public float[] SourceRelationScores { get; set; }
        public int[] SegmentByStep { get; set; }
        public IReadOnlyList<int> ForcedEndingSteps
        {
            get
            {
                if (forcedEndingSteps == null)
                    forcedEndingSteps = new List<int>();

                return forcedEndingSteps.AsReadOnly();
            }
        }

        private List<int> forcedEndingSteps;

        public void RecordForcedEndingStep(int stepIndex)
        {
            if (forcedEndingSteps == null)
                forcedEndingSteps = new List<int>();

            if (!forcedEndingSteps.Contains(stepIndex))
                forcedEndingSteps.Add(stepIndex);
        }

        public void ClearForcedEndingSteps()
        {
            if (forcedEndingSteps != null)
                forcedEndingSteps.Clear();
        }

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

        public ResponseType responseType
        {
            get { return ResponseType; }
            set { ResponseType = value; }
        }
    }
}
