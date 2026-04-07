using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class DensityFeatures
    {
        public int StepCount;
        public int ActiveStepCount;
        public int InactiveStepCount;
        public float StepDensity;
        public float[] SegmentDensities = new float[4];
    }
}
