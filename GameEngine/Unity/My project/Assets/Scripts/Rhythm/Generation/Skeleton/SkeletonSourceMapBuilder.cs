using System;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
    internal sealed class SkeletonSourceAnchorMap
    {
        public bool[] CombinedAnchors { get; private set; }
        public bool[] ExplicitAnchors { get; private set; }
        public bool[] FallbackAnchors { get; private set; }

        public SkeletonSourceAnchorMap(
            bool[] combinedAnchors,
            bool[] explicitAnchors,
            bool[] fallbackAnchors)
        {
            CombinedAnchors = combinedAnchors;
            ExplicitAnchors = explicitAnchors;
            FallbackAnchors = fallbackAnchors;
        }
    }

    internal sealed class SkeletonSourceNeighbourhoodMap
    {
        public bool[] SourceGap { get; private set; }
        public bool[] AdjacentToSource { get; private set; }
        public bool[] NearSource { get; private set; }
        public bool[] InterstitialSourceGap { get; private set; }
        public int[] DistanceToNearestSource { get; private set; }
        public int[] PreviousSourceDistance { get; private set; }
        public int[] NextSourceDistance { get; private set; }
        public int[] LocalSourceDensity { get; private set; }
        public int[] SegmentSourceCounts { get; private set; }
        public float[] SegmentSourceWeights { get; private set; }

        public SkeletonSourceNeighbourhoodMap(
            bool[] sourceGap,
            bool[] adjacentToSource,
            bool[] nearSource,
            bool[] interstitialSourceGap,
            int[] distanceToNearestSource,
            int[] previousSourceDistance,
            int[] nextSourceDistance,
            int[] localSourceDensity,
            int[] segmentSourceCounts,
            float[] segmentSourceWeights)
        {
            SourceGap = sourceGap;
            AdjacentToSource = adjacentToSource;
            NearSource = nearSource;
            InterstitialSourceGap = interstitialSourceGap;
            DistanceToNearestSource = distanceToNearestSource;
            PreviousSourceDistance = previousSourceDistance;
            NextSourceDistance = nextSourceDistance;
            LocalSourceDensity = localSourceDensity;
            SegmentSourceCounts = segmentSourceCounts;
            SegmentSourceWeights = segmentSourceWeights;
        }
    }

    internal static class SkeletonSourceMapBuilder
    {
        public static bool[] BuildOccupiedMap(PatternTurn sourceTurn, int turnLengthSteps)
        {
            if (sourceTurn == null)
                throw new ArgumentNullException(nameof(sourceTurn));

            if (sourceTurn.velocity == null)
                throw new ArgumentException("Source turn velocity data is required.", nameof(sourceTurn));

            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            var occupied = new bool[turnLengthSteps];
            int mappedLength = Math.Min(turnLengthSteps, sourceTurn.velocity.Length);

            // Source and response turns share the same step grid. If lengths differ,
            // map the aligned prefix, truncate overflow, and leave missing tail steps empty.
            for (int i = 0; i < mappedLength; i++)
                occupied[i] = sourceTurn.velocity[i] > 0;

            return occupied;
        }

        public static SkeletonSourceNeighbourhoodMap BuildNeighbourhoodMap(
            bool[] sourceOccupied,
            int[] stepToSegment)
        {
            if (sourceOccupied == null)
                throw new ArgumentNullException(nameof(sourceOccupied));

            if (stepToSegment == null)
                throw new ArgumentNullException(nameof(stepToSegment));

            if (sourceOccupied.Length != stepToSegment.Length)
                throw new ArgumentException("Source and segment maps must have matching lengths.", nameof(stepToSegment));

            int stepCount = sourceOccupied.Length;
            var sourceGap = new bool[stepCount];
            var adjacentToSource = new bool[stepCount];
            var nearSource = new bool[stepCount];
            var interstitialSourceGap = new bool[stepCount];
            var distanceToNearestSource = new int[stepCount];
            var previousSourceDistance = new int[stepCount];
            var nextSourceDistance = new int[stepCount];
            var localSourceDensity = new int[stepCount];
            int segmentCount = GetSegmentCount(stepToSegment);
            var segmentSourceCounts = new int[segmentCount];
            var segmentStepCounts = new int[segmentCount];
            var segmentSourceWeights = new float[segmentCount];

            for (int i = 0; i < stepCount; i++)
            {
                int segmentIndex = stepToSegment[i];
                if (segmentIndex >= 0 && segmentIndex < segmentCount)
                {
                    segmentStepCounts[segmentIndex]++;
                    if (sourceOccupied[i])
                        segmentSourceCounts[segmentIndex]++;
                }
            }

            for (int i = 0; i < segmentCount; i++)
            {
                segmentSourceWeights[i] = segmentStepCounts[i] > 0
                    ? segmentSourceCounts[i] / (float)segmentStepCounts[i]
                    : 0f;
            }

            int previousSource = -1;
            for (int i = 0; i < stepCount; i++)
            {
                previousSourceDistance[i] = previousSource < 0
                    ? stepCount + 1
                    : i - previousSource;

                if (sourceOccupied[i])
                    previousSource = i;
            }

            int nextSource = -1;
            for (int i = stepCount - 1; i >= 0; i--)
            {
                nextSourceDistance[i] = nextSource < 0
                    ? stepCount + 1
                    : nextSource - i;

                if (sourceOccupied[i])
                    nextSource = i;
            }

            for (int i = 0; i < stepCount; i++)
            {
                sourceGap[i] = !sourceOccupied[i];
                int previousDistance = previousSourceDistance[i];
                int nextDistance = nextSourceDistance[i];
                int nearest = Math.Min(previousDistance, nextDistance);
                if (sourceOccupied[i])
                    nearest = 0;

                distanceToNearestSource[i] = nearest;
                adjacentToSource[i] = !sourceOccupied[i] && nearest == 1;
                nearSource[i] = !sourceOccupied[i] && nearest > 0 && nearest <= 2;
                interstitialSourceGap[i] = !sourceOccupied[i] &&
                                           previousDistance <= stepCount &&
                                           nextDistance <= stepCount;
                localSourceDensity[i] = CountLocalSourceHits(sourceOccupied, i, 2);
            }

            return new SkeletonSourceNeighbourhoodMap(
                sourceGap,
                adjacentToSource,
                nearSource,
                interstitialSourceGap,
                distanceToNearestSource,
                previousSourceDistance,
                nextSourceDistance,
                localSourceDensity,
                segmentSourceCounts,
                segmentSourceWeights);
        }

        public static SkeletonSourceAnchorMap BuildAnchorMap(TurnAnalysisResult sourceAnalysis, int turnLengthSteps)
        {
            if (sourceAnalysis == null)
                throw new ArgumentNullException(nameof(sourceAnalysis));

            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            var explicitAnchors = new bool[turnLengthSteps];
            var fallbackAnchors = new bool[turnLengthSteps];
            var combinedAnchors = new bool[turnLengthSteps];
            AnchorFeatures anchorFeatures = sourceAnalysis.Anchor ?? new AnchorFeatures();
            // Fallback indices are only trusted inside the same aligned source prefix
            // used by per-step anchor flags; response-tail indices are left unanchored.
            int alignedSourcePrefixLength = GetAlignedSourcePrefixLength(anchorFeatures, turnLengthSteps);

            CopyAnchorFlags(anchorFeatures, explicitAnchors);
            CopyAnchorIndices(anchorFeatures, explicitAnchors, fallbackAnchors, alignedSourcePrefixLength);

            for (int i = 0; i < turnLengthSteps; i++)
                combinedAnchors[i] = explicitAnchors[i] || fallbackAnchors[i];

            return new SkeletonSourceAnchorMap(
                combinedAnchors,
                explicitAnchors,
                fallbackAnchors);
        }

        private static void CopyAnchorFlags(AnchorFeatures anchorFeatures, bool[] anchors)
        {
            if (anchorFeatures.StepIsAnchor == null)
                return;

            int mappedLength = Math.Min(anchors.Length, anchorFeatures.StepIsAnchor.Count);
            for (int i = 0; i < mappedLength; i++)
                anchors[i] = anchorFeatures.StepIsAnchor[i];
        }

        private static void CopyAnchorIndices(
            AnchorFeatures anchorFeatures,
            bool[] explicitAnchors,
            bool[] fallbackAnchors,
            int alignedSourcePrefixLength)
        {
            if (anchorFeatures.AnchorIndices == null)
                return;

            for (int i = 0; i < anchorFeatures.AnchorIndices.Count; i++)
            {
                int anchorIndex = anchorFeatures.AnchorIndices[i];
                if (anchorIndex < 0 ||
                    anchorIndex >= fallbackAnchors.Length ||
                    anchorIndex >= alignedSourcePrefixLength)
                {
                    continue;
                }

                if (!explicitAnchors[anchorIndex])
                    fallbackAnchors[anchorIndex] = true;
            }
        }

        private static int GetAlignedSourcePrefixLength(AnchorFeatures anchorFeatures, int turnLengthSteps)
        {
            int anchorStepCount = anchorFeatures.StepIsAnchor != null && anchorFeatures.StepIsAnchor.Count > 0
                ? anchorFeatures.StepIsAnchor.Count
                : anchorFeatures.StepCount;

            if (anchorStepCount < 0)
                anchorStepCount = 0;

            return Math.Min(turnLengthSteps, anchorStepCount);
        }

        private static int CountLocalSourceHits(bool[] sourceOccupied, int centerStep, int radius)
        {
            int start = Math.Max(0, centerStep - radius);
            int end = Math.Min(sourceOccupied.Length - 1, centerStep + radius);
            int count = 0;

            for (int i = start; i <= end; i++)
            {
                if (sourceOccupied[i])
                    count++;
            }

            return count;
        }

        private static int GetSegmentCount(int[] stepToSegment)
        {
            int maxSegment = -1;
            for (int i = 0; i < stepToSegment.Length; i++)
            {
                if (stepToSegment[i] > maxSegment)
                    maxSegment = stepToSegment[i];
            }

            return Math.Max(1, maxSegment + 1);
        }
    }
}
