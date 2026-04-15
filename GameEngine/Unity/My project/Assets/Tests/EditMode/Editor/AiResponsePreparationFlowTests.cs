using System;
using System.Collections.Generic;
using IT4s.Data;
using IT4s.Orchestration;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.Transformations;
using IT4s.Rhythm.TurnAnalysis;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;
using NUnit.Framework;

namespace IT4s.Orchestration.Tests
{
    public sealed class AiResponsePreparationFlowTests
    {
        [Test]
        public void Prepare_InvokesCallbacksInAnalysedPlannedGeneratedOrder()
        {
            PatternTurn compiledPattern = CreateCompiledPatternTurn();
            ResponsePlan plannedResponse = CreateResponsePlan(ResponseType.Contrast);
            var flow = new AiResponsePreparationFlow(
                CreateTurnAnalyser(),
                new FixedResponsePlanner(plannedResponse),
                new FeatureTransformer());
            var callbackOrder = new List<string>();
            TurnAnalysisResult callbackAnalysis = null;
            ResponsePlan callbackPlan = null;
            PatternTurn callbackGeneratedPattern = null;

            AiResponsePreparationResult result = flow.Prepare(
                compiledPattern,
                analysis =>
                {
                    callbackOrder.Add("analysed");
                    callbackAnalysis = analysis;
                },
                plan =>
                {
                    callbackOrder.Add("planned");
                    callbackPlan = plan;
                },
                generatedPattern =>
                {
                    callbackOrder.Add("generated");
                    callbackGeneratedPattern = generatedPattern;
                });

            CollectionAssert.AreEqual(
                new[] { "analysed", "planned", "generated" },
                callbackOrder);
            Assert.That(result.Analysis, Is.SameAs(callbackAnalysis));
            Assert.That(result.ResponsePlan, Is.SameAs(plannedResponse));
            Assert.That(result.ResponsePlan, Is.SameAs(callbackPlan));
            Assert.That(result.GeneratedPattern, Is.SameAs(callbackGeneratedPattern));
            AssertPatternTurnsEqual(new FeatureTransformer().Transform(compiledPattern), result.GeneratedPattern);
        }

        [Test]
        public void Prepare_MissingResponsePlanner_StillAnalysesBeforeError()
        {
            PatternTurn compiledPattern = CreateCompiledPatternTurn();
            var flow = new AiResponsePreparationFlow(
                CreateTurnAnalyser(),
                null,
                new FeatureTransformer());
            var callbackOrder = new List<string>();
            TurnAnalysisResult callbackAnalysis = null;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => flow.Prepare(
                    compiledPattern,
                    analysis =>
                    {
                        callbackOrder.Add("analysed");
                        callbackAnalysis = analysis;
                    }));

            Assert.That(
                exception.Message,
                Is.EqualTo("GeneratingAiResponse failed because IResponsePlanner reference is missing."));
            CollectionAssert.AreEqual(new[] { "analysed" }, callbackOrder);
            Assert.That(callbackAnalysis, Is.Not.Null);
        }

        [Test]
        public void Prepare_MissingFeatureTransformer_StillAnalysesAndPlansBeforeError()
        {
            PatternTurn compiledPattern = CreateCompiledPatternTurn();
            ResponsePlan plannedResponse = CreateResponsePlan(ResponseType.Fill);
            var flow = new AiResponsePreparationFlow(
                CreateTurnAnalyser(),
                new FixedResponsePlanner(plannedResponse),
                null);
            var callbackOrder = new List<string>();
            TurnAnalysisResult callbackAnalysis = null;
            ResponsePlan callbackPlan = null;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => flow.Prepare(
                    compiledPattern,
                    analysis =>
                    {
                        callbackOrder.Add("analysed");
                        callbackAnalysis = analysis;
                    },
                    plan =>
                    {
                        callbackOrder.Add("planned");
                        callbackPlan = plan;
                    }));

            Assert.That(
                exception.Message,
                Is.EqualTo("GeneratingAiResponse failed because FeatureTransformer reference is missing."));
            CollectionAssert.AreEqual(new[] { "analysed", "planned" }, callbackOrder);
            Assert.That(callbackAnalysis, Is.Not.Null);
            Assert.That(callbackPlan, Is.SameAs(plannedResponse));
        }

        private static TurnAnalyser CreateTurnAnalyser()
        {
            return new TurnAnalyser(
                new DensityAnalyser(),
                new EnergyAnalyser(new EnergyThresholds(90f, 10f, 1f, 45f, 50f, 50f)),
                new AnchorAnalyser(),
                new EndActivityAnalyser(),
                new SegmentActivityProfileAnalyser(new SegmentActivityProfileThresholds(0.02f)));
        }

        private static PatternTurn CreateCompiledPatternTurn()
        {
            return new PatternTurn
            {
                turnId = 7,
                bpm = 120f,
                stepsPerQuarter = 4,
                sampleRate = 44100,
                startSamples = 0,
                endSamples = 352800,
                velocity = new[] { 100, 0, 0, 0, 70, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                offsetSamples = new int[16]
            };
        }

        private static ResponsePlan CreateResponsePlan(ResponseType responseType)
        {
            return new ResponsePlan(
                responseType,
                0.5f,
                0.5f,
                true,
                true,
                0.2f,
                0.2f,
                0.2f,
                16);
        }

        private static void AssertPatternTurnsEqual(PatternTurn expected, PatternTurn actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.turnId, Is.EqualTo(expected.turnId));
            Assert.That(actual.bpm, Is.EqualTo(expected.bpm));
            Assert.That(actual.stepsPerQuarter, Is.EqualTo(expected.stepsPerQuarter));
            Assert.That(actual.sampleRate, Is.EqualTo(expected.sampleRate));
            Assert.That(actual.startSamples, Is.EqualTo(expected.startSamples));
            Assert.That(actual.endSamples, Is.EqualTo(expected.endSamples));
            CollectionAssert.AreEqual(expected.velocity, actual.velocity);
            CollectionAssert.AreEqual(expected.offsetSamples, actual.offsetSamples);
        }

        private sealed class FixedResponsePlanner : IResponsePlanner
        {
            private readonly ResponsePlan responsePlan;

            public FixedResponsePlanner(ResponsePlan responsePlan)
            {
                this.responsePlan = responsePlan;
            }

            public ResponsePlan Plan(TurnAnalysisResult analysis)
            {
                return responsePlan;
            }
        }
    }
}
