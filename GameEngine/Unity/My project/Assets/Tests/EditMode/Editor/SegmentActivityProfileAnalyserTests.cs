using System.Collections.Generic;
using System.Reflection;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Rhythm.TurnAnalysis.Tests
{
    public sealed class SegmentActivityProfileAnalyserTests
    {
        [Test]
        public void Analyze_AllZeroSegments_ClassifiesAsFlat()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(0f, 0f, 0f, 0f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.Flat));
        }

        [Test]
        public void Analyze_ClearIncreasingProfile_ClassifiesAsIncreasing()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(0f, 0.25f, 0.5f, 1f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.Increasing));
        }

        [Test]
        public void Analyze_ClearDecreasingProfile_ClassifiesAsDecreasing()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(1f, 0.5f, 0.25f, 0f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.Decreasing));
        }

        [Test]
        public void Analyze_FrontWeightedNonMonotonicProfile_ClassifiesAsFrontLoaded()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(1f, 0.8f, 0.9f, 0.2f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.FrontLoaded));
        }

        [Test]
        public void Analyze_BackWeightedNonMonotonicProfile_ClassifiesAsBackLoaded()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(0.2f, 0.9f, 0.8f, 1f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.BackLoaded));
        }

        [Test]
        public void Analyze_MiddleStrongProfile_ClassifiesAsMidPeak()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(0.1f, 0.9f, 0.9f, 0.1f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.MidPeak));
        }

        [Test]
        public void Analyze_MiddleWeakProfile_ClassifiesAsMidDip()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(0.9f, 0.1f, 0.1f, 0.9f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.MidDip));
        }

        [Test]
        public void Analyze_NearFlatNoisyProfile_ClassifiesAsFlatDueToEpsilon()
        {
            SegmentActivityProfileFeatures features = AnalyzeDensity(1f, 1.01f, 0.995f, 1f);

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.Flat));
        }

        [Test]
        public void Analyze_ClassifiesEnergyShapeWithSameDerivedStage()
        {
            var analyser = Analyser();

            SegmentActivityProfileFeatures features = analyser.Analyze(
                Density(0f, 0f, 0f, 0f),
                Energy(0f, 25f, 50f, 100f));

            Assert.That(features.DensityShape, Is.EqualTo(ActivityShape.Flat));
            Assert.That(features.EnergyShape, Is.EqualTo(ActivityShape.Increasing));
        }

        [Test]
        public void Analyze_NullDensityFeatures_ThrowsArgumentNullException()
        {
            var analyser = Analyser();

            Assert.That(
                () => analyser.Analyze(null, Energy(0f, 0f, 0f, 0f)),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Analyze_NullEnergyFeatures_ThrowsArgumentNullException()
        {
            var analyser = Analyser();

            Assert.That(
                () => analyser.Analyze(Density(0f, 0f, 0f, 0f), null),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Analyze_InvalidSegmentCount_ThrowsArgumentException()
        {
            var analyser = Analyser();
            DensityFeatures density = Density(0f, 0f, 0f, 0f);
            ReplaceBackingField(
                density,
                nameof(DensityFeatures.SegmentDensities),
                new[] { 0f, 0f, 0f });

            Assert.That(
                () => analyser.Analyze(density, Energy(0f, 0f, 0f, 0f)),
                Throws.ArgumentException);
        }

        [Test]
        public void Analyze_NullSegmentList_ThrowsArgumentException()
        {
            var analyser = Analyser();
            EnergyFeatures energy = Energy(0f, 0f, 0f, 0f);
            ReplaceBackingField<IReadOnlyList<float>>(
                energy,
                nameof(EnergyFeatures.SegmentMeanVelocities),
                null);

            Assert.That(
                () => analyser.Analyze(Density(0f, 0f, 0f, 0f), energy),
                Throws.ArgumentException);
        }

        private static SegmentActivityProfileFeatures AnalyzeDensity(params float[] segments)
        {
            return Analyser().Analyze(
                Density(segments),
                Energy(0f, 0f, 0f, 0f));
        }

        private static SegmentActivityProfileAnalyser Analyser()
        {
            return new SegmentActivityProfileAnalyser(
                new SegmentActivityProfileThresholds(0.02f));
        }

        private static DensityFeatures Density(params float[] segments)
        {
            return new DensityFeatures(
                0,
                0,
                0,
                0f,
                segments);
        }

        private static EnergyFeatures Energy(params float[] segments)
        {
            return new EnergyFeatures(
                0f,
                0,
                0f,
                segments,
                false,
                true,
                false,
                false,
                false,
                false);
        }

        private static void ReplaceBackingField<T>(
            object target,
            string propertyName,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                string.Format("<{0}>k__BackingField", propertyName),
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
