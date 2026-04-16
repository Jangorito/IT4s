using System;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Models;

namespace IT4s.Rhythm.Generation.Skeleton
{
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

        public static bool[] BuildAnchorMap(TurnAnalysisResult sourceAnalysis, int turnLengthSteps)
        {
            if (sourceAnalysis == null)
                throw new ArgumentNullException(nameof(sourceAnalysis));

            if (turnLengthSteps <= 0)
                throw new ArgumentOutOfRangeException(nameof(turnLengthSteps), "Turn length steps must be greater than zero.");

            var anchors = new bool[turnLengthSteps];
            AnchorFeatures anchorFeatures = sourceAnalysis.Anchor ?? new AnchorFeatures();

            CopyAnchorFlags(anchorFeatures, anchors);
            CopyAnchorIndices(anchorFeatures, anchors);

            return anchors;
        }

        private static void CopyAnchorFlags(AnchorFeatures anchorFeatures, bool[] anchors)
        {
            if (anchorFeatures.StepIsAnchor == null)
                return;

            int mappedLength = Math.Min(anchors.Length, anchorFeatures.StepIsAnchor.Count);
            for (int i = 0; i < mappedLength; i++)
                anchors[i] = anchorFeatures.StepIsAnchor[i];
        }

        private static void CopyAnchorIndices(AnchorFeatures anchorFeatures, bool[] anchors)
        {
            if (anchorFeatures.AnchorIndices == null)
                return;

            for (int i = 0; i < anchorFeatures.AnchorIndices.Count; i++)
            {
                int anchorIndex = anchorFeatures.AnchorIndices[i];
                if (anchorIndex >= 0 && anchorIndex < anchors.Length)
                    anchors[anchorIndex] = true;
            }
        }
    }
}
