using IT4s.Data;
using IT4s.Rhythm.Generation.Skeleton;
using IT4s.Rhythm.Generation.Skeleton.Models;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Rhythm.Generation.Skeleton.Tests
{
    public sealed class SkeletonBuilderTests
    {
        [Test]
        public void BuildSkeleton_NullRequest_ThrowsArgumentNullException()
        {
            var builder = new SkeletonBuilder();

            Assert.That(() => builder.BuildSkeleton(null), Throws.ArgumentNullException);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void BuildSkeleton_InvalidTurnLength_ThrowsArgumentOutOfRangeException(int turnLengthSteps)
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(turnLengthSteps: 48);
            request.TurnLengthSteps = turnLengthSteps;
            request.Plan = Plan(turnLengthSteps);

            Assert.That(() => builder.BuildSkeleton(request), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(-12)]
        public void BuildSkeleton_InvalidStepsPerQuarter_ThrowsArgumentOutOfRangeException(int stepsPerQuarter)
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request();
            request.StepsPerQuarter = stepsPerQuarter;
            request.SourceTurn.stepsPerQuarter = stepsPerQuarter;

            Assert.That(() => builder.BuildSkeleton(request), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void BuildSkeleton_MissingRequiredFields_ThrowsArgumentException()
        {
            var builder = new SkeletonBuilder();

            SkeletonBuildRequest missingPlan = Request();
            missingPlan.Plan = null;

            SkeletonBuildRequest missingSourceTurn = Request();
            missingSourceTurn.SourceTurn = null;

            SkeletonBuildRequest missingAnalysis = Request();
            missingAnalysis.SourceAnalysis = null;

            SkeletonBuildRequest missingConfig = Request();
            missingConfig.Config = null;

            SkeletonBuildRequest missingVelocity = Request();
            missingVelocity.SourceTurn.velocity = null;

            Assert.That(() => builder.BuildSkeleton(missingPlan), Throws.ArgumentException);
            Assert.That(() => builder.BuildSkeleton(missingSourceTurn), Throws.ArgumentException);
            Assert.That(() => builder.BuildSkeleton(missingAnalysis), Throws.ArgumentException);
            Assert.That(() => builder.BuildSkeleton(missingConfig), Throws.ArgumentException);
            Assert.That(() => builder.BuildSkeleton(missingVelocity), Throws.ArgumentException);
        }

        [Test]
        public void BuildSkeleton_PlanLengthMismatch_ThrowsArgumentException()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(turnLengthSteps: 48);
            request.Plan = Plan(96);

            Assert.That(() => builder.BuildSkeleton(request), Throws.ArgumentException);
        }

        [Test]
        public void BuildSkeleton_ReturnsCorrectlySizedEmptyPattern()
        {
            var builder = new SkeletonBuilder();

            SkeletonPattern pattern = builder.BuildSkeleton(Request(turnLengthSteps: 48));

            Assert.That(pattern.TurnLengthSteps, Is.EqualTo(48));
            Assert.That(pattern.ActiveSteps, Has.Length.EqualTo(48));
            Assert.That(pattern.SelectionScores, Has.Length.EqualTo(48));
            Assert.That(pattern.StepMeta, Has.Length.EqualTo(48));
            Assert.That(pattern.SelectedStepIndices, Is.Empty);
            Assert.That(pattern.SelectionScores[0], Is.Not.EqualTo(0f));

            for (int i = 0; i < pattern.TurnLengthSteps; i++)
            {
                Assert.That(pattern.ActiveSteps[i], Is.False, "Active flag at step {0}", i);
                Assert.That(pattern.SelectionScores[i], Is.EqualTo(pattern.StepMeta[i].FinalScore), "Selection score at step {0}", i);
                Assert.That(pattern.StepMeta[i].StepIndex, Is.EqualTo(i), "Step index at {0}", i);
                Assert.That(pattern.StepMeta[i].SegmentIndex, Is.InRange(0, 3), "Segment index at step {0}", i);
                Assert.That(pattern.StepMeta[i].StepsFromEnd, Is.EqualTo(pattern.TurnLengthSteps - 1 - i), "Steps from end at step {0}", i);
                Assert.That(pattern.StepMeta[i].IsStrongBeat, Is.EqualTo(i % 12 == 0), "Strong beat flag at step {0}", i);
                Assert.That(pattern.StepMeta[i].Selected, Is.False, "Selected flag at step {0}", i);
                Assert.That(pattern.StepMeta[i].SpacingPenalty, Is.EqualTo(0f), "Spacing penalty at step {0}", i);
                Assert.That(pattern.StepMeta[i].RawScore, Is.EqualTo(pattern.StepMeta[i].FinalScore), "Raw/final score at step {0}", i);
            }

            Assert.That(pattern.Summary.ActiveCount, Is.EqualTo(0));
            Assert.That(pattern.Summary.AchievedDensity, Is.EqualTo(0f));
            Assert.That(pattern.Summary.SourceOverlapCount, Is.EqualTo(0));
            Assert.That(pattern.Summary.AnchorAlignedCount, Is.EqualTo(0));
            Assert.That(pattern.Summary.DensityTargetMet, Is.False);
            Assert.That(pattern.Summary.UsedStochasticTieBreak, Is.False);
        }

        [Test]
        public void BuildSkeleton_SourceOccupiedSteps_AreMappedIntoStepMeta()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                sourceVelocities: Velocities(48, 0, 5, 47));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[0].SourceOccupied, Is.True);
            Assert.That(pattern.StepMeta[5].SourceOccupied, Is.True);
            Assert.That(pattern.StepMeta[47].SourceOccupied, Is.True);
            Assert.That(pattern.StepMeta[1].SourceOccupied, Is.False);
            Assert.That(pattern.StepMeta[46].SourceOccupied, Is.False);
        }

        [Test]
        public void BuildSkeleton_SourceOccupiedMap_UsesAlignedPrefixWhenLengthsDiffer()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 8,
                sourceVelocities: new[] { 0, 96, 0, 80 });

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[1].SourceOccupied, Is.True);
            Assert.That(pattern.StepMeta[3].SourceOccupied, Is.True);

            for (int i = 4; i < pattern.TurnLengthSteps; i++)
                Assert.That(pattern.StepMeta[i].SourceOccupied, Is.False, "Padded source occupancy at step {0}", i);
        }

        [Test]
        public void BuildSkeleton_SourceAnchorFlags_AreMappedAsExplicitAnchors()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                sourceAnalysis: Analysis(ExplicitAnchorFlagsOnly(16, 2, 7)));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[2].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[2].IsExplicitAnchor, Is.True);
            Assert.That(pattern.StepMeta[2].IsFallbackAnchor, Is.False);
            Assert.That(pattern.StepMeta[7].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[7].IsExplicitAnchor, Is.True);
            Assert.That(pattern.StepMeta[7].IsFallbackAnchor, Is.False);
            Assert.That(pattern.StepMeta[1].SourceAnchor, Is.False);
            Assert.That(pattern.StepMeta[1].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[1].IsFallbackAnchor, Is.False);
            Assert.That(pattern.StepMeta[8].SourceAnchor, Is.False);
        }

        [Test]
        public void BuildSkeleton_SourceAnchorIndices_AreMappedAsFallbackAnchorsWhenFlagsAreAbsent()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                sourceAnalysis: Analysis(AnchorIndicesOnly(16, 3, 10)));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[3].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[3].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[3].IsFallbackAnchor, Is.True);
            Assert.That(pattern.StepMeta[10].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[10].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[10].IsFallbackAnchor, Is.True);
            Assert.That(pattern.StepMeta[4].SourceAnchor, Is.False);
            Assert.That(pattern.StepMeta[4].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[4].IsFallbackAnchor, Is.False);
        }

        [Test]
        public void BuildSkeleton_AnchorIndices_IgnoresNegativeAndOutOfRangeFallbackValues()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 8,
                sourceAnalysis: Analysis(AnchorIndicesOnly(8, -1, 2, 8)));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[2].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[2].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[2].IsFallbackAnchor, Is.True);

            for (int i = 0; i < pattern.TurnLengthSteps; i++)
            {
                if (i == 2)
                    continue;

                Assert.That(pattern.StepMeta[i].SourceAnchor, Is.False, "Unexpected anchor at step {0}", i);
                Assert.That(pattern.StepMeta[i].IsFallbackAnchor, Is.False, "Unexpected fallback anchor at step {0}", i);
            }
        }

        [Test]
        public void BuildSkeleton_AnchorProvenance_PreservesExplicitAndFallbackSources()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 16,
                sourceAnalysis: Analysis(ExplicitFlagsWithAnchorIndices(
                    flagLength: 16,
                    explicitAnchorIndices: new[] { 2 },
                    fallbackAnchorIndices: new[] { 2, 9 })));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[2].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[2].IsExplicitAnchor, Is.True);
            Assert.That(pattern.StepMeta[2].IsFallbackAnchor, Is.False);

            Assert.That(pattern.StepMeta[9].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[9].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[9].IsFallbackAnchor, Is.True);
        }

        [Test]
        public void BuildSkeleton_ExplicitAnchorFlags_TruncatesWhenLongerThanResponse()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 4,
                sourceAnalysis: Analysis(ExplicitAnchorFlagsOnly(8, 1, 6)));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[1].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[1].IsExplicitAnchor, Is.True);
            Assert.That(pattern.StepMeta[1].IsFallbackAnchor, Is.False);

            Assert.That(pattern.StepMeta[0].SourceAnchor, Is.False);
            Assert.That(pattern.StepMeta[2].SourceAnchor, Is.False);
            Assert.That(pattern.StepMeta[3].SourceAnchor, Is.False);
        }

        [Test]
        public void BuildSkeleton_AnchorProvenance_ConstrainsFallbackToAlignedPrefixWhenFlagLengthDiffers()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 8,
                sourceAnalysis: Analysis(ExplicitFlagsWithAnchorIndices(
                    flagLength: 4,
                    explicitAnchorIndices: new[] { 1 },
                    fallbackAnchorIndices: new[] { 1, 2, 6, 99 })));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[1].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[1].IsExplicitAnchor, Is.True);
            Assert.That(pattern.StepMeta[1].IsFallbackAnchor, Is.False);

            Assert.That(pattern.StepMeta[2].SourceAnchor, Is.True);
            Assert.That(pattern.StepMeta[2].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[2].IsFallbackAnchor, Is.True);

            Assert.That(pattern.StepMeta[6].SourceAnchor, Is.False);
            Assert.That(pattern.StepMeta[6].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[6].IsFallbackAnchor, Is.False);

            Assert.That(pattern.StepMeta[5].SourceAnchor, Is.False);
            Assert.That(pattern.StepMeta[5].IsExplicitAnchor, Is.False);
            Assert.That(pattern.StepMeta[5].IsFallbackAnchor, Is.False);
        }

        [Test]
        public void BuildSkeleton_PreserveAnchors_MarksProtectedAnchorMetadata()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 16,
                plan: Plan(16, preserveAnchors: true),
                sourceAnalysis: Analysis(ExplicitAnchorFlagsOnly(16, 4)));

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            Assert.That(pattern.StepMeta[4].Protected, Is.True);
            AssertHasFlag(pattern.StepMeta[4].ReasonFlags, SkeletonReasonFlags.ProtectedAnchor);
            Assert.That(pattern.StepMeta[5].Protected, Is.False);
        }

        [Test]
        public void BuildSkeleton_EndingRegion_UsesFinalQuarterNoteWindow()
        {
            var builder = new SkeletonBuilder();

            SkeletonPattern pattern = builder.BuildSkeleton(Request(turnLengthSteps: 48, stepsPerQuarter: 12));

            for (int i = 0; i < 36; i++)
                Assert.That(pattern.StepMeta[i].InEndingRegion, Is.False, "Ending flag before window at step {0}", i);

            for (int i = 36; i < 48; i++)
                Assert.That(pattern.StepMeta[i].InEndingRegion, Is.True, "Ending flag inside window at step {0}", i);
        }

        [Test]
        public void BuildSkeleton_EndingProximity_PopulatesStepsFromEnd()
        {
            var builder = new SkeletonBuilder();

            SkeletonPattern pattern = builder.BuildSkeleton(Request(turnLengthSteps: 48, stepsPerQuarter: 12));

            Assert.That(pattern.StepMeta[0].StepsFromEnd, Is.EqualTo(47));
            Assert.That(pattern.StepMeta[24].StepsFromEnd, Is.EqualTo(23));
            Assert.That(pattern.StepMeta[47].StepsFromEnd, Is.EqualTo(0));
        }

        [Test]
        public void BuildSkeleton_MetricMap_TreatsStepZeroAsStrongestGridOriginAndWouldCatchShift()
        {
            var builder = new SkeletonBuilder();

            SkeletonPattern pattern = builder.BuildSkeleton(Request(turnLengthSteps: 48, stepsPerQuarter: 12));

            Assert.That(pattern.StepMeta[0].MetricScore, Is.EqualTo(pattern.StepMeta[12].MetricScore));
            Assert.That(pattern.StepMeta[0].MetricScore, Is.GreaterThan(pattern.StepMeta[6].MetricScore));
            Assert.That(pattern.StepMeta[6].MetricScore, Is.GreaterThan(pattern.StepMeta[3].MetricScore));
            Assert.That(pattern.StepMeta[3].MetricScore, Is.GreaterThan(pattern.StepMeta[1].MetricScore));

            AssertHasFlag(pattern.StepMeta[0].ReasonFlags, SkeletonReasonFlags.MetricStrong);
            AssertHasFlag(pattern.StepMeta[12].ReasonFlags, SkeletonReasonFlags.MetricStrong);
            AssertHasFlag(pattern.StepMeta[24].ReasonFlags, SkeletonReasonFlags.MetricStrong);
            AssertHasFlag(pattern.StepMeta[36].ReasonFlags, SkeletonReasonFlags.MetricStrong);
            AssertLacksFlag(pattern.StepMeta[6].ReasonFlags, SkeletonReasonFlags.MetricStrong);
            AssertHasFlag(pattern.StepMeta[1].ReasonFlags, SkeletonReasonFlags.MetricWeak);

            Assert.That(pattern.StepMeta[0].IsStrongBeat, Is.True);
            Assert.That(pattern.StepMeta[1].IsStrongBeat, Is.False);
            Assert.That(pattern.StepMeta[12].IsStrongBeat, Is.True);
            Assert.That(pattern.StepMeta[6].IsStrongBeat, Is.False);
            Assert.That(pattern.StepMeta[11].IsStrongBeat, Is.False);
            Assert.That(pattern.StepMeta[3].IsStrongBeat, Is.False);
        }

        [Test]
        public void BuildSkeleton_MetricMap_IsDeterministic()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(turnLengthSteps: 48, stepsPerQuarter: 12);

            SkeletonPattern first = builder.BuildSkeleton(request);
            SkeletonPattern second = builder.BuildSkeleton(request);

            for (int i = 0; i < request.TurnLengthSteps; i++)
            {
                Assert.That(second.StepMeta[i].MetricScore, Is.EqualTo(first.StepMeta[i].MetricScore), "Metric score at step {0}", i);
                Assert.That(second.StepMeta[i].IsStrongBeat, Is.EqualTo(first.StepMeta[i].IsStrongBeat), "Strong beat flag at step {0}", i);
                Assert.That(second.StepMeta[i].ReasonFlags, Is.EqualTo(first.StepMeta[i].ReasonFlags), "Reason flags at step {0}", i);
            }
        }

        [Test]
        public void BuildSkeleton_SegmentMap_AssignsValidCoarseSegments()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 10,
                config: new SkeletonBuilderConfig
                {
                    RebalanceAcrossSegments = true
                });

            SkeletonPattern pattern = builder.BuildSkeleton(request);

            AssertSegment(pattern, 0, 0);
            AssertSegment(pattern, 1, 0);
            AssertSegment(pattern, 2, 0);
            AssertSegment(pattern, 3, 1);
            AssertSegment(pattern, 4, 1);
            AssertSegment(pattern, 5, 1);
            AssertSegment(pattern, 6, 2);
            AssertSegment(pattern, 7, 2);
            AssertSegment(pattern, 8, 3);
            AssertSegment(pattern, 9, 3);

            for (int i = 0; i < pattern.TurnLengthSteps; i++)
                Assert.That(pattern.StepMeta[i].SegmentIndex, Is.InRange(0, 3), "Segment index at step {0}", i);
        }

        [Test]
        public void BuildSkeleton_RawScores_PreserveMetricDominanceUnderLowDensity()
        {
            var builder = new SkeletonBuilder();
            SkeletonPattern pattern = builder.BuildSkeleton(Request(
                turnLengthSteps: 48,
                stepsPerQuarter: 12,
                plan: Plan(48, targetDensity: 0.10f),
                config: MetricFocusedConfig()));

            Assert.That(pattern.StepMeta[0].FinalScore, Is.GreaterThan(pattern.StepMeta[6].FinalScore));
            Assert.That(pattern.StepMeta[6].FinalScore, Is.GreaterThan(pattern.StepMeta[3].FinalScore));
            Assert.That(pattern.StepMeta[3].FinalScore, Is.GreaterThan(pattern.StepMeta[1].FinalScore));
            Assert.That(pattern.StepMeta[0].FinalScore, Is.GreaterThan(pattern.StepMeta[1].FinalScore));
        }

        [Test]
        public void BuildSkeleton_RawScores_MetricDominatesWeakSourceAndDensityFavour()
        {
            var builder = new SkeletonBuilder();
            SkeletonPattern pattern = builder.BuildSkeleton(Request(
                turnLengthSteps: 48,
                stepsPerQuarter: 12,
                sourceVelocities: Velocities(48, 1),
                plan: Plan(
                    48,
                    responseType: ResponseType.Mirror,
                    targetDensity: 0.90f,
                    complementarityBias: 0f),
                config: DominanceStressConfig()));

            Assert.That(pattern.StepMeta[0].IsStrongBeat, Is.True);
            AssertHasFlag(pattern.StepMeta[1].ReasonFlags, SkeletonReasonFlags.MetricWeak);
            Assert.That(pattern.StepMeta[1].SourceOccupied, Is.True);
            Assert.That(pattern.StepMeta[1].DensityShapingScore, Is.GreaterThan(0f));
            Assert.That(pattern.StepMeta[0].MetricScore, Is.GreaterThan(pattern.StepMeta[1].MetricScore));
            Assert.That(pattern.StepMeta[0].FinalScore, Is.GreaterThan(pattern.StepMeta[1].FinalScore));
        }

        [Test]
        public void BuildSkeleton_MirrorSourceRelation_RewardsSourceOccupiedComparableStep()
        {
            var builder = new SkeletonBuilder();
            SkeletonPattern pattern = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                sourceVelocities: Velocities(16, 0),
                plan: Plan(16, responseType: ResponseType.Mirror, complementarityBias: 0f),
                config: NoJitterConfig()));

            Assert.That(pattern.StepMeta[0].IsStrongBeat, Is.True);
            Assert.That(pattern.StepMeta[4].IsStrongBeat, Is.True);
            Assert.That(pattern.StepMeta[0].SourceRelationScore, Is.GreaterThan(pattern.StepMeta[4].SourceRelationScore));
            Assert.That(pattern.StepMeta[0].FinalScore, Is.GreaterThan(pattern.StepMeta[4].FinalScore));
        }

        [Test]
        public void BuildSkeleton_ComplementSourceRelation_RewardsComparableGapStep()
        {
            var builder = new SkeletonBuilder();
            SkeletonPattern pattern = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                sourceVelocities: Velocities(16, 0),
                plan: Plan(16, responseType: ResponseType.Complement, complementarityBias: 1f),
                config: NoJitterConfig()));

            Assert.That(pattern.StepMeta[0].IsStrongBeat, Is.True);
            Assert.That(pattern.StepMeta[4].IsStrongBeat, Is.True);
            Assert.That(pattern.StepMeta[4].SourceRelationScore, Is.GreaterThan(pattern.StepMeta[0].SourceRelationScore));
            Assert.That(pattern.StepMeta[4].FinalScore, Is.GreaterThan(pattern.StepMeta[0].FinalScore));
        }

        [Test]
        public void BuildSkeleton_SourceRelation_MirrorBonusExceedsComplementGapBonus()
        {
            var builder = new SkeletonBuilder();

            SkeletonPattern mirror = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                sourceVelocities: Velocities(16, 0),
                plan: Plan(16, responseType: ResponseType.Mirror, complementarityBias: 0f),
                config: NoJitterConfig()));

            SkeletonPattern complement = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                sourceVelocities: Velocities(16, 0),
                plan: Plan(16, responseType: ResponseType.Complement, complementarityBias: 1f),
                config: NoJitterConfig()));

            Assert.That(mirror.StepMeta[0].SourceRelationScore, Is.GreaterThan(complement.StepMeta[4].SourceRelationScore));
        }

        [Test]
        public void BuildSkeleton_PreserveAnchors_BoostsAnchorScoreWhenEnabled()
        {
            var builder = new SkeletonBuilder();
            TurnAnalysisResult analysis = Analysis(ExplicitAnchorFlagsOnly(16, 4));

            SkeletonPattern unprotected = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                plan: Plan(16, preserveAnchors: false),
                sourceAnalysis: analysis,
                config: NoJitterConfig()));

            SkeletonPattern protectedPattern = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                plan: Plan(16, preserveAnchors: true),
                sourceAnalysis: analysis,
                config: NoJitterConfig()));

            Assert.That(unprotected.StepMeta[4].AnchorScore, Is.EqualTo(0f));
            Assert.That(protectedPattern.StepMeta[4].AnchorScore, Is.GreaterThan(0f));
            Assert.That(protectedPattern.StepMeta[4].FinalScore, Is.GreaterThan(unprotected.StepMeta[4].FinalScore));
        }

        [Test]
        public void BuildSkeleton_PreserveAnchors_RanksExplicitAboveFallbackAboveNonAnchor()
        {
            var builder = new SkeletonBuilder();
            TurnAnalysisResult analysis = Analysis(ExplicitFlagsWithAnchorIndices(
                flagLength: 16,
                explicitAnchorIndices: new[] { 0 },
                fallbackAnchorIndices: new[] { 0, 4 }));

            SkeletonPattern pattern = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                plan: Plan(16, preserveAnchors: true),
                sourceAnalysis: analysis,
                config: AnchorPriorityConfig()));

            Assert.That(pattern.StepMeta[0].IsExplicitAnchor, Is.True);
            Assert.That(pattern.StepMeta[4].IsFallbackAnchor, Is.True);
            Assert.That(pattern.StepMeta[8].SourceAnchor, Is.False);
            Assert.That(pattern.StepMeta[0].AnchorScore, Is.GreaterThan(pattern.StepMeta[4].AnchorScore));
            Assert.That(pattern.StepMeta[4].AnchorScore, Is.GreaterThan(pattern.StepMeta[8].AnchorScore));
            Assert.That(pattern.StepMeta[0].FinalScore, Is.GreaterThan(pattern.StepMeta[4].FinalScore));
            Assert.That(pattern.StepMeta[4].FinalScore, Is.GreaterThan(pattern.StepMeta[8].FinalScore));
        }

        [Test]
        public void BuildSkeleton_EndingInfluence_AffectsOnlyEndingRegion()
        {
            var builder = new SkeletonBuilder();

            SkeletonPattern noEnding = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                plan: Plan(16, responseType: ResponseType.Intensify),
                config: NoEndingConfig()));

            SkeletonPattern withEnding = builder.BuildSkeleton(Request(
                turnLengthSteps: 16,
                stepsPerQuarter: 4,
                plan: Plan(16, responseType: ResponseType.Intensify),
                config: NoJitterConfig()));

            Assert.That(withEnding.StepMeta[12].InEndingRegion, Is.True);
            Assert.That(withEnding.StepMeta[8].InEndingRegion, Is.False);
            Assert.That(withEnding.StepMeta[12].EndingScore, Is.GreaterThan(0f));
            Assert.That(withEnding.StepMeta[12].FinalScore, Is.GreaterThan(noEnding.StepMeta[12].FinalScore));
            Assert.That(withEnding.StepMeta[8].EndingScore, Is.EqualTo(0f));
            Assert.That(withEnding.StepMeta[8].FinalScore, Is.EqualTo(noEnding.StepMeta[8].FinalScore));
        }

        [Test]
        public void BuildSkeleton_DensityShaping_SharpensLowDensityAndLiftsWeakHighDensity()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuilderConfig config = MetricFocusedConfig();

            SkeletonPattern lowDensity = builder.BuildSkeleton(Request(
                turnLengthSteps: 48,
                stepsPerQuarter: 12,
                plan: Plan(48, targetDensity: 0.10f),
                config: config));

            SkeletonPattern highDensity = builder.BuildSkeleton(Request(
                turnLengthSteps: 48,
                stepsPerQuarter: 12,
                plan: Plan(48, targetDensity: 0.90f),
                config: config));

            Assert.That(lowDensity.StepMeta[0].DensityShapingScore, Is.GreaterThan(lowDensity.StepMeta[1].DensityShapingScore));
            Assert.That(highDensity.StepMeta[1].DensityShapingScore, Is.GreaterThan(0f));
            Assert.That(highDensity.StepMeta[3].DensityShapingScore, Is.GreaterThan(0f));
            Assert.That(highDensity.StepMeta[1].DensityShapingScore, Is.LessThan(0.10f));
            Assert.That(highDensity.StepMeta[0].FinalScore, Is.GreaterThan(highDensity.StepMeta[1].FinalScore));
        }

        [Test]
        public void BuildSkeleton_DensityShaping_HighDensityDoesNotOutrankStrongestBeats()
        {
            var builder = new SkeletonBuilder();
            SkeletonPattern pattern = builder.BuildSkeleton(Request(
                turnLengthSteps: 48,
                stepsPerQuarter: 12,
                plan: Plan(48, targetDensity: 1f),
                config: MetricFocusedConfig()));

            Assert.That(pattern.StepMeta[1].DensityShapingScore, Is.GreaterThan(0f));
            Assert.That(pattern.StepMeta[0].FinalScore, Is.GreaterThan(pattern.StepMeta[1].FinalScore));
            Assert.That(pattern.StepMeta[12].FinalScore, Is.GreaterThan(pattern.StepMeta[11].FinalScore));
        }

        [Test]
        public void BuildSkeleton_ZeroJitter_ProducesIdenticalSelectionScores()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 48,
                stepsPerQuarter: 12,
                plan: Plan(48, responseType: ResponseType.Fill, targetDensity: 0.70f, complementarityBias: 0.75f),
                sourceVelocities: Velocities(48, 0, 7, 12, 31),
                sourceAnalysis: Analysis(ExplicitAnchorFlagsOnly(48, 12, 36)),
                config: NoJitterConfig());

            SkeletonPattern first = builder.BuildSkeleton(request);
            SkeletonPattern second = builder.BuildSkeleton(request);

            for (int i = 0; i < request.TurnLengthSteps; i++)
            {
                Assert.That(second.SelectionScores[i], Is.EqualTo(first.SelectionScores[i]), "Selection score at step {0}", i);
                Assert.That(second.StepMeta[i].FinalScore, Is.EqualTo(first.StepMeta[i].FinalScore), "Final score at step {0}", i);
                Assert.That(second.StepMeta[i].JitterOffset, Is.EqualTo(0f), "Jitter at step {0}", i);
            }
        }

        [Test]
        public void BuildSkeleton_JitteredScoreOrdering_IsDeterministic()
        {
            var builder = new SkeletonBuilder();
            SkeletonBuildRequest request = Request(
                turnLengthSteps: 48,
                stepsPerQuarter: 12,
                plan: Plan(48, responseType: ResponseType.Fill, targetDensity: 0.75f, complementarityBias: 0.80f),
                sourceVelocities: Velocities(48, 0, 3, 12, 17, 31),
                sourceAnalysis: Analysis(ExplicitAnchorFlagsOnly(48, 12, 36)),
                config: JitteredConfig());

            SkeletonPattern first = builder.BuildSkeleton(request);
            SkeletonPattern second = builder.BuildSkeleton(request);

            AssertSameScoreOrdering(first, second);
        }

        private static SkeletonBuildRequest Request(
            int turnLengthSteps = 48,
            int stepsPerQuarter = 12,
            int[] sourceVelocities = null,
            TurnAnalysisResult sourceAnalysis = null,
            ResponsePlan plan = null,
            SkeletonBuilderConfig config = null)
        {
            return new SkeletonBuildRequest
            {
                Plan = plan ?? Plan(turnLengthSteps),
                SourceTurn = new PatternTurn
                {
                    stepsPerQuarter = stepsPerQuarter,
                    velocity = sourceVelocities ?? new int[turnLengthSteps]
                },
                SourceAnalysis = sourceAnalysis ?? Analysis(),
                TurnLengthSteps = turnLengthSteps,
                StepsPerQuarter = stepsPerQuarter,
                Config = config ?? new SkeletonBuilderConfig()
            };
        }

        private static ResponsePlan Plan(
            int turnLengthSteps,
            ResponseType responseType = ResponseType.Mirror,
            float targetDensity = 0.50f,
            float complementarityBias = 0.25f,
            bool preserveAnchors = false,
            bool mirrorEnding = false)
        {
            return new ResponsePlan(
                responseType,
                targetDensity: targetDensity,
                complementarityBias: complementarityBias,
                preserveAnchors: preserveAnchors,
                mirrorEnding: mirrorEnding,
                turnLengthSteps: turnLengthSteps);
        }

        private static SkeletonBuilderConfig NoJitterConfig()
        {
            return new SkeletonBuilderConfig
            {
                SelectionJitter = 0f
            };
        }

        private static SkeletonBuilderConfig MetricFocusedConfig()
        {
            return new SkeletonBuilderConfig
            {
                MirrorWeight = 0f,
                ComplementWeight = 0f,
                AnchorInfluenceWeight = 0f,
                EndingInfluenceWeight = 0f,
                SelectionJitter = 0f,
                RebalanceAcrossSegments = false
            };
        }

        private static SkeletonBuilderConfig DominanceStressConfig()
        {
            return new SkeletonBuilderConfig
            {
                AnchorInfluenceWeight = 0f,
                EndingInfluenceWeight = 0f,
                SelectionJitter = 0f,
                RebalanceAcrossSegments = false
            };
        }

        private static SkeletonBuilderConfig AnchorPriorityConfig()
        {
            return new SkeletonBuilderConfig
            {
                MirrorWeight = 0f,
                ComplementWeight = 0f,
                EndingInfluenceWeight = 0f,
                SelectionJitter = 0f,
                RebalanceAcrossSegments = false
            };
        }

        private static SkeletonBuilderConfig JitteredConfig()
        {
            return new SkeletonBuilderConfig
            {
                SelectionJitter = 1f
            };
        }

        private static SkeletonBuilderConfig NoEndingConfig()
        {
            return new SkeletonBuilderConfig
            {
                EndingInfluenceWeight = 0f,
                SelectionJitter = 0f
            };
        }

        private static TurnAnalysisResult Analysis(AnchorFeatures anchor = null)
        {
            return new TurnAnalysisResult(
                new DensityFeatures(),
                new EnergyFeatures(),
                anchor ?? new AnchorFeatures(),
                new EndActivityFeatures(),
                new SegmentActivityProfileFeatures());
        }

        private static AnchorFeatures ExplicitAnchorFlagsOnly(int stepCount, params int[] anchorIndices)
        {
            return ExplicitFlagsWithAnchorIndices(stepCount, anchorIndices, null);
        }

        private static AnchorFeatures ExplicitFlagsWithAnchorIndices(
            int flagLength,
            int[] explicitAnchorIndices,
            int[] fallbackAnchorIndices)
        {
            var flags = new bool[flagLength];
            var salience = new float[flagLength];
            var anchorsPerSegment = new int[4];
            int[] explicitAnchors = explicitAnchorIndices ?? new int[0];
            int[] fallbackAnchors = fallbackAnchorIndices;

            for (int i = 0; i < explicitAnchors.Length; i++)
            {
                int anchorIndex = explicitAnchors[i];
                flags[anchorIndex] = true;
                salience[anchorIndex] = 0.75f;
                anchorsPerSegment[anchorIndex * 4 / flagLength]++;
            }

            return new AnchorFeatures(
                flagLength,
                salience,
                flags,
                explicitAnchors.Length,
                fallbackAnchors,
                explicitAnchors.Length > 0 ? explicitAnchors[0] : (int?)null,
                explicitAnchors.Length > 0 ? 0.75f : 0f,
                explicitAnchors.Length > 0 && explicitAnchors[0] == 0,
                explicitAnchors.Length > 0 && explicitAnchors[explicitAnchors.Length - 1] == flagLength - 1,
                anchorsPerSegment);
        }

        private static AnchorFeatures AnchorIndicesOnly(int stepCount, params int[] anchorIndices)
        {
            return new AnchorFeatures(
                stepCount,
                null,
                null,
                anchorIndices.Length,
                anchorIndices,
                anchorIndices.Length > 0 ? anchorIndices[0] : (int?)null,
                anchorIndices.Length > 0 ? 0.75f : 0f,
                false,
                false,
                null);
        }

        private static int[] Velocities(int stepCount, params int[] activeIndices)
        {
            var velocities = new int[stepCount];
            for (int i = 0; i < activeIndices.Length; i++)
                velocities[activeIndices[i]] = 96;

            return velocities;
        }

        private static void AssertSegment(SkeletonPattern pattern, int stepIndex, int expectedSegment)
        {
            Assert.That(pattern.StepMeta[stepIndex].SegmentIndex, Is.EqualTo(expectedSegment), "Segment index at step {0}", stepIndex);
        }

        private static void AssertHasFlag(SkeletonReasonFlags actual, SkeletonReasonFlags expected)
        {
            Assert.That((actual & expected) == expected, Is.True, "Expected flag {0} in {1}", expected, actual);
        }

        private static void AssertLacksFlag(SkeletonReasonFlags actual, SkeletonReasonFlags unexpected)
        {
            Assert.That((actual & unexpected) == 0, Is.True, "Unexpected flag {0} in {1}", unexpected, actual);
        }

        private static void AssertSameScoreOrdering(SkeletonPattern expected, SkeletonPattern actual)
        {
            Assert.That(actual.SelectionScores, Has.Length.EqualTo(expected.SelectionScores.Length));

            for (int left = 0; left < expected.SelectionScores.Length; left++)
            {
                for (int right = left + 1; right < expected.SelectionScores.Length; right++)
                {
                    Assert.That(
                        CompareScores(actual.SelectionScores[left], actual.SelectionScores[right]),
                        Is.EqualTo(CompareScores(expected.SelectionScores[left], expected.SelectionScores[right])),
                        "Score ordering for steps {0} and {1}",
                        left,
                        right);
                }
            }
        }

        private static int CompareScores(float left, float right)
        {
            if (left > right)
                return 1;

            if (left < right)
                return -1;

            return 0;
        }
    }
}
