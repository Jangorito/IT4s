using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class EndActivityFeatures
    {
        public float EndDensity { get; private set; }
        public float EndEnergy { get; private set; }
        public int EndAccent { get; private set; }

        public EndActivityFeatures()
            : this(0f, 0f, 0)
        {
        }

        public EndActivityFeatures(
            float endDensity,
            float endEnergy,
            int endAccent)
        {
            EndDensity = endDensity;
            EndEnergy = endEnergy;
            EndAccent = endAccent;
        }
    }
}
