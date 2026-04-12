using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class AnchorSupportFeatures
    {
        public float AverageSupport { get; private set; }
        public float StrongHitRatio { get; private set; }
        public float SupportedWeakHitRatio { get; private set; }
        public float UnsupportedWeakHitRatio { get; private set; }

        public AnchorSupportFeatures()
            : this(0.5f, 0f, 0f, 0f)
        {
        }

        public AnchorSupportFeatures(
            float averageSupport,
            float strongHitRatio,
            float supportedWeakHitRatio,
            float unsupportedWeakHitRatio)
        {
            AverageSupport = averageSupport;
            StrongHitRatio = strongHitRatio;
            SupportedWeakHitRatio = supportedWeakHitRatio;
            UnsupportedWeakHitRatio = unsupportedWeakHitRatio;
        }
    }
}
