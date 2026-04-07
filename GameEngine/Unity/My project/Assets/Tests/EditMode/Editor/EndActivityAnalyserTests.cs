using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Rhythm.TurnAnalysis.Tests
{
    public sealed class EndActivityAnalyserTests
    {
        [Test]
        public void Analyze_EmptyTurn_ReturnsAllZeros()
        {
            var analyser = new EndActivityAnalyser();

            EndActivityFeatures features = analyser.Analyze(Turn());

            Assert.That(features.EndDensity, Is.EqualTo(0f));
            Assert.That(features.EndEnergy, Is.EqualTo(0f));
            Assert.That(features.EndAccent, Is.EqualTo(0));
        }

        [Test]
        public void Analyze_NoActiveStepsInEndWindow_ReturnsAllZeros()
        {
            var analyser = new EndActivityAnalyser();

            EndActivityFeatures features = analyser.Analyze(Turn(100, 80, 60, 0, 0, 0, 0, 0));

            Assert.That(features.EndDensity, Is.EqualTo(0f));
            Assert.That(features.EndEnergy, Is.EqualTo(0f));
            Assert.That(features.EndAccent, Is.EqualTo(0));
        }

        [Test]
        public void Analyze_SingleActiveStepInEndWindow_ReturnsWindowDensityAndVelocityMetrics()
        {
            var analyser = new EndActivityAnalyser();

            EndActivityFeatures features = analyser.Analyze(Turn(0, 0, 0, 0, 0, 0, 80, 0));

            Assert.That(features.EndDensity, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(features.EndEnergy, Is.EqualTo(80f).Within(0.0001f));
            Assert.That(features.EndAccent, Is.EqualTo(80));
        }

        [Test]
        public void Analyze_FullyActiveEndWindow_ReturnsDensityOne()
        {
            var analyser = new EndActivityAnalyser();

            EndActivityFeatures features = analyser.Analyze(Turn(0, 0, 0, 0, 0, 0, 40, 90));

            Assert.That(features.EndDensity, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(features.EndEnergy, Is.EqualTo(65f).Within(0.0001f));
            Assert.That(features.EndAccent, Is.EqualTo(90));
        }

        [Test]
        public void Analyze_IgnoresActivityBeforeEndWindow()
        {
            var analyser = new EndActivityAnalyser();

            EndActivityFeatures features = analyser.Analyze(Turn(127, 110, 90, 70, 50, 30, 0, 0));

            Assert.That(features.EndDensity, Is.EqualTo(0f));
            Assert.That(features.EndEnergy, Is.EqualTo(0f));
            Assert.That(features.EndAccent, Is.EqualTo(0));
        }

        [Test]
        public void Analyze_NullPattern_ThrowsArgumentNullException()
        {
            var analyser = new EndActivityAnalyser();

            Assert.That(() => analyser.Analyze(null), Throws.ArgumentNullException);
        }

        private static PatternTurn Turn(params int[] velocities)
        {
            return new PatternTurn
            {
                velocity = velocities
            };
        }
    }
}
