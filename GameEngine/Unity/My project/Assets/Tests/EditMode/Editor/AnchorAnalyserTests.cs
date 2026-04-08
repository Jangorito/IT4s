using IT4s.Data;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Rhythm.TurnAnalysis.Tests
{
    public sealed class AnchorAnalyserTests
    {
        [Test]
        public void Analyze_EmptyTurn_ReturnsEmptyStepDataAndDefaultAggregates()
        {
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(Turn());

            Assert.That(features.StepCount, Is.EqualTo(0));
            Assert.That(features.StepSalienceScores.Count, Is.EqualTo(0));
            Assert.That(features.StepIsAnchor.Count, Is.EqualTo(0));
            Assert.That(features.AnchorCount, Is.EqualTo(0));
            Assert.That(features.AnchorIndices.Count, Is.EqualTo(0));
            Assert.That(features.StrongestAnchorIndex, Is.Null);
            Assert.That(features.StrongestAnchorScore, Is.EqualTo(0f));
            Assert.That(features.HasOpeningAnchor, Is.False);
            Assert.That(features.HasClosingAnchor, Is.False);
            AssertIntList(features.AnchorCountsPerSegment, 0, 0, 0, 0);
            Assert.That(features.AverageSupport, Is.EqualTo(0f));
            Assert.That(features.StrongHitRatio, Is.EqualTo(0f));
            Assert.That(features.SupportedWeakHitRatio, Is.EqualTo(0f));
            Assert.That(features.UnsupportedWeakHitRatio, Is.EqualTo(0f));
        }

        [Test]
        public void Analyze_NoHitTurn_PreservesStepLengthAndReturnsNoAnchors()
        {
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(Turn(0, 0, 0, 0));

            Assert.That(features.StepCount, Is.EqualTo(4));
            AssertFloatList(features.StepSalienceScores, 0f, 0f, 0f, 0f);
            AssertBoolList(features.StepIsAnchor, false, false, false, false);
            Assert.That(features.AnchorCount, Is.EqualTo(0));
            Assert.That(features.AnchorIndices.Count, Is.EqualTo(0));
            Assert.That(features.StrongestAnchorIndex, Is.Null);
            Assert.That(features.StrongestAnchorScore, Is.EqualTo(0f));
            AssertIntList(features.AnchorCountsPerSegment, 0, 0, 0, 0);
            Assert.That(features.AverageSupport, Is.EqualTo(0f));
            Assert.That(features.StrongHitRatio, Is.EqualTo(0f));
            Assert.That(features.SupportedWeakHitRatio, Is.EqualTo(0f));
            Assert.That(features.UnsupportedWeakHitRatio, Is.EqualTo(0f));
        }

        [Test]
        public void Analyze_SingleHit_UsesVelocityIsolationAndPositionScores()
        {
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(Turn(127));

            Assert.That(features.StepCount, Is.EqualTo(1));
            AssertFloatList(features.StepSalienceScores, 0.7f);
            AssertBoolList(features.StepIsAnchor, true);
            Assert.That(features.AnchorCount, Is.EqualTo(1));
            AssertIntList(features.AnchorIndices, 0);
            Assert.That(features.StrongestAnchorIndex, Is.EqualTo(0));
            Assert.That(features.StrongestAnchorScore, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(features.HasOpeningAnchor, Is.True);
            Assert.That(features.HasClosingAnchor, Is.True);
            AssertIntList(features.AnchorCountsPerSegment, 1, 0, 0, 0);
            Assert.That(features.AverageSupport, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(features.StrongHitRatio, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(features.SupportedWeakHitRatio, Is.EqualTo(0f));
            Assert.That(features.UnsupportedWeakHitRatio, Is.EqualTo(0f));
        }

        [Test]
        public void Analyze_InactiveStepsAlwaysHaveZeroSalienceAndFalseAnchorFlag()
        {
            var pattern = Turn(127, 0, 64, 0, 127);
            int[] originalVelocities = (int[])pattern.velocity.Clone();
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(pattern);

            for (int i = 0; i < pattern.StepCount; i++)
            {
                if (pattern.velocity[i] > 0)
                    continue;

                Assert.That(features.StepSalienceScores[i], Is.EqualTo(0f), "Inactive salience at step {0}", i);
                Assert.That(features.StepIsAnchor[i], Is.False, "Inactive anchor flag at step {0}", i);
            }

            CollectionAssert.AreEqual(originalVelocities, pattern.velocity);
        }

        [Test]
        public void Analyze_StrongestAnchorTie_ChoosesEarliestIndex()
        {
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(Turn(127, 0, 0, 0, 127));

            Assert.That(features.AnchorCount, Is.EqualTo(2));
            AssertIntList(features.AnchorIndices, 0, 4);
            AssertFloatList(features.StepSalienceScores, 0.7f, 0f, 0f, 0f, 0.7f);
            AssertBoolList(features.StepIsAnchor, true, false, false, false, true);
            Assert.That(features.StrongestAnchorIndex, Is.EqualTo(0));
            Assert.That(features.StrongestAnchorScore, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(features.HasOpeningAnchor, Is.True);
            Assert.That(features.HasClosingAnchor, Is.True);
        }

        [Test]
        public void Analyze_SegmentCounts_UseSharedFourSegmentPartition()
        {
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(Turn(127, 0, 0, 0, 127, 0, 0, 127));

            AssertIntList(features.AnchorIndices, 0, 4, 7);
            Assert.That(features.AnchorCount, Is.EqualTo(3));
            AssertIntList(features.AnchorCountsPerSegment, 1, 0, 1, 1);
        }

        [Test]
        public void Analyze_SupportedWeakHits_ContributeToSupportSummaryWithoutChangingAnchorDetection()
        {
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(TurnWithQuarterSubdivision(4, 127, 0, 96, 0, 88, 0, 72, 0));

            AssertIntList(features.AnchorIndices, 0);
            Assert.That(features.AnchorCount, Is.EqualTo(1));
            Assert.That(features.AverageSupport, Is.EqualTo(0.725f).Within(0.0001f));
            Assert.That(features.StrongHitRatio, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(features.SupportedWeakHitRatio, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(features.UnsupportedWeakHitRatio, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Analyze_UnsupportedWeakHits_AreReportedSeparatelyInSupportSummary()
        {
            var analyser = new AnchorAnalyser();

            AnchorFeatures features = analyser.Analyze(TurnWithQuarterSubdivision(4, 0, 90, 0, 70, 0, 0, 0, 0, 65, 0, 55, 0, 0, 0, 0, 0));

            Assert.That(features.AverageSupport, Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(features.StrongHitRatio, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(features.SupportedWeakHitRatio, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(features.UnsupportedWeakHitRatio, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Analyze_NullPattern_ThrowsArgumentNullException()
        {
            var analyser = new AnchorAnalyser();

            Assert.That(() => analyser.Analyze(null), Throws.ArgumentNullException);
        }

        private static PatternTurn Turn(params int[] velocities)
        {
            return new PatternTurn
            {
                velocity = velocities
            };
        }

        private static PatternTurn TurnWithQuarterSubdivision(int stepsPerQuarter, params int[] velocities)
        {
            return new PatternTurn
            {
                stepsPerQuarter = stepsPerQuarter,
                velocity = velocities
            };
        }

        private static void AssertFloatList(
            System.Collections.Generic.IReadOnlyList<float> actual,
            params float[] expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Length));

            for (int i = 0; i < expected.Length; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]).Within(0.0001f), "Float value at index {0}", i);
        }

        private static void AssertBoolList(
            System.Collections.Generic.IReadOnlyList<bool> actual,
            params bool[] expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Length));

            for (int i = 0; i < expected.Length; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]), "Bool value at index {0}", i);
        }

        private static void AssertIntList(
            System.Collections.Generic.IReadOnlyList<int> actual,
            params int[] expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Length));

            for (int i = 0; i < expected.Length; i++)
                Assert.That(actual[i], Is.EqualTo(expected[i]), "Int value at index {0}", i);
        }
    }
}
