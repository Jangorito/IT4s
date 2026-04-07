using System;
using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Rhythm.TurnAnalysis.Tests
{
    public sealed class TurnAnalyserTests
    {
        [TestCase("densityAnalyser")]
        [TestCase("energyAnalyser")]
        [TestCase("anchorAnalyser")]
        [TestCase("endActivityAnalyser")]
        [TestCase("segmentActivityProfileAnalyser")]
        public void Constructor_NullDependency_ThrowsArgumentNullException(string nullDependencyName)
        {
            DependencySet dependencies = Dependencies();
            dependencies.Clear(nullDependencyName);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new TurnAnalyser(
                    dependencies.DensityAnalyser,
                    dependencies.EnergyAnalyser,
                    dependencies.AnchorAnalyser,
                    dependencies.EndActivityAnalyser,
                    dependencies.SegmentActivityProfileAnalyser));

            Assert.That(exception.ParamName, Is.EqualTo(nullDependencyName));
        }

        [Test]
        public void Analyze_NullPattern_ThrowsArgumentNullException()
        {
            var analyser = Analyser();

            Assert.That(() => analyser.Analyze(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Analyze_ReturnsNonNullTurnAnalysisResult()
        {
            var analyser = Analyser();

            TurnAnalysisResult result = analyser.Analyze(Turn(20, 0, 40, 0, 80, 0, 100, 0));

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Analyze_PopulatesAllResultSections()
        {
            var analyser = Analyser();

            TurnAnalysisResult result = analyser.Analyze(Turn(20, 0, 40, 0, 80, 0, 100, 0));

            Assert.That(result.Density, Is.Not.Null);
            Assert.That(result.Energy, Is.Not.Null);
            Assert.That(result.Anchor, Is.Not.Null);
            Assert.That(result.EndActivity, Is.Not.Null);
            Assert.That(result.SegmentActivityProfile, Is.Not.Null);
        }

        [Test]
        public void Analyze_DerivesSegmentActivityProfileFromDensityAndEnergy()
        {
            var dependencies = Dependencies();
            var analyser = new TurnAnalyser(
                dependencies.DensityAnalyser,
                dependencies.EnergyAnalyser,
                dependencies.AnchorAnalyser,
                dependencies.EndActivityAnalyser,
                dependencies.SegmentActivityProfileAnalyser);

            TurnAnalysisResult result = analyser.Analyze(Turn(10, 0, 30, 0, 60, 0, 90, 0));
            SegmentActivityProfileFeatures expected = dependencies.SegmentActivityProfileAnalyser.Analyze(
                result.Density,
                result.Energy);

            Assert.That(result.SegmentActivityProfile.DensityShape, Is.EqualTo(expected.DensityShape));
            Assert.That(result.SegmentActivityProfile.EnergyShape, Is.EqualTo(expected.EnergyShape));
        }

        [Test]
        public void Analyze_EmptyTurn_ReturnsValidResultWithNonNullFeatureObjects()
        {
            var analyser = Analyser();

            TurnAnalysisResult result = analyser.Analyze(Turn());

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Density, Is.Not.Null);
            Assert.That(result.Energy, Is.Not.Null);
            Assert.That(result.Anchor, Is.Not.Null);
            Assert.That(result.EndActivity, Is.Not.Null);
            Assert.That(result.SegmentActivityProfile, Is.Not.Null);
        }

        private static TurnAnalyser Analyser()
        {
            DependencySet dependencies = Dependencies();

            return new TurnAnalyser(
                dependencies.DensityAnalyser,
                dependencies.EnergyAnalyser,
                dependencies.AnchorAnalyser,
                dependencies.EndActivityAnalyser,
                dependencies.SegmentActivityProfileAnalyser);
        }

        private static DependencySet Dependencies()
        {
            return new DependencySet(
                new DensityAnalyser(),
                new EnergyAnalyser(EnergyThresholds()),
                new AnchorAnalyser(),
                new EndActivityAnalyser(),
                new SegmentActivityProfileAnalyser(SegmentActivityProfileThresholds()));
        }

        private static PatternTurn Turn(params int[] velocities)
        {
            return new PatternTurn
            {
                velocity = velocities
            };
        }

        private static EnergyThresholds EnergyThresholds()
        {
            return new EnergyThresholds(
                90f,
                10f,
                1f,
                45f,
                50f,
                50f);
        }

        private static SegmentActivityProfileThresholds SegmentActivityProfileThresholds()
        {
            return new SegmentActivityProfileThresholds(0.02f);
        }

        private sealed class DependencySet
        {
            public DependencySet(
                DensityAnalyser densityAnalyser,
                EnergyAnalyser energyAnalyser,
                AnchorAnalyser anchorAnalyser,
                EndActivityAnalyser endActivityAnalyser,
                SegmentActivityProfileAnalyser segmentActivityProfileAnalyser)
            {
                DensityAnalyser = densityAnalyser;
                EnergyAnalyser = energyAnalyser;
                AnchorAnalyser = anchorAnalyser;
                EndActivityAnalyser = endActivityAnalyser;
                SegmentActivityProfileAnalyser = segmentActivityProfileAnalyser;
            }

            public DensityAnalyser DensityAnalyser { get; private set; }
            public EnergyAnalyser EnergyAnalyser { get; private set; }
            public AnchorAnalyser AnchorAnalyser { get; private set; }
            public EndActivityAnalyser EndActivityAnalyser { get; private set; }
            public SegmentActivityProfileAnalyser SegmentActivityProfileAnalyser { get; private set; }

            public void Clear(string dependencyName)
            {
                switch (dependencyName)
                {
                    case "densityAnalyser":
                        DensityAnalyser = null;
                        break;
                    case "energyAnalyser":
                        EnergyAnalyser = null;
                        break;
                    case "anchorAnalyser":
                        AnchorAnalyser = null;
                        break;
                    case "endActivityAnalyser":
                        EndActivityAnalyser = null;
                        break;
                    case "segmentActivityProfileAnalyser":
                        SegmentActivityProfileAnalyser = null;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dependencyName), dependencyName, "Unknown dependency.");
                }
            }
        }
    }
}
