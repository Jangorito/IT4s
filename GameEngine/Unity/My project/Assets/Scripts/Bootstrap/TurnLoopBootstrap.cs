using IT4s.Input;
using IT4s.Diagnostics;
using IT4s.Orchestration;
using IT4s.Rhythm;
using IT4s.Rhythm.Generation.Skeleton;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.Transformations;
using IT4s.Rhythm.TurnAnalysis;
using IT4s.Rhythm.TurnAnalysis.Analysers;
using IT4s.Rhythm.TurnAnalysis.Models;
using UnityEngine;

namespace IT4s.Bootstrap
{
    /// <summary>
    /// Scene composition root for the Intelli-Trading 4s turn-taking loop.
    /// This component wires the existing collaborators together, injects them into the
    /// TurnLoopController, and starts the loop. It deliberately contains no loop logic.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class TurnLoopBootstrap : MonoBehaviour
    {
        private const int BelaSampleRateHz = 44100;
        private const float DefaultHighEnergyMeanVelocity = 90f;
        private const float DefaultLowEnergyMeanVelocity = 10f;
        private const float DefaultFlatVarianceThreshold = 1f;
        private const float DefaultAccentPeakOverMeanThreshold = 45f;
        private const float DefaultCrescendoMinDelta = 50f;
        private const float DefaultDecrescendoMinDelta = 50f;
        private const float DefaultShapeEpsilon = 0.02f;

        [Header("Scene Components")]
        [SerializeField] private TurnLoopController turnLoopController;
        [SerializeField] private TurnCaptureController turnCaptureController;
        [SerializeField] private OscHitReceiver oscHitReceiver;
        [SerializeField] private IT4ChuckTurnPlayer aiTurnPlayer;

        [Header("Default Musical Timing")]
        [SerializeField] private float bpm = 120f;
        [SerializeField] private int beatsPerBar = 4;
        [SerializeField] private int barsPerTurn = 2;
        [SerializeField] private int stepsPerQuarter = 12;
        [SerializeField] private int sampleRate = BelaSampleRateHz;

        [Header("Loop Closure")]
        [SerializeField]
        [Tooltip("When enabled, the controller returns to WaitingForHuman after the AI response playback duration elapses.")]
        private bool returnToWaitingAfterAiPlayback;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Small grace period added after the estimated AI response duration before re-arming human input.")]
        private float aiPlaybackCompletionPaddingSeconds = 0.05f;

        [Header("Debug Logging")]
        [SerializeField]
        [Tooltip("When disabled, suppresses TurnLoopController debug messages while keeping warnings/errors intact.")]
        private bool enableTurnLoopDebugMessages = true;

        [Header("Evaluation Trace")]
        [SerializeField]
        [Tooltip("When enabled, prints a clean dissertation-ready trace of the main turn-loop phases.")]
        private bool enableEvaluationTrace;

        private PatternCompiler patternCompiler;
        private FeatureTransformer featureTransformer;
        private TurnAnalyser turnAnalyser;
        private IResponsePlanner responsePlanner;
        private ISkeletonBuilder skeletonBuilder;
        private SkeletonBuilderConfig skeletonBuilderConfig;
        private MusicalTimingConfig initialTimingConfig;
        private EvaluationTraceLogger evaluationTraceLogger;

        private void Awake()
        {
            RuntimeDebugLog.SuppressInformationalLogs = enableEvaluationTrace;

            if (!ValidateSceneReferences())
            {
                enabled = false;
                return;
            }

            turnLoopController.ConfigureDebugMessages(false);

            patternCompiler = new PatternCompiler();
            featureTransformer = new FeatureTransformer();
            turnAnalyser = CreateTurnAnalyser();
            responsePlanner = new ResponsePlanner();
            skeletonBuilder = new SkeletonBuilder();
            skeletonBuilderConfig = new SkeletonBuilderConfig();

            initialTimingConfig = new MusicalTimingConfig(
                bpm,
                beatsPerBar,
                barsPerTurn,
                stepsPerQuarter,
                sampleRate);

            if (!initialTimingConfig.IsValid(out string timingError))
            {
                Debug.LogError($"[TurnLoopBootstrap] Initial musical timing is invalid: {timingError}");
                enabled = false;
                return;
            }

            turnLoopController.InjectDependencies(
                turnCaptureController,
                oscHitReceiver != null ? oscHitReceiver.Buffer : null,
                patternCompiler,
                aiTurnPlayer,
                featureTransformer,
                turnAnalyser,
                responsePlanner,
                initialTimingConfig,
                oscHitReceiver,
                skeletonBuilder,
                skeletonBuilderConfig);

            turnLoopController.ConfigureReturnToWaitingAfterAiPlayback(
                returnToWaitingAfterAiPlayback,
                aiPlaybackCompletionPaddingSeconds);

            ConfigureEvaluationTrace();
            turnLoopController.ConfigureDebugMessages(enableTurnLoopDebugMessages && !enableEvaluationTrace);

            if (!enableEvaluationTrace)
            {
                RuntimeDebugLog.Log($"[TurnLoopBootstrap] Turn loop collaborators wired with timing {initialTimingConfig}.");
            }
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            turnLoopController.StartLoop();

            if (!enableEvaluationTrace)
            {
                RuntimeDebugLog.Log("[TurnLoopBootstrap] Turn loop started.");
            }
        }

        private void OnDestroy()
        {
            evaluationTraceLogger?.Dispose();
            evaluationTraceLogger = null;
        }

        private bool ValidateSceneReferences()
        {
            if (turnLoopController == null)
            {
                Debug.LogError("[TurnLoopBootstrap] No TurnLoopController assigned.");
                return false;
            }

            if (turnCaptureController == null)
            {
                Debug.LogError("[TurnLoopBootstrap] No TurnCaptureController assigned.");
                return false;
            }

            if (oscHitReceiver == null)
            {
                Debug.LogError("[TurnLoopBootstrap] No OscHitReceiver assigned.");
                return false;
            }

            if (aiTurnPlayer == null)
            {
                Debug.LogError("[TurnLoopBootstrap] No IT4ChuckTurnPlayer assigned.");
                return false;
            }

            return true;
        }

        private void ConfigureEvaluationTrace()
        {
            evaluationTraceLogger?.Dispose();
            evaluationTraceLogger = enableEvaluationTrace
                ? new EvaluationTraceLogger(turnLoopController)
                : null;
        }

        private static TurnAnalyser CreateTurnAnalyser()
        {
            return new TurnAnalyser(
                new DensityAnalyser(),
                new EnergyAnalyser(DefaultEnergyThresholds()),
                new AnchorAnalyser(),
                new EndActivityAnalyser(),
                new SegmentActivityProfileAnalyser(DefaultSegmentActivityProfileThresholds()));
        }

        private static EnergyThresholds DefaultEnergyThresholds()
        {
            return new EnergyThresholds(
                DefaultHighEnergyMeanVelocity,
                DefaultLowEnergyMeanVelocity,
                DefaultFlatVarianceThreshold,
                DefaultAccentPeakOverMeanThreshold,
                DefaultCrescendoMinDelta,
                DefaultDecrescendoMinDelta);
        }

        private static SegmentActivityProfileThresholds DefaultSegmentActivityProfileThresholds()
        {
            return new SegmentActivityProfileThresholds(DefaultShapeEpsilon);
        }
    }
}
