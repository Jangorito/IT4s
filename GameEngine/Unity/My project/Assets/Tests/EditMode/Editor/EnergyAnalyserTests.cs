using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Rhythm.TurnAnalysis.Tests
{
    public sealed class EnergyAnalyserTests
    {
        [Test]
        public void Analyze_ComputesRawMetricsFromActiveStepVelocitiesOnly()
        {
            var pattern = Turn(0, 20, 40, 0, 80, 0, 100, 0);
            int[] originalVelocities = (int[])pattern.velocity.Clone();
            var analyser = new EnergyAnalyser(Thresholds(90f, 10f, 1f, 45f, 50f, 50f));

            EnergyFeatures features = analyser.Analyze(pattern);

            Assert.That(features.MeanVelocity, Is.EqualTo(60f).Within(0.0001f));
            Assert.That(features.PeakVelocity, Is.EqualTo(100));
            Assert.That(features.VelocityVariance, Is.EqualTo(1000f).Within(0.0001f));
            AssertFloatList(features.SegmentMeanVelocities, 20f, 40f, 80f, 100f);
            CollectionAssert.AreEqual(originalVelocities, pattern.velocity);
        }

        [Test]
        public void Analyze_DerivesHighAndFlatEnergyFromThresholds()
        {
            var pattern = Turn(90, 0, 90, 0);
            var analyser = new EnergyAnalyser(Thresholds(80f, 20f, 0f, 1f, 100f, 100f));

            EnergyFeatures features = analyser.Analyze(pattern);

            Assert.That(features.IsHighEnergy, Is.True);
            Assert.That(features.IsLowEnergy, Is.False);
            Assert.That(features.IsFlatEnergy, Is.True);
            Assert.That(features.IsAccented, Is.False);
            Assert.That(features.IsCrescendo, Is.False);
            Assert.That(features.IsDecrescendo, Is.False);
        }

        [Test]
        public void Analyze_DerivesAccentFromPeakOverMeanDelta()
        {
            var pattern = Turn(60, 60, 60, 120);
            var analyser = new EnergyAnalyser(Thresholds(200f, 10f, 0f, 40f, 100f, 100f));

            EnergyFeatures features = analyser.Analyze(pattern);

            Assert.That(features.MeanVelocity, Is.EqualTo(75f).Within(0.0001f));
            Assert.That(features.PeakVelocity, Is.EqualTo(120));
            Assert.That(features.IsAccented, Is.True);
        }

        [Test]
        public void Analyze_DerivesCrescendoAndDecrescendoFromSegmentMeanProfile()
        {
            var crescendoAnalyser = new EnergyAnalyser(Thresholds(200f, 10f, 0f, 100f, 60f, 60f));
            var decrescendoAnalyser = new EnergyAnalyser(Thresholds(200f, 10f, 0f, 100f, 60f, 60f));

            EnergyFeatures crescendo = crescendoAnalyser.Analyze(Turn(20, 0, 40, 0, 80, 0, 100, 0));
            EnergyFeatures decrescendo = decrescendoAnalyser.Analyze(Turn(100, 0, 80, 0, 40, 0, 20, 0));

            Assert.That(crescendo.IsCrescendo, Is.True);
            Assert.That(crescendo.IsDecrescendo, Is.False);
            Assert.That(decrescendo.IsCrescendo, Is.False);
            Assert.That(decrescendo.IsDecrescendo, Is.True);
        }

        [Test]
        public void Analyze_NoActiveSteps_ReturnsLowEnergyOnlyEvenWhenThresholdsAreZero()
        {
            var analyser = new EnergyAnalyser(Thresholds(0f, 0f, 0f, 0f, 0f, 0f));

            EnergyFeatures features = analyser.Analyze(Turn(0, 0, 0, 0));

            Assert.That(features.MeanVelocity, Is.EqualTo(0f));
            Assert.That(features.PeakVelocity, Is.EqualTo(0));
            Assert.That(features.VelocityVariance, Is.EqualTo(0f));
            AssertFloatList(features.SegmentMeanVelocities, 0f, 0f, 0f, 0f);
            AssertLowEnergyOnly(features);
        }

        [Test]
        public void Analyze_EmptyTurn_ReturnsLowEnergyOnly()
        {
            var analyser = new EnergyAnalyser(Thresholds(0f, 0f, 0f, 0f, 0f, 0f));

            EnergyFeatures features = analyser.Analyze(Turn());

            Assert.That(features.MeanVelocity, Is.EqualTo(0f));
            Assert.That(features.PeakVelocity, Is.EqualTo(0));
            Assert.That(features.VelocityVariance, Is.EqualTo(0f));
            AssertFloatList(features.SegmentMeanVelocities, 0f, 0f, 0f, 0f);
            AssertLowEnergyOnly(features);
        }

        [Test]
        public void Analyze_SingleActiveHit_HasZeroVarianceAndNoAccentByDefault()
        {
            var analyser = new EnergyAnalyser(Thresholds(100f, 10f, 0f, 1f, 100f, 100f));

            EnergyFeatures features = analyser.Analyze(Turn(0, 50, 0, 0));

            Assert.That(features.MeanVelocity, Is.EqualTo(50f).Within(0.0001f));
            Assert.That(features.PeakVelocity, Is.EqualTo(50));
            Assert.That(features.VelocityVariance, Is.EqualTo(0f).Within(0.0001f));
            AssertFloatList(features.SegmentMeanVelocities, 0f, 50f, 0f, 0f);
            Assert.That(features.IsAccented, Is.False);
        }

        [Test]
        public void Analyze_NullPattern_ThrowsArgumentNullException()
        {
            var analyser = new EnergyAnalyser(Thresholds(100f, 10f, 0f, 1f, 100f, 100f));

            Assert.That(() => analyser.Analyze(null), Throws.ArgumentNullException);
        }

        private static PatternTurn Turn(params int[] velocities)
        {
            return new PatternTurn
            {
                velocity = velocities
            };
        }

        private static EnergyThresholds Thresholds(
            float highEnergyMeanVelocity,
            float lowEnergyMeanVelocity,
            float flatVarianceThreshold,
            float accentPeakOverMeanThreshold,
            float crescendoMinDelta,
            float decrescendoMinDelta)
        {
            return new EnergyThresholds(
                highEnergyMeanVelocity,
                lowEnergyMeanVelocity,
                flatVarianceThreshold,
                accentPeakOverMeanThreshold,
                crescendoMinDelta,
                decrescendoMinDelta);
        }

        private static void AssertFloatList(
            System.Collections.Generic.IReadOnlyList<float> actual,
            params float[] expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Length));

            for (int i = 0; i < expected.Length; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(0.0001f));
        }

        private static void AssertLowEnergyOnly(EnergyFeatures features)
        {
            Assert.That(features.IsHighEnergy, Is.False);
            Assert.That(features.IsLowEnergy, Is.True);
            Assert.That(features.IsFlatEnergy, Is.False);
            Assert.That(features.IsAccented, Is.False);
            Assert.That(features.IsCrescendo, Is.False);
            Assert.That(features.IsDecrescendo, Is.False);
        }
    }
}
