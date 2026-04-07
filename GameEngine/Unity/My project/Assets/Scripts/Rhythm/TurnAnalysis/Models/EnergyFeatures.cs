using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class EnergyFeatures
    {
        public float MeanVelocity;
        public int PeakVelocity;
        public float VelocityVariance;
        public float[] SegmentMeanVelocities = new float[4];
        public bool IsHighEnergy;
        public bool IsLowEnergy;
        public bool IsFlatEnergy;
        public bool IsAccented;
        public bool IsCrescendo;
        public bool IsDecrescendo;
    }
}
