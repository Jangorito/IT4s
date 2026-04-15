using System;
using System.Collections.Generic;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Rhythm.ResponsePlanning.Tests
{
    public sealed class ResponsePlannerTests
    {
        [Test]
        public void Plan_SparseLowEnergyWithoutWeakEnding_TendsTowardIntensify()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.20f,
                    energy: 0.20f,
                    endDensity: 0.35f,
                    endEnergy: 0.40f,
                    endAccent: 60));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Intensify));
            Assert.That(plan.MirrorEnding, Is.False);
        }

        [Test]
        public void Plan_SparseLowEnergyOpenEnding_PrefersFillWhenClosureIsPrimaryIssue()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.20f,
                    energy: 0.20f,
                    endDensity: 0.10f,
                    endEnergy: 0.10f,
                    endAccent: 0));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Fill));
            Assert.That(plan.MirrorEnding, Is.False);
        }

        [Test]
        public void Plan_BalancedMeaningfulAnchorsStrongEnding_TendsTowardMirror()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.80f,
                    endDensity: 0.75f,
                    endEnergy: 0.80f,
                    endAccent: 120));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Mirror));
            Assert.That(plan.PreserveAnchors, Is.True);
            Assert.That(plan.MirrorEnding, Is.True);
        }

        [Test]
        public void Plan_BalancedMeaningfulAnchorsWithConversationalSpace_CanPreferComplementOverMirror()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.45f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.80f));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Complement));
            Assert.That(plan.PreserveAnchors, Is.True);
            Assert.That(plan.MirrorEnding, Is.False);
        }

        [Test]
        public void Plan_BusyHighEnergy_TendsTowardSimplify()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.85f,
                    energy: 0.80f));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Simplify));
        }

        [Test]
        public void Plan_WithoutPredictableProfile_ContrastDoesNotBecomeDefaultFallback()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f));

            Assert.That(plan.ResponseType, Is.Not.EqualTo(ResponseType.Contrast));
        }

        [Test]
        public void Plan_TargetDensity_ClampsToConfiguredBoundsAndTurnLength()
        {
            var maxPlanner = Planner(
                new ResponsePlannerConfig
                {
                    MaxTargetDensity = 0.80f
                });

            ResponsePlan maxPlan = maxPlanner.Plan(
                Analysis(
                    density: 0.69f,
                    energy: 0.20f,
                    stepCount: 0,
                    endDensity: 0.10f,
                    endEnergy: 0.10f,
                    endAccent: 0));

            var minPlanner = Planner(
                new ResponsePlannerConfig
                {
                    MinTargetDensity = 0.70f,
                    MaxTargetDensity = 1.00f,
                    MaxTargetDensityDelta = 1.00f
                });

            ResponsePlan minPlan = minPlanner.Plan(
                Analysis(
                    density: 0.80f,
                    energy: 0.90f));

            Assert.That(maxPlan.TargetDensity, Is.EqualTo(0.80f).Within(0.0001f));
            Assert.That(maxPlan.TurnLengthSteps, Is.EqualTo(1));
            Assert.That(minPlan.TargetDensity, Is.EqualTo(0.70f).Within(0.0001f));
        }

        [Test]
        public void Plan_ComplementarityBias_ClampsToUnitInterval()
        {
            var highClampPlanner = Planner(
                new ResponsePlannerConfig
                {
                    ComplementarityAdjustment = 0.40f
                });

            ResponsePlan highClampPlan = highClampPlanner.Plan(
                Analysis(
                    density: 0.20f,
                    energy: 0.20f,
                    endDensity: 0.10f,
                    endEnergy: 0.10f,
                    endAccent: 0));

            var lowClampPlanner = Planner(
                new ResponsePlannerConfig
                {
                    ComplementarityAdjustment = 0.40f
                });

            ResponsePlan lowClampPlan = lowClampPlanner.Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.80f,
                    endDensity: 0.75f,
                    endEnergy: 0.80f,
                    endAccent: 120));

            Assert.That(highClampPlan.ComplementarityBias, Is.EqualTo(1.00f).Within(0.0001f));
            Assert.That(lowClampPlan.ComplementarityBias, Is.EqualTo(0.00f).Within(0.0001f));
        }

        [Test]
        public void Plan_PreserveAnchors_OnlyBecomesTrueWhenAnchorsAreMeaningful()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.40f,
                    endDensity: 0.75f,
                    endEnergy: 0.80f,
                    endAccent: 120));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Mirror));
            Assert.That(plan.PreserveAnchors, Is.False);
        }

        [Test]
        public void Plan_MirrorEnding_OnlyTrueForStrongMirrorResponses()
        {
            ResponsePlan mirrorPlan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.80f,
                    endDensity: 0.75f,
                    endEnergy: 0.80f,
                    endAccent: 120));

            ResponsePlan nonMirrorPlan = Planner().Plan(
                Analysis(
                    density: 0.85f,
                    energy: 0.80f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.80f,
                    endDensity: 0.75f,
                    endEnergy: 0.80f,
                    endAccent: 120));

            Assert.That(mirrorPlan.ResponseType, Is.EqualTo(ResponseType.Mirror));
            Assert.That(mirrorPlan.MirrorEnding, Is.True);
            Assert.That(nonMirrorPlan.ResponseType, Is.EqualTo(ResponseType.Simplify));
            Assert.That(nonMirrorPlan.MirrorEnding, Is.False);
        }

        [Test]
        public void Plan_SameInput_ProducesDeterministicPlan()
        {
            TurnAnalysisResult analysis = Analysis(
                density: 0.62f,
                energy: 0.58f,
                anchorCount: 1,
                strongestAnchorScore: 0.70f);

            ResponsePlan firstPlan = Planner().Plan(analysis);
            ResponsePlan secondPlan = Planner().Plan(analysis);

            AssertPlansEqual(firstPlan, secondPlan);
        }

        [Test]
        public void Plan_PopulatesLastSnapshotWithScoresAndFinalPlan()
        {
            ResponsePlanner planner = Planner();
            ResponsePlan plan = planner.Plan(
                Analysis(
                    density: 0.45f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.80f));

            ResponsePlannerDebugSnapshot snapshot = planner.LastSnapshot;

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.SelectedResponseType, Is.EqualTo(plan.ResponseType));
            Assert.That(snapshot.FinalPlan, Is.Not.Null);
            Assert.That(snapshot.FinalPlan.ResponseType, Is.EqualTo(plan.ResponseType));
            Assert.That(snapshot.PerResponseTypeScores, Has.Count.EqualTo(6));
            Assert.That(snapshot.SourceDescriptorSummary, Is.Not.Null);
            Assert.That(snapshot.SourceDescriptorSummary.Summary, Does.Contain("ConversationalSpace"));
            Assert.That(snapshot.SourceNumericSummary, Is.Not.Null);
            Assert.That(snapshot.SourceNumericSummary.AnchorCount, Is.EqualTo(2));

            Assert.That(HasScoreFor(snapshot.PerResponseTypeScores, ResponseType.Mirror), Is.True);
            Assert.That(HasScoreFor(snapshot.PerResponseTypeScores, ResponseType.Complement), Is.True);
            Assert.That(HasScoreFor(snapshot.PerResponseTypeScores, ResponseType.Simplify), Is.True);
            Assert.That(HasScoreFor(snapshot.PerResponseTypeScores, ResponseType.Intensify), Is.True);
            Assert.That(HasScoreFor(snapshot.PerResponseTypeScores, ResponseType.Contrast), Is.True);
            Assert.That(HasScoreFor(snapshot.PerResponseTypeScores, ResponseType.Fill), Is.True);
        }

        private static ResponsePlanner Planner(ResponsePlannerConfig config = null)
        {
            return config == null
                ? new ResponsePlanner()
                : new ResponsePlanner(config);
        }

        private static TurnAnalysisResult Analysis(
            float density,
            float energy,
            int stepCount = 16,
            int anchorCount = 0,
            float strongestAnchorScore = 0f,
            float endDensity = 0f,
            float endEnergy = 0f,
            int endAccent = 0,
            ActivityShape densityShape = ActivityShape.Flat,
            ActivityShape energyShape = ActivityShape.Flat)
        {
            return new TurnAnalysisResult(
                Density(density, stepCount),
                Energy(energy),
                Anchor(anchorCount, strongestAnchorScore, stepCount),
                EndActivity(endDensity, endEnergy, endAccent),
                new SegmentActivityProfileFeatures(densityShape, energyShape));
        }

        private static DensityFeatures Density(float density, int stepCount)
        {
            int safeStepCount = Math.Max(stepCount, 0);
            int activeSteps = (int)Math.Round(density * safeStepCount, MidpointRounding.AwayFromZero);

            return new DensityFeatures(
                stepCount,
                activeSteps,
                Math.Max(0, stepCount - activeSteps),
                density,
                Segments(density));
        }

        private static EnergyFeatures Energy(float energy)
        {
            float meanVelocity = energy * 127f;

            return new EnergyFeatures(
                meanVelocity,
                (int)Math.Round(meanVelocity, MidpointRounding.AwayFromZero),
                0f,
                Segments(meanVelocity),
                energy > 0.70f,
                energy < 0.35f,
                true,
                false,
                false,
                false);
        }

        private static AnchorFeatures Anchor(int anchorCount, float strongestAnchorScore, int stepCount)
        {
            int safeStepCount = Math.Max(stepCount, 1);
            int boundedAnchorCount = Math.Min(Math.Max(anchorCount, 0), safeStepCount);
            var salience = new float[safeStepCount];
            var anchorFlags = new bool[safeStepCount];
            var anchorIndices = new List<int>();
            var anchorsPerSegment = new int[4];

            for (int i = 0; i < boundedAnchorCount; i++)
            {
                salience[i] = strongestAnchorScore;
                anchorFlags[i] = true;
                anchorIndices.Add(i);
                anchorsPerSegment[Math.Min(i * 4 / safeStepCount, 3)]++;
            }

            return new AnchorFeatures(
                stepCount,
                salience,
                anchorFlags,
                boundedAnchorCount,
                anchorIndices,
                boundedAnchorCount > 0 ? 0 : (int?)null,
                strongestAnchorScore,
                boundedAnchorCount > 0,
                stepCount > 0 && anchorFlags[stepCount - 1],
                anchorsPerSegment);
        }

        private static EndActivityFeatures EndActivity(float density, float energy, int accent)
        {
            return new EndActivityFeatures(density, energy * 127f, accent);
        }

        private static IReadOnlyList<float> Segments(float value)
        {
            return new[] { value, value, value, value };
        }

        private static bool HasScoreFor(IReadOnlyList<ResponseTypeScore> scores, ResponseType responseType)
        {
            for (int i = 0; i < scores.Count; i++)
            {
                if (scores[i].ResponseType == responseType)
                    return true;
            }

            return false;
        }

        private static void AssertPlansEqual(ResponsePlan expected, ResponsePlan actual)
        {
            Assert.That(actual.ResponseType, Is.EqualTo(expected.ResponseType));
            Assert.That(actual.TargetDensity, Is.EqualTo(expected.TargetDensity).Within(0.0001f));
            Assert.That(actual.ComplementarityBias, Is.EqualTo(expected.ComplementarityBias).Within(0.0001f));
            Assert.That(actual.PreserveAnchors, Is.EqualTo(expected.PreserveAnchors));
            Assert.That(actual.MirrorEnding, Is.EqualTo(expected.MirrorEnding));
            Assert.That(actual.TurnLengthSteps, Is.EqualTo(expected.TurnLengthSteps));
        }
    }
}
