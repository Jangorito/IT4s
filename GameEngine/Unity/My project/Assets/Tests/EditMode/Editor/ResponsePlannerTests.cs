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
        public void Plan_BalancedAnchoredStrongEnding_ReturnsMirror()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.8f,
                    endDensity: 0.75f,
                    endEnergy: 0.8f,
                    endAccent: 120));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Mirror));
            Assert.That(plan.MirrorEnding, Is.True);
        }

        [Test]
        public void Plan_BalancedAnchoredGappedInput_PrefersComplementOverMirror()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.8f));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Complement));
            Assert.That(plan.PreserveAnchors, Is.True);
        }

        [Test]
        public void Plan_BusyHighEnergy_ReturnsSimplify()
        {
            ResponsePlan plan = Planner().Plan(Analysis(density: 0.85f, energy: 0.8f));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Simplify));
        }

        [Test]
        public void Plan_SparseLowEnergyWithoutWeakEnding_ReturnsIntensify()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.2f,
                    energy: 0.2f,
                    endDensity: 0.35f,
                    endEnergy: 0.4f,
                    endAccent: 60));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Intensify));
        }

        [Test]
        public void Plan_SparseLowEnergyOpenEnding_PrefersFillOverIntensify()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.2f,
                    energy: 0.2f,
                    endDensity: 0.1f,
                    endEnergy: 0.1f,
                    endAccent: 0));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Fill));
            Assert.That(plan.MirrorEnding, Is.False);
        }

        [Test]
        public void Plan_DirectionalHighEnergyWithoutMeaningfulAnchors_ReturnsContrast()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.8f,
                    densityShape: ActivityShape.BackLoaded,
                    energyShape: ActivityShape.BackLoaded));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Contrast));
        }

        [Test]
        public void Plan_ComplementWithMeaningfulAnchors_DoesNotDropAnchorPreservation()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 1,
                    strongestAnchorScore: 0.8f));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Complement));
            Assert.That(plan.PreserveAnchors, Is.True);
        }

        [Test]
        public void Plan_ClampsDensityAndTurnLengthStepsToConfiguredBounds()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.69f,
                    energy: 0.2f,
                    stepCount: 0,
                    endDensity: 0.1f,
                    endEnergy: 0.1f,
                    endAccent: 0));

            Assert.That(plan.TargetDensity, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(plan.TurnLengthSteps, Is.EqualTo(1));
        }

        [Test]
        public void Plan_SameInput_ProducesDeterministicPlan()
        {
            TurnAnalysisResult analysis = Analysis(
                density: 0.62f,
                energy: 0.58f,
                anchorCount: 1,
                strongestAnchorScore: 0.7f);

            ResponsePlan firstPlan = Planner().Plan(analysis);
            ResponsePlan secondPlan = Planner().Plan(analysis);

            AssertPlansEqual(firstPlan, secondPlan);
        }

        [Test]
        public void Plan_OutputsOnlyCorePlannerFields()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 1,
                    strongestAnchorScore: 0.8f));

            Assert.That(plan.ResponseType, Is.EqualTo(ResponseType.Complement));
            Assert.That(plan.TargetDensity, Is.InRange(0f, 1f));
            Assert.That(plan.ComplementarityBias, Is.InRange(0f, 1f));
            Assert.That(plan.TurnLengthSteps, Is.GreaterThanOrEqualTo(1));
        }

        private static ResponsePlanner Planner()
        {
            return new ResponsePlanner();
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
            int activeSteps = (int)Math.Round(density * Math.Max(stepCount, 0), MidpointRounding.AwayFromZero);

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
                energy > 0.7f,
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
