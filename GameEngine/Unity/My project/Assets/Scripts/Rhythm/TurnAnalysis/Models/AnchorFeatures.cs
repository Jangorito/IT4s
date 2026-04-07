using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class AnchorFeatures
    {
        public int StepCount;
        public float[] StepSalienceScores = new float[0];
        public bool[] StepIsAnchor = new bool[0];
        public int AnchorCount;
        public int[] AnchorIndices = new int[0];
        public int? StrongestAnchorIndex;
        public float StrongestAnchorScore;
        public bool HasOpeningAnchor;
        public bool HasClosingAnchor;
        public int[] AnchorCountsPerSegment = new int[4];
    }
}
