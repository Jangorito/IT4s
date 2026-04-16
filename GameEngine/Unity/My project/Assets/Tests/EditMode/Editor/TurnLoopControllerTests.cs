// using System.Collections.Generic;
// using System.Reflection;
// using System.Text.RegularExpressions;
// using IT4s.Data;
// using IT4s.Input;
// using IT4s.Orchestration;
// using IT4s.Rhythm;
// using IT4s.Rhythm.ResponsePlanning;
// using IT4s.Rhythm.ResponsePlanning.Models;
// using IT4s.Rhythm.Transformations;
// using IT4s.Rhythm.TurnAnalysis;
// using IT4s.Rhythm.TurnAnalysis.Analysers;
// using IT4s.Rhythm.TurnAnalysis.Models;
// using NUnit.Framework;
// using UnityEngine;
// using UnityEngine.TestTools;

// namespace IT4s.Orchestration.Tests
// {
//     public sealed class TurnLoopControllerTests
//     {
//         private const BindingFlags InstanceFlags =
//             BindingFlags.Instance |
//             BindingFlags.Public |
//             BindingFlags.NonPublic;

//         private readonly List<GameObject> createdObjects = new List<GameObject>();

//         [TearDown]
//         public void TearDown()
//         {
//             for (int i = createdObjects.Count - 1; i >= 0; i--)
//             {
//                 Object.DestroyImmediate(createdObjects[i]);
//             }

//             createdObjects.Clear();
//             LogAssert.NoUnexpectedReceived();
//         }

//         [Test]
//         public void InjectDependencies_StoresTurnAnalyserAndResponsePlanner()
//         {
//             ControllerHarness harness = CreateHarness();

//             Assert.That(GetPrivateField<TurnAnalyser>(harness.Controller, "turnAnalyser"), Is.SameAs(harness.TurnAnalyser));
//             Assert.That(GetPrivateField<IResponsePlanner>(harness.Controller, "responsePlanner"), Is.SameAs(harness.ResponsePlanner));
//         }

//         [Test]
//         public void TickGeneratingAiResponse_AnalysesPlansAndContinuesTemporaryGenerationInOrder()
//         {
//             ResponsePlan plannedResponse = new ResponsePlan(
//                 ResponseType.Contrast,
//                 0.7f,
//                 0.4f,
//                 false,
//                 true,
//                 16);
//             ControllerHarness harness = CreateHarness(new FixedResponsePlanner(plannedResponse));
//             PatternTurn compiledPattern = CreateCompiledPatternTurn();
//             PatternTurn expectedGeneratedPattern = new FeatureTransformer().Transform(compiledPattern);
//             var eventOrder = new List<string>();
//             TurnAnalysisResult analysedTurn = null;
//             TurnAnalysisResult plannedAnalysis = null;
//             ResponsePlan plannedTurn = null;
//             PatternTurn generatedPattern = null;
//             bool analysisWasStoredDuringEvent = false;
//             bool planWasStoredDuringEvent = false;
//             bool generatedPatternWasStoredDuringEvent = false;

//             harness.Controller.OnHumanTurnAnalysed += analysis =>
//             {
//                 if (analysis == null)
//                 {
//                     return;
//                 }

//                 eventOrder.Add("analysed");
//                 analysedTurn = analysis;
//                 analysisWasStoredDuringEvent =
//                     harness.Controller.HasLastAnalysisResult &&
//                     ReferenceEquals(harness.Controller.LastAnalysisResult, analysis);
//             };

//             harness.Controller.OnResponsePlanned += (analysis, plan) =>
//             {
//                 if (plan == null)
//                 {
//                     return;
//                 }

//                 eventOrder.Add("planned");
//                 plannedAnalysis = analysis;
//                 plannedTurn = plan;
//                 planWasStoredDuringEvent =
//                     harness.Controller.HasCurrentResponsePlan &&
//                     ReferenceEquals(harness.Controller.CurrentResponsePlan, plan) &&
//                     ReferenceEquals(harness.Controller.LastAnalysisResult, analysis);
//             };

//             harness.Controller.OnAiPatternGenerated += pattern =>
//             {
//                 if (pattern == null)
//                 {
//                     return;
//                 }

//                 eventOrder.Add("generated");
//                 generatedPattern = pattern;
//                 generatedPatternWasStoredDuringEvent =
//                     harness.Controller.HasLastGeneratedAiPatternTurn &&
//                     ReferenceEquals(harness.Controller.LastGeneratedAiPatternTurn, pattern);
//             };

//             PrepareGeneratingState(harness.Controller, compiledPattern);

//             LogAssert.Expect(
//                 LogType.Error,
//                 new Regex(@"\[TurnLoopController\] GeneratingAiResponse failed because IT4ChuckTurnPlayer is not ready\."));

//             harness.Controller.Tick();

//             CollectionAssert.AreEqual(
//                 new[] { "analysed", "planned", "generated" },
//                 eventOrder);
//             Assert.That(analysedTurn, Is.Not.Null);
//             Assert.That(analysisWasStoredDuringEvent, Is.True);
//             Assert.That(plannedAnalysis, Is.SameAs(analysedTurn));
//             Assert.That(plannedTurn, Is.SameAs(plannedResponse));
//             Assert.That(planWasStoredDuringEvent, Is.True);
//             Assert.That(generatedPattern, Is.Not.Null);
//             Assert.That(generatedPatternWasStoredDuringEvent, Is.True);
//             AssertPatternTurnsEqual(expectedGeneratedPattern, generatedPattern);
//             Assert.That(harness.Controller.CurrentPhase, Is.EqualTo(TurnPhase.Error));
//         }

//         [Test]
//         public void TickGeneratingAiResponse_EmitsStructuredResponsePlanSummary()
//         {
//             ResponsePlan plannedResponse = new ResponsePlan(
//                 ResponseType.Fill,
//                 0.625f,
//                 0.125f,
//                 true,
//                 false,
//                 12);
//             ControllerHarness harness = CreateHarness(new FixedResponsePlanner(plannedResponse));
//             PatternTurn compiledPattern = CreateCompiledPatternTurn();
//             var debugMessages = new List<string>();

//             harness.Controller.OnDebugMessage += debugMessages.Add;
//             PrepareGeneratingState(harness.Controller, compiledPattern);

//             LogAssert.Expect(
//                 LogType.Error,
//                 new Regex(@"\[TurnLoopController\] GeneratingAiResponse failed because IT4ChuckTurnPlayer is not ready\."));

//             harness.Controller.Tick();

//             Assert.That(
//                 debugMessages,
//                 Does.Contain(
//                     "Response plan: ResponseType=Fill, TargetDensity=0.625, ComplementarityBias=0.125, PreserveAnchors=True, MirrorEnding=False, TurnLengthSteps=12."));
//         }

//         [Test]
//         public void StartLoop_MissingTurnAnalyser_MovesToError()
//         {
//             ControllerHarness harness = CreateHarness(includeTurnAnalyser: false);

//             LogAssert.Expect(
//                 LogType.Error,
//                 new Regex(@"\[TurnLoopController\] Turn loop cannot start because TurnAnalyser is missing\."));

//             harness.Controller.StartLoop();

//             Assert.That(harness.Controller.CurrentPhase, Is.EqualTo(TurnPhase.Error));
//         }

//         [Test]
//         public void StartLoop_MissingResponsePlanner_MovesToError()
//         {
//             ControllerHarness harness = CreateHarness(includeResponsePlanner: false);

//             LogAssert.Expect(
//                 LogType.Error,
//                 new Regex(@"\[TurnLoopController\] Turn loop cannot start because IResponsePlanner is missing\."));

//             harness.Controller.StartLoop();

//             Assert.That(harness.Controller.CurrentPhase, Is.EqualTo(TurnPhase.Error));
//         }

//         [Test]
//         public void TickGeneratingAiResponse_MissingResponsePlanner_StillPublishesAnalysisBeforeError()
//         {
//             ControllerHarness harness = CreateHarness();
//             PatternTurn compiledPattern = CreateCompiledPatternTurn();
//             int analysisEventCount = 0;
//             TurnAnalysisResult analysedTurn = null;

//             harness.Controller.OnHumanTurnAnalysed += analysis =>
//             {
//                 if (analysis == null)
//                 {
//                     return;
//                 }

//                 analysisEventCount++;
//                 analysedTurn = analysis;
//             };

//             PrepareGeneratingState(harness.Controller, compiledPattern);
//             SetPrivateField(harness.Controller, "responsePlanner", null);

//             LogAssert.Expect(
//                 LogType.Error,
//                 new Regex(@"\[TurnLoopController\] GeneratingAiResponse failed because IResponsePlanner reference is missing\."));

//             harness.Controller.Tick();

//             Assert.That(analysisEventCount, Is.EqualTo(1));
//             Assert.That(analysedTurn, Is.Not.Null);
//             Assert.That(harness.Controller.HasLastAnalysisResult, Is.True);
//             Assert.That(harness.Controller.HasCurrentResponsePlan, Is.False);
//             Assert.That(harness.Controller.HasLastGeneratedAiPatternTurn, Is.False);
//             Assert.That(harness.Controller.CurrentPhase, Is.EqualTo(TurnPhase.Error));
//         }

//         [Test]
//         public void TickGeneratingAiResponse_MissingFeatureTransformer_StillPublishesAnalysisAndPlanBeforeError()
//         {
//             ResponsePlan plannedResponse = DefaultResponsePlan();
//             ControllerHarness harness = CreateHarness(new FixedResponsePlanner(plannedResponse));
//             PatternTurn compiledPattern = CreateCompiledPatternTurn();
//             var eventOrder = new List<string>();
//             TurnAnalysisResult analysedTurn = null;
//             ResponsePlan plannedTurn = null;

//             harness.Controller.OnHumanTurnAnalysed += analysis =>
//             {
//                 if (analysis == null)
//                 {
//                     return;
//                 }

//                 eventOrder.Add("analysed");
//                 analysedTurn = analysis;
//             };

//             harness.Controller.OnResponsePlanned += (analysis, plan) =>
//             {
//                 if (plan == null)
//                 {
//                     return;
//                 }

//                 eventOrder.Add("planned");
//                 plannedTurn = plan;
//                 Assert.That(analysis, Is.SameAs(analysedTurn));
//             };

//             PrepareGeneratingState(harness.Controller, compiledPattern);
//             SetPrivateField(harness.Controller, "featureTransformer", null);

//             LogAssert.Expect(
//                 LogType.Error,
//                 new Regex(@"\[TurnLoopController\] GeneratingAiResponse failed because FeatureTransformer reference is missing\."));

//             harness.Controller.Tick();

//             CollectionAssert.AreEqual(new[] { "analysed", "planned" }, eventOrder);
//             Assert.That(plannedTurn, Is.SameAs(plannedResponse));
//             Assert.That(harness.Controller.HasLastAnalysisResult, Is.True);
//             Assert.That(harness.Controller.HasCurrentResponsePlan, Is.True);
//             Assert.That(harness.Controller.HasLastGeneratedAiPatternTurn, Is.False);
//             Assert.That(harness.Controller.CurrentPhase, Is.EqualTo(TurnPhase.Error));
//         }

//         [Test]
//         public void ClearGeneratedAiResponseState_ClearsOnlyGeneratedPatternState()
//         {
//             ControllerHarness harness = CreateHarness();

//             InvokePrivateMethod(harness.Controller, "StoreAnalysisResult", new TurnAnalysisResult());
//             InvokePrivateMethod(
//                 harness.Controller,
//                 "StoreResponsePlan",
//                 new ResponsePlan(
//                     ResponseType.Mirror,
//                     0.5f,
//                     true,
//                     true,
//                     0.2f,
//                     16));
//             InvokePrivateMethod(harness.Controller, "StoreGeneratedAiPattern", CreateCompiledPatternTurn());

//             Assert.That(harness.Controller.HasLastAnalysisResult, Is.True);
//             Assert.That(harness.Controller.HasCurrentResponsePlan, Is.True);
//             Assert.That(harness.Controller.HasLastGeneratedAiPatternTurn, Is.True);

//             InvokePrivateMethod(harness.Controller, "ClearGeneratedAiResponseState");

//             Assert.That(harness.Controller.HasLastAnalysisResult, Is.True);
//             Assert.That(harness.Controller.LastAnalysisResult, Is.Not.Null);
//             Assert.That(harness.Controller.HasCurrentResponsePlan, Is.True);
//             Assert.That(harness.Controller.CurrentResponsePlan, Is.Not.Null);
//             Assert.That(harness.Controller.HasLastGeneratedAiPatternTurn, Is.False);
//             Assert.That(harness.Controller.LastGeneratedAiPatternTurn, Is.Null);
//         }

//         [Test]
//         public void ClearAnalysisAndPlanningRuntimeState_ClearsFieldsWithoutRaisingCompletionEvents()
//         {
//             ControllerHarness harness = CreateHarness();
//             int analysisEventCount = 0;
//             int planningEventCount = 0;

//             harness.Controller.OnHumanTurnAnalysed += _ => analysisEventCount++;
//             harness.Controller.OnResponsePlanned += (_, _) => planningEventCount++;

//             InvokePrivateMethod(harness.Controller, "StoreAnalysisResult", new TurnAnalysisResult());
//             InvokePrivateMethod(
//                 harness.Controller,
//                 "StoreResponsePlan",
//                 new ResponsePlan(
//                     ResponseType.Mirror,
//                     0.5f,
//                     true,
//                     true,
//                     0.2f,
//                     16));

//             Assert.That(analysisEventCount, Is.EqualTo(1));
//             Assert.That(planningEventCount, Is.EqualTo(1));

//             InvokePrivateMethod(harness.Controller, "ClearAnalysisAndPlanningRuntimeState");

//             Assert.That(analysisEventCount, Is.EqualTo(1));
//             Assert.That(planningEventCount, Is.EqualTo(1));
//             Assert.That(harness.Controller.HasLastAnalysisResult, Is.False);
//             Assert.That(harness.Controller.LastAnalysisResult, Is.Null);
//             Assert.That(harness.Controller.HasCurrentResponsePlan, Is.False);
//             Assert.That(harness.Controller.CurrentResponsePlan, Is.Null);
//         }

//         [Test]
//         public void ClearGeneratedAiResponseState_DoesNotRaiseAnalysisOrPlanningCompletionEvents()
//         {
//             ControllerHarness harness = CreateHarness();
//             int analysisEventCount = 0;
//             int planningEventCount = 0;

//             harness.Controller.OnHumanTurnAnalysed += _ => analysisEventCount++;
//             harness.Controller.OnResponsePlanned += (_, _) => planningEventCount++;

//             InvokePrivateMethod(harness.Controller, "StoreAnalysisResult", new TurnAnalysisResult());
//             InvokePrivateMethod(
//                 harness.Controller,
//                 "StoreResponsePlan",
//                 new ResponsePlan(
//                     ResponseType.Mirror,
//                     0.5f,
//                     true,
//                     true,
//                     0.2f,
//                     16));
//             InvokePrivateMethod(harness.Controller, "StoreGeneratedAiPattern", CreateCompiledPatternTurn());

//             Assert.That(analysisEventCount, Is.EqualTo(1));
//             Assert.That(planningEventCount, Is.EqualTo(1));

//             InvokePrivateMethod(harness.Controller, "ClearGeneratedAiResponseState");

//             Assert.That(analysisEventCount, Is.EqualTo(1));
//             Assert.That(planningEventCount, Is.EqualTo(1));
//             Assert.That(harness.Controller.HasLastAnalysisResult, Is.True);
//             Assert.That(harness.Controller.HasCurrentResponsePlan, Is.True);
//             Assert.That(harness.Controller.HasLastGeneratedAiPatternTurn, Is.False);
//         }

//         private ControllerHarness CreateHarness(
//             IResponsePlanner responsePlanner = null,
//             TurnAnalyser turnAnalyser = null,
//             bool includeResponsePlanner = true,
//             bool includeTurnAnalyser = true)
//         {
//             var controllerObject = CreateObject("TurnLoopController");
//             var captureObject = CreateObject("TurnCaptureController");
//             var receiverObject = CreateObject("OscHitReceiver");
//             var playerObject = CreateObject("IT4ChuckTurnPlayer");

//             var controller = controllerObject.AddComponent<TurnLoopController>();
//             var turnCaptureController = captureObject.AddComponent<TestTurnCaptureController>();
//             var hitReceiver = receiverObject.AddComponent<TestOscHitReceiver>();
//             var turnPlayer = playerObject.AddComponent<TestChuckTurnPlayer>();
//             HitBuffer hitBuffer = new HitBuffer();
//             PatternCompiler patternCompiler = new PatternCompiler();
//             FeatureTransformer featureTransformer = new FeatureTransformer();
//             TurnAnalyser resolvedAnalyser = includeTurnAnalyser
//                 ? (turnAnalyser ?? CreateTurnAnalyser())
//                 : null;
//             IResponsePlanner resolvedPlanner = includeResponsePlanner
//                 ? (responsePlanner ?? new FixedResponsePlanner(DefaultResponsePlan()))
//                 : null;

//             controller.InjectDependencies(
//                 turnCaptureController,
//                 hitBuffer,
//                 patternCompiler,
//                 turnPlayer,
//                 featureTransformer,
//                 resolvedAnalyser,
//                 resolvedPlanner,
//                 CreateTimingConfig(),
//                 hitReceiver);

//             return new ControllerHarness(
//                 controller,
//                 turnCaptureController,
//                 hitReceiver,
//                 turnPlayer,
//                 hitBuffer,
//                 resolvedAnalyser,
//                 resolvedPlanner);
//         }

//         private GameObject CreateObject(string name)
//         {
//             var gameObject = new GameObject(name);
//             createdObjects.Add(gameObject);
//             return gameObject;
//         }

//         private static void PrepareGeneratingState(TurnLoopController controller, PatternTurn compiledPattern)
//         {
//             controller.StartLoop();
//             InvokePrivateMethod(controller, "StoreCompiledHumanPattern", compiledPattern);
//             InvokePrivateMethod(controller, "SetPhase", TurnPhase.GeneratingAiResponse);
//         }

//         private static TurnAnalyser CreateTurnAnalyser()
//         {
//             return new TurnAnalyser(
//                 new DensityAnalyser(),
//                 new EnergyAnalyser(new EnergyThresholds(90f, 10f, 1f, 45f, 50f, 50f)),
//                 new AnchorAnalyser(),
//                 new EndActivityAnalyser(),
//                 new SegmentActivityProfileAnalyser(new SegmentActivityProfileThresholds(0.02f)));
//         }

//         private static MusicalTimingConfig CreateTimingConfig()
//         {
//             return new MusicalTimingConfig(
//                 120f,
//                 4,
//                 2,
//                 4,
//                 44100);
//         }

//         private static PatternTurn CreateCompiledPatternTurn()
//         {
//             return new PatternTurn
//             {
//                 turnId = 7,
//                 bpm = 120f,
//                 stepsPerQuarter = 4,
//                 sampleRate = 44100,
//                 startSamples = 0,
//                 endSamples = 352800,
//                 velocity = new[] { 100, 0, 0, 0, 70, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
//                 offsetSamples = new int[16]
//             };
//         }

//         private static ResponsePlan DefaultResponsePlan()
//         {
//             return new ResponsePlan(
//                 ResponseType.Mirror,
//                 0.5f,
//                 0.2f,
//                 true,
//                 true,
//                 16);
//         }

//         private static void AssertPatternTurnsEqual(PatternTurn expected, PatternTurn actual)
//         {
//             Assert.That(actual, Is.Not.Null);
//             Assert.That(actual.turnId, Is.EqualTo(expected.turnId));
//             Assert.That(actual.bpm, Is.EqualTo(expected.bpm));
//             Assert.That(actual.stepsPerQuarter, Is.EqualTo(expected.stepsPerQuarter));
//             Assert.That(actual.sampleRate, Is.EqualTo(expected.sampleRate));
//             Assert.That(actual.startSamples, Is.EqualTo(expected.startSamples));
//             Assert.That(actual.endSamples, Is.EqualTo(expected.endSamples));
//             CollectionAssert.AreEqual(expected.velocity, actual.velocity);
//             CollectionAssert.AreEqual(expected.offsetSamples, actual.offsetSamples);
//         }

//         private static void InvokePrivateMethod(object target, string methodName, params object[] arguments)
//         {
//             MethodInfo method = target.GetType().GetMethod(methodName, InstanceFlags);
//             Assert.That(method, Is.Not.Null, $"Expected private method '{methodName}' to exist.");
//             method.Invoke(target, arguments);
//         }

//         private static T GetPrivateField<T>(object target, string fieldName)
//         {
//             FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);
//             Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
//             return (T)field.GetValue(target);
//         }

//         private static void SetPrivateField(object target, string fieldName, object value)
//         {
//             FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);
//             Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' to exist.");
//             field.SetValue(target, value);
//         }

//         private sealed class ControllerHarness
//         {
//             public ControllerHarness(
//                 TurnLoopController controller,
//                 TurnCaptureController turnCaptureController,
//                 OscHitReceiver hitReceiver,
//                 IT4ChuckTurnPlayer turnPlayer,
//                 HitBuffer hitBuffer,
//                 TurnAnalyser turnAnalyser,
//                 IResponsePlanner responsePlanner)
//             {
//                 Controller = controller;
//                 TurnCaptureController = turnCaptureController;
//                 HitReceiver = hitReceiver;
//                 TurnPlayer = turnPlayer;
//                 HitBuffer = hitBuffer;
//                 TurnAnalyser = turnAnalyser;
//                 ResponsePlanner = responsePlanner;
//             }

//             public TurnLoopController Controller { get; }
//             public TurnCaptureController TurnCaptureController { get; }
//             public OscHitReceiver HitReceiver { get; }
//             public IT4ChuckTurnPlayer TurnPlayer { get; }
//             public HitBuffer HitBuffer { get; }
//             public TurnAnalyser TurnAnalyser { get; }
//             public IResponsePlanner ResponsePlanner { get; }
//         }

//         private sealed class FixedResponsePlanner : IResponsePlanner
//         {
//             private readonly ResponsePlan responsePlan;

//             public FixedResponsePlanner(ResponsePlan responsePlan)
//             {
//                 this.responsePlan = responsePlan;
//             }

//             public ResponsePlan Plan(TurnAnalysisResult analysis)
//             {
//                 return responsePlan;
//             }
//         }

//         private sealed class TestTurnCaptureController : TurnCaptureController
//         {
//             private new void Awake()
//             {
//             }
//         }

//         private sealed class TestOscHitReceiver : OscHitReceiver
//         {
//             private new void Awake()
//             {
//             }
//         }

//         private sealed class TestChuckTurnPlayer : IT4ChuckTurnPlayer
//         {
//             private new void Awake()
//             {
//             }
//         }
//     }
// }
