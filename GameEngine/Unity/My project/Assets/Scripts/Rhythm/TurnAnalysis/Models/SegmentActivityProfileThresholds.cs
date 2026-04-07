using System;

namespace IT4s.Rhythm.TurnAnalysis.Models
{
    [Serializable]
    public sealed class SegmentActivityProfileThresholds
    {
        public float ShapeEpsilon { get; private set; }

        public SegmentActivityProfileThresholds(float shapeEpsilon)
        {
            ShapeEpsilon = shapeEpsilon;
        }
    }
}
