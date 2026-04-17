using System;
using System.Collections.Generic;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    [Serializable]
    public sealed class SkeletonDebugSnapshot
    {
        public SkeletonDebugSnapshot(SkeletonPattern pattern, ResponsePlan plan)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));

            TurnLengthSteps = pattern.TurnLengthSteps;
            ActiveSteps = Copy(pattern.ActiveSteps);
            SelectedStepIndices = Copy(pattern.SelectedStepIndices);
            StepAnnotations = CopyAnnotations(pattern.StepMeta);
            SelectedStepCount = SelectedStepIndices.Count;
            TargetDensity = plan != null ? plan.TargetDensity : 0f;
            PreserveAnchors = plan != null && plan.PreserveAnchors;
            RequireStrongEnding = plan != null && plan.MirrorEnding;

            SkeletonPatternSummary summary = pattern.Summary;
            if (summary == null)
            {
                ActiveCount = SelectedStepCount;
                AchievedDensity = TurnLengthSteps > 0 ? ActiveCount / (float)TurnLengthSteps : 0f;
                SourceOverlapCount = 0;
                AnchorAlignedCount = 0;
                DensityTargetMet = false;
                return;
            }

            ActiveCount = summary.ActiveCount;
            AchievedDensity = summary.AchievedDensity;
            SourceOverlapCount = summary.SourceOverlapCount;
            AnchorAlignedCount = summary.AnchorAlignedCount;
            DensityTargetMet = summary.DensityTargetMet;
        }

        public int TurnLengthSteps { get; private set; }
        public IReadOnlyList<bool> ActiveSteps { get; private set; }
        public IReadOnlyList<int> SelectedStepIndices { get; private set; }
        public IReadOnlyList<SkeletonStepDebugAnnotation> StepAnnotations { get; private set; }
        public int SelectedStepCount { get; private set; }
        public int ActiveCount { get; private set; }
        public float TargetDensity { get; private set; }
        public float AchievedDensity { get; private set; }
        public bool PreserveAnchors { get; private set; }
        public bool RequireStrongEnding { get; private set; }
        public int SourceOverlapCount { get; private set; }
        public int AnchorAlignedCount { get; private set; }
        public bool DensityTargetMet { get; private set; }

        public bool IsActiveStep(int stepIndex)
        {
            return HasIndex(ActiveSteps, stepIndex) && ActiveSteps[stepIndex];
        }

        public SkeletonStepDebugAnnotation GetAnnotation(int stepIndex)
        {
            return HasIndex(StepAnnotations, stepIndex)
                ? StepAnnotations[stepIndex]
                : SkeletonStepDebugAnnotation.Empty(stepIndex);
        }

        private static bool HasIndex<T>(IReadOnlyList<T> values, int index)
        {
            return values != null && index >= 0 && index < values.Count;
        }

        private static IReadOnlyList<bool> Copy(bool[] values)
        {
            if (values == null || values.Length == 0)
                return Array.AsReadOnly(new bool[0]);

            var copy = new bool[values.Length];
            Array.Copy(values, copy, values.Length);
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<int> Copy(int[] values)
        {
            if (values == null || values.Length == 0)
                return Array.AsReadOnly(new int[0]);

            var copy = new int[values.Length];
            Array.Copy(values, copy, values.Length);
            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<SkeletonStepDebugAnnotation> CopyAnnotations(SkeletonStepMeta[] stepMeta)
        {
            if (stepMeta == null || stepMeta.Length == 0)
                return Array.AsReadOnly(new SkeletonStepDebugAnnotation[0]);

            var copy = new SkeletonStepDebugAnnotation[stepMeta.Length];
            for (int i = 0; i < stepMeta.Length; i++)
            {
                SkeletonStepMeta meta = stepMeta[i];
                copy[i] = meta == null
                    ? SkeletonStepDebugAnnotation.Empty(i)
                    : new SkeletonStepDebugAnnotation(
                        meta.StepIndex,
                        meta.SourceAnchor,
                        meta.InEndingRegion,
                        meta.IsStrongBeat,
                        HasFlag(meta.ReasonFlags, SkeletonReasonFlags.MetricWeak),
                        meta.Protected,
                        meta.SourceOccupied,
                        meta.AdjacentToSource,
                        meta.InterstitialSourceGap,
                        meta.DistanceToNearestSourceHit,
                        meta.SegmentSourceWeight);
            }

            return Array.AsReadOnly(copy);
        }

        private static bool HasFlag(SkeletonReasonFlags actual, SkeletonReasonFlags expected)
        {
            return (actual & expected) == expected;
        }
    }

    [Serializable]
    public sealed class SkeletonStepDebugAnnotation
    {
        public SkeletonStepDebugAnnotation(
            int stepIndex,
            bool isAnchor,
            bool inEndingRegion,
            bool isStrongBeat,
            bool isWeakMetrical,
            bool isProtectedAnchor,
            bool sourceOccupied = false,
            bool adjacentToSource = false,
            bool interstitialSourceGap = false,
            int distanceToNearestSourceHit = 0,
            float segmentSourceWeight = 0f)
        {
            StepIndex = stepIndex;
            IsAnchor = isAnchor;
            InEndingRegion = inEndingRegion;
            IsStrongBeat = isStrongBeat;
            IsWeakMetrical = isWeakMetrical;
            IsProtectedAnchor = isProtectedAnchor;
            SourceOccupied = sourceOccupied;
            AdjacentToSource = adjacentToSource;
            InterstitialSourceGap = interstitialSourceGap;
            DistanceToNearestSourceHit = distanceToNearestSourceHit;
            SegmentSourceWeight = segmentSourceWeight;
        }

        public int StepIndex { get; private set; }
        public bool IsAnchor { get; private set; }
        public bool InEndingRegion { get; private set; }
        public bool IsStrongBeat { get; private set; }
        public bool IsWeakMetrical { get; private set; }
        public bool IsProtectedAnchor { get; private set; }
        public bool SourceOccupied { get; private set; }
        public bool AdjacentToSource { get; private set; }
        public bool InterstitialSourceGap { get; private set; }
        public int DistanceToNearestSourceHit { get; private set; }
        public float SegmentSourceWeight { get; private set; }

        public static SkeletonStepDebugAnnotation Empty(int stepIndex)
        {
            return new SkeletonStepDebugAnnotation(stepIndex, false, false, false, false, false);
        }
    }
}
