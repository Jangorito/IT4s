using System;
using System.Collections.Generic;
using IT4s.Rhythm.TurnAnalysis.Models;
using IT4s.Rhythm.TurnAnalysis.Utils;

namespace IT4s.Rhythm.TurnAnalysis.Analysers
{
    public sealed class SegmentActivityProfileAnalyser
    {
        private readonly SegmentActivityProfileThresholds thresholds;

        public SegmentActivityProfileAnalyser(SegmentActivityProfileThresholds thresholds)
        {
            this.thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
        }

        public SegmentActivityProfileFeatures Analyze(
            DensityFeatures density,
            EnergyFeatures energy)
        {
            if (density == null)
                throw new ArgumentNullException(nameof(density));

            if (energy == null)
                throw new ArgumentNullException(nameof(energy));

            return new SegmentActivityProfileFeatures(
                ClassifyShape(density.SegmentDensities),
                ClassifyShape(energy.SegmentMeanVelocities));
        }

        private ActivityShape ClassifyShape(IReadOnlyList<float> segments)
        {
            if (segments == null || segments.Count != SegmentHelper.SegmentCount)
                throw new ArgumentException("Segment Activity Profile requires exactly 4 segment values.", nameof(segments));

            float epsilon = thresholds.ShapeEpsilon;

            float s0 = segments[0];
            float s1 = segments[1];
            float s2 = segments[2];
            float s3 = segments[3];

            float d01 = NormalizeDelta(s1 - s0, epsilon);
            float d12 = NormalizeDelta(s2 - s1, epsilon);
            float d23 = NormalizeDelta(s3 - s2, epsilon);

            if (d01 == 0f && d12 == 0f && d23 == 0f)
                return ActivityShape.Flat;

            bool nonDecreasing = d01 >= 0f && d12 >= 0f && d23 >= 0f;
            bool overallUp = (s3 - s0) > epsilon;

            if (nonDecreasing && overallUp)
                return ActivityShape.Increasing;

            bool nonIncreasing = d01 <= 0f && d12 <= 0f && d23 <= 0f;
            bool overallDown = (s0 - s3) > epsilon;

            if (nonIncreasing && overallDown)
                return ActivityShape.Decreasing;

            float earlyMean = (s0 + s1) * 0.5f;
            float lateMean = (s2 + s3) * 0.5f;

            if ((earlyMean - lateMean) > epsilon && s0 >= s3)
                return ActivityShape.FrontLoaded;

            if ((lateMean - earlyMean) > epsilon && s3 >= s0)
                return ActivityShape.BackLoaded;

            float middleMean = (s1 + s2) * 0.5f;
            float edgeMean = (s0 + s3) * 0.5f;

            if ((middleMean - edgeMean) > epsilon)
                return ActivityShape.MidPeak;

            if ((edgeMean - middleMean) > epsilon)
                return ActivityShape.MidDip;

            return ActivityShape.Flat;
        }

        private static float NormalizeDelta(float delta, float epsilon)
        {
            return Math.Abs(delta) <= epsilon ? 0f : delta;
        }
    }
}
