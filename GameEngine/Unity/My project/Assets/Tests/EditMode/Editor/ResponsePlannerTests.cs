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
        public void Plan_StrongEndActivity_ReturnsFill()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    endDensity: 0.75f,
                    endEnergy: 0.8f,
                    endAccent: 120));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Fill));
        }

        [Test]
        public void Plan_StrongAnchorsWithHighSupport_ReturnsMirror()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    anchorCount: 2,
                    strongestAnchorScore: 0.8f,
                    averageSupport: 0.8f));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Mirror));
        }

        [Test]
        public void Plan_AnchorsWithLowSupportAndBelowMidLevel_ReturnsIntensify()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.3f,
                    energy: 0.45f,
                    anchorCount: 1,
                    strongestAnchorScore: 0.6f,
                    averageSupport: 0.2f));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Intensify));
        }

        [Test]
        public void Plan_AnchorsWithLowSupportAndMidLevelActivity_ReturnsComplement()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.6f,
                    energy: 0.6f,
                    anchorCount: 1,
                    strongestAnchorScore: 0.6f,
                    averageSupport: 0.2f));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Complement));
        }

        [Test]
        public void Plan_AnchorsPresentWithoutSupportExtremes_ReturnsComplement()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.6f,
                    energy: 0.6f,
                    anchorCount: 1,
                    strongestAnchorScore: 0.6f,
                    averageSupport: 0.5f));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Complement));
        }

        [Test]
        public void Plan_LowDensityOrLowEnergy_ReturnsIntensify()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.2f,
                    energy: 0.55f));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Intensify));
        }

        [Test]
        public void Plan_HighDensityAndHighEnergy_ReturnsSimplify()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.8f,
                    energy: 0.8f));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Simplify));
        }

        [Test]
        public void Plan_SharedEarlyDirectionalSap_ReturnsContrast()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    densityShape: ActivityShape.Decreasing,
                    energyShape: ActivityShape.Decreasing));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Contrast));
        }

        [Test]
        public void Plan_SharedLateDirectionalSap_ReturnsContrast()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    densityShape: ActivityShape.BackLoaded,
                    energyShape: ActivityShape.BackLoaded));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Contrast));
        }

        [Test]
        public void Plan_LateBiasedSapWithoutSharedDirection_ReturnsComplement()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    densityShape: ActivityShape.BackLoaded,
                    energyShape: ActivityShape.Flat));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Complement));
        }

        [Test]
        public void Plan_NoHigherPriorityRuleMatches_ReturnsMirror()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f));

            Assert.That(plan.Type, Is.EqualTo(ResponseType.Mirror));
        }

        [Test]
        public void Plan_ClampsContinuousOutputsToUnitRange()
        {
            ResponsePlan upperPlan = new ResponsePlanner(new ConstantRandomSource(1d)).Plan(
                Analysis(
                    density: 0.95f,
                    energy: 0.95f,
                    averageSupport: 0.2f,
                    endDensity: 1f,
                    endEnergy: 1f,
                    endAccent: 127));

            AssertPlanRange(upperPlan);
            Assert.That(upperPlan.TargetDensity, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(upperPlan.TargetEnergy, Is.EqualTo(1f).Within(0.0001f));

            ResponsePlan lowerPlan = new ResponsePlanner(new ConstantRandomSource(0d)).Plan(
                Analysis(
                    density: 0f,
                    energy: 0f,
                    anchorCount: 1,
                    strongestAnchorScore: 0.8f,
                    averageSupport: 0.8f));

            AssertPlanRange(lowerPlan);
            Assert.That(lowerPlan.TargetDensity, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(lowerPlan.TargetEnergy, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Plan_ClampsTurnLengthStepsToAtLeastOne()
        {
            ResponsePlan plan = Planner().Plan(
                Analysis(
                    density: 0.55f,
                    energy: 0.55f,
                    stepCount: 0));

            Assert.That(plan.TurnLengthSteps, Is.EqualTo(1));
        }

        [TestCase(ResponseType.Mirror, 0.225f, 0.325f, 0.15f)]
        [TestCase(ResponseType.Complement, 0.4f, 0.525f, 0.8f)]
        [TestCase(ResponseType.Simplify, 0.175f, 0.2f, 0.3f)]
        [TestCase(ResponseType.Intensify, 0.475f, 0.625f, 0.6f)]
        [TestCase(ResponseType.Contrast, 0.65f, 0.725f, 0.5f)]
        [TestCase(ResponseType.Fill, 0.75f, 0.825f, 0.7f)]
        public void Plan_AssignsRangedParametersPerResponseType(
            ResponseType responseType,
            float expectedVariationAmount,
            float expectedSyncopationBias,
            float expectedComplementarityBias)
        {
            ResponsePlan plan = Planner().Plan(AnalysisForType(responseType));

            Assert.That(plan.Type, Is.EqualTo(responseType));
            Assert.That(plan.VariationAmount, Is.EqualTo(expectedVariationAmount).Within(0.0001f));
            Assert.That(plan.SyncopationBias, Is.EqualTo(expectedSyncopationBias).Within(0.0001f));
            Assert.That(plan.ComplementarityBias, Is.EqualTo(expectedComplementarityBias).Within(0.0001f));
        }

        [Test]
        public void Plan_ModulatesComplementarityBiasFromSupportAfterRangeSampling()
        {
            ResponsePlan lowSupportPlan = Planner().Plan(
                Analysis(
                    density: 0.8f,
                    energy: 0.8f,
                    averageSupport: 0.2f));

            ResponsePlan highSupportPlan = Planner().Plan(
                Analysis(
                    density: 0.8f,
                    energy: 0.8f,
                    averageSupport: 0.8f));

            Assert.That(lowSupportPlan.Type, Is.EqualTo(ResponseType.Simplify));
            Assert.That(highSupportPlan.Type, Is.EqualTo(ResponseType.Simplify));
            Assert.That(lowSupportPlan.ComplementarityBias, Is.EqualTo(0.35f).Within(0.0001f));
            Assert.That(highSupportPlan.ComplementarityBias, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(lowSupportPlan.ComplementarityBias, Is.GreaterThan(highSupportPlan.ComplementarityBias));
        }

        [Test]
        public void Plan_SameSeed_ProducesDeterministicResults()
        {
            var firstPlanner = new ResponsePlanner(12345);
            var secondPlanner = new ResponsePlanner(12345);
            TurnAnalysisResult analysis = Analysis(
                density: 0.62f,
                energy: 0.58f,
                anchorCount: 1,
                strongestAnchorScore: 0.6f,
                averageSupport: 0.5f);

            ResponsePlan firstPlan = firstPlanner.Plan(analysis);
            ResponsePlan secondPlan = secondPlanner.Plan(analysis);

            AssertPlansEqual(firstPlan, secondPlan);
        }

        private static ResponsePlanner Planner()
        {
            return new ResponsePlanner(new ConstantRandomSource(0.5d));
        }

        private static TurnAnalysisResult AnalysisForType(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Mirror:
                    return Analysis(
                        density: 0.55f,
                        energy: 0.55f,
                        anchorCount: 2,
                        strongestAnchorScore: 0.8f,
                        averageSupport: 0.8f);

                case ResponseType.Complement:
                    return Analysis(
                        density: 0.6f,
                        energy: 0.6f,
                        anchorCount: 1,
                        strongestAnchorScore: 0.6f,
                        averageSupport: 0.5f);

                case ResponseType.Simplify:
                    return Analysis(
                        density: 0.8f,
                        energy: 0.8f,
                        averageSupport: 0.5f);

                case ResponseType.Intensify:
                    return Analysis(
                        density: 0.2f,
                        energy: 0.55f,
                        averageSupport: 0.5f);

                case ResponseType.Contrast:
                    return Analysis(
                        density: 0.55f,
                        energy: 0.55f,
                        averageSupport: 0.5f,
                        densityShape: ActivityShape.BackLoaded,
                        energyShape: ActivityShape.BackLoaded);

                case ResponseType.Fill:
                    return Analysis(
                        density: 0.55f,
                        energy: 0.55f,
                        averageSupport: 0.5f,
                        endDensity: 0.75f,
                        endEnergy: 0.8f,
                        endAccent: 120);

                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, "Unknown response type.");
            }
        }

        private static TurnAnalysisResult Analysis(
            float density,
            float energy,
            int stepCount = 16,
            int anchorCount = 0,
            float strongestAnchorScore = 0f,
            float averageSupport = 0.5f,
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
                AnchorSupport(averageSupport),
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

        private static AnchorSupportFeatures AnchorSupport(float averageSupport)
        {
            return new AnchorSupportFeatures(
                averageSupport,
                0f,
                0f,
                0f);
        }

        private static EndActivityFeatures EndActivity(float density, float energy, int accent)
        {
            return new EndActivityFeatures(
                density,
                energy * 127f,
                accent);
        }

        private static IReadOnlyList<float> Segments(float value)
        {
            return new[] { value, value, value, value };
        }

        private static void AssertPlanRange(ResponsePlan plan)
        {
            Assert.That(plan.TargetDensity, Is.InRange(0f, 1f));
            Assert.That(plan.TargetEnergy, Is.InRange(0f, 1f));
            Assert.That(plan.VariationAmount, Is.InRange(0f, 1f));
            Assert.That(plan.SyncopationBias, Is.InRange(0f, 1f));
            Assert.That(plan.ComplementarityBias, Is.InRange(0f, 1f));
            Assert.That(plan.TurnLengthSteps, Is.GreaterThanOrEqualTo(1));
        }

        private static void AssertPlansEqual(ResponsePlan expected, ResponsePlan actual)
        {
            Assert.That(actual.Type, Is.EqualTo(expected.Type));
            Assert.That(actual.TargetDensity, Is.EqualTo(expected.TargetDensity).Within(0.0001f));
            Assert.That(actual.TargetEnergy, Is.EqualTo(expected.TargetEnergy).Within(0.0001f));
            Assert.That(actual.PreserveAnchors, Is.EqualTo(expected.PreserveAnchors));
            Assert.That(actual.MirrorEnding, Is.EqualTo(expected.MirrorEnding));
            Assert.That(actual.VariationAmount, Is.EqualTo(expected.VariationAmount).Within(0.0001f));
            Assert.That(actual.SyncopationBias, Is.EqualTo(expected.SyncopationBias).Within(0.0001f));
            Assert.That(actual.ComplementarityBias, Is.EqualTo(expected.ComplementarityBias).Within(0.0001f));
            Assert.That(actual.TurnLengthSteps, Is.EqualTo(expected.TurnLengthSteps));
        }

        private sealed class ConstantRandomSource : IRandomSource
        {
            private readonly double sample;

            public ConstantRandomSource(double sample)
            {
                this.sample = sample;
            }

            public double NextDouble()
            {
                return sample;
            }
        }
    }
}
