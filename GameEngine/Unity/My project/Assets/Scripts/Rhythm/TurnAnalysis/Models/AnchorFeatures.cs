using System;
using System.Collections.Generic;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class AnchorFeatures
    {
        public int StepCount { get; private set; }
        public IReadOnlyList<float> StepSalienceScores { get; private set; }
        public IReadOnlyList<bool> StepIsAnchor { get; private set; }
        public int AnchorCount { get; private set; }
        public IReadOnlyList<int> AnchorIndices { get; private set; }
        public int? StrongestAnchorIndex { get; private set; }
        public float StrongestAnchorScore { get; private set; }
        public bool HasOpeningAnchor { get; private set; }
        public bool HasClosingAnchor { get; private set; }
        public IReadOnlyList<int> AnchorCountsPerSegment { get; private set; }

        public AnchorFeatures()
            : this(0, null, null, 0, null, null, 0f, false, false, null)
        {
        }

        public AnchorFeatures(
            int stepCount,
            IReadOnlyList<float> stepSalienceScores,
            IReadOnlyList<bool> stepIsAnchor,
            int anchorCount,
            IReadOnlyList<int> anchorIndices,
            int? strongestAnchorIndex,
            float strongestAnchorScore,
            bool hasOpeningAnchor,
            bool hasClosingAnchor,
            IReadOnlyList<int> anchorCountsPerSegment)
        {
            StepCount = stepCount;
            StepSalienceScores = CopyOrEmpty(stepSalienceScores);
            StepIsAnchor = CopyOrEmpty(stepIsAnchor);
            AnchorCount = anchorCount;
            AnchorIndices = CopyOrEmpty(anchorIndices);
            StrongestAnchorIndex = strongestAnchorIndex;
            StrongestAnchorScore = strongestAnchorScore;
            HasOpeningAnchor = hasOpeningAnchor;
            HasClosingAnchor = hasClosingAnchor;
            AnchorCountsPerSegment = CopyOrDefaultSegments(anchorCountsPerSegment);
        }

        private static IReadOnlyList<T> CopyOrEmpty<T>(IReadOnlyList<T> values)
        {
            if (values == null)
                return Array.AsReadOnly(new T[0]);

            var copy = new T[values.Count];
            for (int i = 0; i < values.Count; i++)
                copy[i] = values[i];

            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<int> CopyOrDefaultSegments(IReadOnlyList<int> values)
        {
            var copy = new int[4];
            if (values == null)
                return Array.AsReadOnly(copy);

            int count = Math.Min(copy.Length, values.Count);
            for (int i = 0; i < count; i++)
                copy[i] = values[i];

            return Array.AsReadOnly(copy);
        }
    }
}
