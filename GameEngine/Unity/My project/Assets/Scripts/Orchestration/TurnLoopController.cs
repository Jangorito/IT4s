using System;
using System.Globalization;
using IT4s.Data;
using IT4s.Input;
using IT4s.Rhythm;
using IT4s.Rhythm.ResponsePlanning;
using IT4s.Rhythm.ResponsePlanning.Models;
using IT4s.Rhythm.Transformations;
using IT4s.Rhythm.TurnAnalysis;
using IT4s.Rhythm.TurnAnalysis.Models;
using UnityEngine;

namespace IT4s.Orchestration
{
    /// <summary>
    /// High-level phases for the Intelli-Trading 4s human-to-AI turn loop.
    /// The enum is defined up front so UI, logging, and tests can depend on a stable
    /// vocabulary even before each phase has concrete runtime logic behind it.
    /// </summary>
    public enum TurnPhase
    {
        /// <summary>
        /// The loop is active but waiting for the human performer to begin the next turn.
        /// </summary>
        WaitingForHuman,

        /// <summary>
        /// A human turn is being captured from the input systems.
        /// </summary>
        CapturingHuman,

        /// <summary>
        /// The captured human material is being converted into a structured rhythmic pattern.
        /// </summary>
        CompilingHumanTurn,

        /// <summary>
        /// An AI or rule-based response is being derived from the compiled human turn.
        /// </summary>
        GeneratingAiResponse,

        /// <summary>
        /// The system is rendering or streaming the AI response back to the performer.
        /// </summary>
        PlayingAiResponse,

        /// <summary>
        /// A short hand-off phase between major steps in the loop.
        /// </summary>
        Transition,

        /// <summary>
        /// A terminal or recoverable failure state that keeps orchestration observable.
        /// </summary>
        Error

        // TODO: Debugging mode phase that plays captured turn before generating a response
    }

    /// <summary>
    /// Orchestration owner for the turn-taking loop.
    /// This controller remains the conductor: it owns phases, lifecycle glue, dependency
    /// validation, observable state publication, error transitions, and playback triggering.
    /// The synchronous AI preparation sub-flow is extracted into a focused plain C# collaborator,
    /// while capture and playback coordination still stay here for this first refactor step.
    /// </summary>
    public class TurnLoopController : MonoBehaviour
    {
        [Header("Scene Dependencies")]
        [SerializeField]
        [Tooltip("Component responsible for opening and closing human capture windows.")]
        private TurnCaptureController turnCaptureController;

        [SerializeField]
        [Tooltip("Receiver supplying incoming Bela hits and the current sample-time estimate.")]
        private OscHitReceiver hitReceiver;

        [SerializeField]
        [Tooltip("Playback endpoint that will render AI responses once orchestration reaches that phase.")]
        private IT4ChuckTurnPlayer aiTurnPlayer;

        [Header("Debug Start Gate")]
        [SerializeField]
        [Tooltip("Temporary debug toggle controlling whether hits are allowed to start a turn.")]
        private bool debugHitValidityEnabled = true;

        [SerializeField]
        [Tooltip("Optional extra debug gate. When enabled, the hold key must be pressed for a hit to start a turn.")]
        private bool requireDebugHoldKeyForStart;

        [SerializeField]
        [Tooltip("Hold this key to allow hits to start a turn when the hold-key gate is enabled.")]
        private KeyCode debugHoldKey = KeyCode.LeftShift;

        // These are plain C# collaborators rather than scene components, so they are expected
        // to be supplied by a composition root or setup code through InjectDependencies(...).
        // The controller never creates them internally because orchestration should only coordinate.
        private HitBuffer hitBuffer;
        private PatternCompiler patternCompiler;
        private FeatureTransformer featureTransformer;
        private TurnAnalyser turnAnalyser;
        private IResponsePlanner responsePlanner;
        private MusicalTimingConfig currentMusicalTiming;

        // Only the controller may change phase. External systems can observe CurrentPhase,
        // but all transitions are funnelled through SetPhase(...) for consistency and logging.
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Transition;
        public MusicalTimingConfig CurrentMusicalTiming => currentMusicalTiming;

        // Running state is kept separate from CurrentPhase so the loop can be paused or stopped
        // without needing extra domain phases before they are truly justified.
        private bool isRunning;

        private HitEvent lastTriggerHit;
        private bool hasLastTriggerHit;
        private long startSamples = -1;
        private long endSamples = -1;
        private TurnWindow lastTurnWindow;
        private bool hasLastTurnWindow;
        private PatternTurn lastCompiledPatternTurn;
        private bool hasLastCompiledPatternTurn;
        private TurnAnalysisResult lastAnalysisResult;
        private bool hasLastAnalysisResult;
        private ResponsePlan currentResponsePlan;
        private bool hasCurrentResponsePlan;
        private PatternTurn lastGeneratedAiPatternTurn;
        private bool hasLastGeneratedAiPatternTurn;
        private bool captureClockWarningIssued;
        private OscHitReceiver subscribedHitReceiver;

        // Events provide observability without forcing UI or debug tools to poll the controller.
        // They also make the orchestration layer easier to test because state changes are explicit.
        public TurnWindow LastTurnWindow => lastTurnWindow;
        public bool HasLastTurnWindow => hasLastTurnWindow;
        public PatternTurn LastCompiledPatternTurn => lastCompiledPatternTurn;
        public bool HasLastCompiledPatternTurn => hasLastCompiledPatternTurn;
        public TurnAnalysisResult LastAnalysisResult => lastAnalysisResult;
        public bool HasLastAnalysisResult => hasLastAnalysisResult;
        public ResponsePlan CurrentResponsePlan => currentResponsePlan;
        public bool HasCurrentResponsePlan => hasCurrentResponsePlan;
        public PatternTurn LastGeneratedAiPatternTurn => lastGeneratedAiPatternTurn;
        public bool HasLastGeneratedAiPatternTurn => hasLastGeneratedAiPatternTurn;
        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<TurnWindow> OnHumanTurnCaptured;
        public event Action<PatternTurn> OnHumanPatternCompiled;
        public event Action<TurnAnalysisResult> OnHumanTurnAnalysed;
        public event Action<TurnAnalysisResult, ResponsePlan> OnResponsePlanned;
        public event Action<PatternTurn> OnAiPatternGenerated;
        public event Action<string> OnDebugMessage;

        private void Awake()
        {
            ResolveReceiverReference();
        }

        private void OnEnable()
        {
            RefreshHitSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeFromHitReceiver();
        }

        private void Update()
        {
            // Unity drives the controller through Update, but the real entry point remains Tick()
            // so future tests or alternate schedulers can execute the same state machine.
            Tick();
        }

        /// <summary>
        /// Supplies all collaborators from outside the controller.
        /// Scene components may still be assigned in the inspector, but this method ensures
        /// plain services such as HitBuffer and PatternCompiler can be injected cleanly.
        /// </summary>
        public void InjectDependencies(
            TurnCaptureController injectedTurnCaptureController,
            HitBuffer injectedHitBuffer,
            PatternCompiler injectedPatternCompiler,
            IT4ChuckTurnPlayer injectedTurnPlayer,
            FeatureTransformer injectedFeatureTransformer,
            TurnAnalyser injectedTurnAnalyser,
            IResponsePlanner injectedResponsePlanner,
            MusicalTimingConfig initialTimingConfig,
            OscHitReceiver injectedHitReceiver = null)
        {
            turnCaptureController = injectedTurnCaptureController;
            hitBuffer = injectedHitBuffer;
            patternCompiler = injectedPatternCompiler;
            aiTurnPlayer = injectedTurnPlayer;
            featureTransformer = injectedFeatureTransformer;
            turnAnalyser = injectedTurnAnalyser;
            responsePlanner = injectedResponsePlanner;
            hitReceiver = injectedHitReceiver != null ? injectedHitReceiver : hitReceiver;

            ResolveReceiverReference();
            RefreshHitSubscription();
            TryApplyMusicalTiming(initialTimingConfig, "Initial musical timing configured.");

            EmitDebugMessage($"Dependencies injected. {DescribeDependencyState()}");
        }

        /// <summary>
        /// Activates the orchestration loop and moves it into the human-ready idle state.
        /// No turn logic is executed here yet; this simply establishes the initial phase.
        /// </summary>
        public void StartLoop()
        {
            if (isRunning)
            {
                EmitDebugMessage("StartLoop was ignored because the turn loop is already running.");
                return;
            }

            ResolveReceiverReference();

            if (!ValidateCriticalDependencies())
            {
                return;
            }

            isRunning = true;
            ClearCaptureRuntimeState();
            EmitDebugMessage($"Turn loop started. {DescribeDependencyState()}");
            SetPhase(TurnPhase.WaitingForHuman);
        }

        /// <summary>
        /// Requests a full timing update in one explicit operation.
        /// Keeping updates centralised inside the controller makes runtime timing changes easy to
        /// validate, log, and reason about as the orchestration owner evolves.
        /// </summary>
        public bool UpdateMusicalTiming(MusicalTimingConfig newConfig)
        {
            return TryApplyMusicalTiming(newConfig, "Musical timing updated.");
        }

        public bool UpdateBpm(float bpm)
        {
            MusicalTimingConfig updated = currentMusicalTiming;
            updated.bpm = bpm;
            return TryApplyMusicalTiming(updated, "Musical timing BPM updated.");
        }

        public bool UpdateBeatsPerBar(int beatsPerBar)
        {
            MusicalTimingConfig updated = currentMusicalTiming;
            updated.beatsPerBar = beatsPerBar;
            return TryApplyMusicalTiming(updated, "Musical timing beats-per-bar updated.");
        }

        public bool UpdateBarsPerTurn(int barsPerTurn)
        {
            MusicalTimingConfig updated = currentMusicalTiming;
            updated.barsPerTurn = barsPerTurn;
            return TryApplyMusicalTiming(updated, "Musical timing bars-per-turn updated.");
        }

        public bool UpdateStepsPerQuarter(int stepsPerQuarter)
        {
            MusicalTimingConfig updated = currentMusicalTiming;
            updated.stepsPerQuarter = stepsPerQuarter;
            return TryApplyMusicalTiming(updated, "Musical timing steps-per-quarter updated.");
        }

        /// <summary>
        /// Stops orchestration without destroying any collaborators.
        /// The controller returns to Transition so observers have a neutral resting state to display.
        /// </summary>
        public void StopLoop()
        {
            if (!isRunning)
            {
                EmitDebugMessage("StopLoop was ignored because the turn loop is not running.");
                return;
            }

            if (turnCaptureController != null && turnCaptureController.IsCapturing)
            {
                turnCaptureController.CancelCapture();
            }

            isRunning = false;
            ClearCaptureRuntimeState();
            SetPhase(TurnPhase.Transition);
            EmitDebugMessage("Turn loop stopped.");
        }

        /// <summary>
        /// Central state-machine tick.
        /// The controller owns phase advancement and decides when each concrete sub-flow runs.
        /// </summary>
        public void Tick()
        {
            if (!isRunning)
            {
                return;
            }

            switch (CurrentPhase)
            {
                case TurnPhase.WaitingForHuman:
                    // Waiting is event-driven in Chunk 2. Incoming hits are handled by HandleHitReceived.
                    break;

                case TurnPhase.CapturingHuman:
                    TickCapturingHuman();
                    break;

                case TurnPhase.CompilingHumanTurn:
                    TickCompilingHumanTurn();
                    break;

                case TurnPhase.GeneratingAiResponse:
                    TickGeneratingAiResponse();
                    break;

                case TurnPhase.PlayingAiResponse:
                    // Future implementation:
                    // Hand the generated response to IT4ChuckTurnPlayer and observe completion so the
                    // loop can return cleanly to the next waiting or transition state.
                    break;

                case TurnPhase.Transition:
                    // Future implementation:
                    // Perform lightweight hand-off work between phases, such as buffer resets, transport
                    // alignment, or any housekeeping needed before the next active state begins.
                    break;

                case TurnPhase.Error:
                    // Future implementation:
                    // Hold orchestration in a known state when a dependency or runtime step fails.
                    // Keeping Error explicit makes failure visible to UI and debugging tools.
                    break;
            }
        }

        /// <summary>
        /// Temporary debug hook for enabling or disabling the acceptance of human hits.
        /// The flag is stored now so later capture logic can respect it without changing the API.
        /// </summary>
        public void SetHitValidity(bool enabled)
        {
            debugHitValidityEnabled = enabled;
            EmitDebugMessage(
                $"Debug hit validity set to {(debugHitValidityEnabled ? "enabled" : "disabled")}. " +
                "Chunk 2 uses this gate to allow or ignore hits while waiting for the human turn.");
        }

        /// <summary>
        /// Single transition gateway for the controller.
        /// Centralising phase changes keeps observability consistent across runtime, UI, and tests.
        /// </summary>
        private void SetPhase(TurnPhase newPhase)
        {
            if (CurrentPhase == newPhase)
            {
                return;
            }

            CurrentPhase = newPhase;

            OnPhaseChanged?.Invoke(CurrentPhase);
            EmitDebugMessage($"Phase changed to {CurrentPhase}.");
        }

        private void EmitDebugMessage(string message)
        {
            Debug.Log($"[TurnLoopController] {message}");
            OnDebugMessage?.Invoke(message);
        }

        private string DescribeDependencyState()
        {
            // This summary is intentionally lightweight but very useful during early integration.
            // It makes the wiring visible in logs and overlays without stepping into implementation.
            return
                $"Dependencies: " +
                $"capture={(turnCaptureController != null ? "set" : "missing")}, " +
                $"receiver={(hitReceiver != null ? "set" : "missing")}, " +
                $"hitBuffer={(hitBuffer != null ? "set" : "missing")}, " +
                $"compiler={(patternCompiler != null ? "set" : "missing")}, " +
                $"turnPlayer={(aiTurnPlayer != null ? "set" : "missing")}, " +
                $"featureTransformer={(featureTransformer != null ? "set" : "missing")}, " +
                $"turnAnalyser={(turnAnalyser != null ? "set" : "missing")}, " +
                $"responsePlanner={(responsePlanner != null ? "set" : "missing")}, " +
                $"timing={DescribeTimingState()}.";
        }

        private void ResolveReceiverReference()
        {
            if (turnCaptureController != null && hitReceiver == null)
            {
                hitReceiver = turnCaptureController.HitReceiver;
            }

            if (turnCaptureController != null && hitReceiver != null && turnCaptureController.HitReceiver != hitReceiver)
            {
                turnCaptureController.SetHitReceiver(hitReceiver);
            }

            if (hitBuffer == null && hitReceiver != null)
            {
                hitBuffer = hitReceiver.Buffer;
            }
        }

        private void RefreshHitSubscription()
        {
            ResolveReceiverReference();

            if (!isActiveAndEnabled)
            {
                return;
            }

            if (subscribedHitReceiver == hitReceiver)
            {
                return;
            }

            UnsubscribeFromHitReceiver();

            if (hitReceiver == null)
            {
                return;
            }

            hitReceiver.OnHitReceived += HandleHitReceived;
            subscribedHitReceiver = hitReceiver;
        }

        private void UnsubscribeFromHitReceiver()
        {
            if (subscribedHitReceiver == null)
            {
                return;
            }

            subscribedHitReceiver.OnHitReceived -= HandleHitReceived;
            subscribedHitReceiver = null;
        }

        public bool IsHitValidForStart()
        {
            if (!debugHitValidityEnabled)
            {
                return false;
            }

            if (!requireDebugHoldKeyForStart)
            {
                return true;
            }

            return UnityEngine.Input.GetKey(debugHoldKey);
        }

        private bool ValidateCriticalDependencies()
        {
            if (turnCaptureController == null)
            {
                MoveToError("Turn loop cannot start because TurnCaptureController is missing.");
                return false;
            }

            if (hitReceiver == null)
            {
                MoveToError("Turn loop cannot start because OscHitReceiver is missing.");
                return false;
            }

            if (!currentMusicalTiming.IsValid(out string timingError))
            {
                MoveToError($"Turn loop cannot start because musical timing is invalid: {timingError}");
                return false;
            }

            if (patternCompiler == null)
            {
                MoveToError("Turn loop cannot start because PatternCompiler is missing.");
                return false;
            }

            if (featureTransformer == null)
            {
                MoveToError("Turn loop cannot start because FeatureTransformer is missing.");
                return false;
            }

            if (turnAnalyser == null)
            {
                MoveToError("Turn loop cannot start because TurnAnalyser is missing.");
                return false;
            }

            if (responsePlanner == null)
            {
                MoveToError("Turn loop cannot start because IResponsePlanner is missing.");
                return false;
            }

            if (aiTurnPlayer == null)
            {
                MoveToError("Turn loop cannot start because IT4ChuckTurnPlayer is missing.");
                return false;
            }

            return true;
        }

        private void HandleHitReceived(HitEvent hitEvent)
        {
            if (!isRunning || CurrentPhase != TurnPhase.WaitingForHuman)
            {
                return;
            }

            if (hitEvent.tSamples < 0)
            {
                Debug.LogWarning($"[TurnLoopController] Ignored hit with invalid sample timestamp {hitEvent.tSamples}.");
                return;
            }

            if (!IsHitValidForStart())
            {
                return;
            }

            EmitDebugMessage($"Valid hit detected for turn start: {hitEvent}.");
            TryStartHumanTurn(hitEvent);
        }

        private void TryStartHumanTurn(HitEvent triggerHit)
        {
            ResolveReceiverReference();

            if (turnCaptureController == null)
            {
                MoveToError("Cannot start human turn because TurnCaptureController is missing.");
                return;
            }

            if (hitReceiver == null)
            {
                MoveToError("Cannot start human turn because OscHitReceiver is missing.");
                return;
            }

            long turnDurationSamples = currentMusicalTiming.GetTurnDurationSamples();

            if (triggerHit.tSamples > long.MaxValue - turnDurationSamples)
            {
                MoveToError(
                    $"Cannot schedule human turn end because start={triggerHit.tSamples} would overflow with duration={turnDurationSamples}.");
                return;
            }

            if (!turnCaptureController.TryBeginCapture(triggerHit.tSamples))
            {
                Debug.LogWarning($"[TurnLoopController] Failed to begin capture at {triggerHit.tSamples} samples.");
                return;
            }

            lastTriggerHit = triggerHit;
            hasLastTriggerHit = true;
            startSamples = triggerHit.tSamples;
            endSamples = startSamples + turnDurationSamples;
            captureClockWarningIssued = false;
            ClearAnalysisAndPlanningRuntimeState();

            EmitDebugMessage($"Human turn started at sample {startSamples}.");
            EmitDebugMessage(
                $"Human turn scheduled to end at sample {endSamples} " +
                $"(duration={turnDurationSamples} samples, {currentMusicalTiming.barsPerTurn} bars at {currentMusicalTiming.bpm} bpm).");
            SetPhase(TurnPhase.CapturingHuman);
        }

        private void TickCapturingHuman()
        {
            if (turnCaptureController == null)
            {
                MoveToError("Capture phase failed because TurnCaptureController is missing.");
                return;
            }

            if (!turnCaptureController.IsCapturing)
            {
                MoveToError("Capture phase lost sync because TurnCaptureController is no longer capturing.");
                return;
            }

            if (startSamples < 0 || endSamples < startSamples)
            {
                MoveToError($"Capture phase has invalid timing bounds start={startSamples}, end={endSamples}.");
                return;
            }

            if (!TryGetCurrentSampleTime(out long currentSamples))
            {
                if (!captureClockWarningIssued)
                {
                    Debug.LogWarning("[TurnLoopController] Capture phase is waiting for a valid sample clock from OscHitReceiver.");
                    captureClockWarningIssued = true;
                }

                return;
            }

            captureClockWarningIssued = false;

            if (currentSamples < endSamples)
            {
                return;
            }

            if (!turnCaptureController.TryEndCapture(endSamples, out var turnWindow))
            {
                MoveToError($"Capture phase failed to end cleanly at sample {endSamples}.");
                return;
            }

            StoreCapturedTurnWindow(turnWindow);

            EmitDebugMessage(
                $"Human turn ended at sample {turnWindow.endSamples}. Window={turnWindow.turnId}, hits={turnWindow.HitCount}.");

            SetPhase(TurnPhase.CompilingHumanTurn);
        }

        private void TickCompilingHumanTurn()
        {
            if (!HasLastTurnWindow)
            {
                MoveToError("CompilingHumanTurn failed because no TurnWindow was available.");
                return;
            }

            if (patternCompiler == null)
            {
                MoveToError("CompilingHumanTurn failed because PatternCompiler reference is missing.");
                return;
            }

            EmitDebugMessage($"Compiling human turn {LastTurnWindow.turnId} with {LastTurnWindow.HitCount} hits.");

            try
            {
                QuantisationSettings quantisation = currentMusicalTiming.ToQuantisationSettings();

                PatternTurn compiled = patternCompiler.Compile(LastTurnWindow, quantisation);

                if (compiled == null)
                {
                    MoveToError($"PatternCompiler returned null when compiling turn {LastTurnWindow.turnId}.");
                    return;
                }

                StoreCompiledHumanPattern(compiled);
                ClearAnalysisAndPlanningRuntimeState();
                ClearGeneratedAiResponseState();

                EmitDebugMessage(
                    $"Compilation succeeded for turn {compiled.turnId}. Steps={compiled.StepCount}, bpm={compiled.bpm}, sampleRate={compiled.sampleRate}.");

                ClearCaptureRuntimeState();
                SetPhase(TurnPhase.GeneratingAiResponse);
            }
            catch (Exception ex)
            {
                MoveToError($"Exception during PatternCompiler.Compile: {ex.Message}");
            }
        }

        private void TickGeneratingAiResponse()
        {
            if (!HasLastCompiledPatternTurn || LastCompiledPatternTurn == null)
            {
                MoveToError("GeneratingAiResponse failed because no compiled human PatternTurn was available.");
                return;
            }

            EmitDebugMessage(
                $"AI response generation started for turn {LastCompiledPatternTurn.turnId}. " +
                $"Source steps={LastCompiledPatternTurn.StepCount}, sampleRate={LastCompiledPatternTurn.sampleRate}.");

            AiResponsePreparationResult preparationResult;

            try
            {
                preparationResult = CreateAiResponsePreparationFlow().Prepare(
                    LastCompiledPatternTurn,
                    StoreAnalysisResult,
                    plan =>
                    {
                        StoreResponsePlan(plan);
                        EmitDebugMessage(FormatResponsePlanSummary(plan));
                    },
                    StoreGeneratedAiPattern);
            }
            catch (Exception ex)
            {
                MoveToError(ex.Message);
                return;
            }

            EmitDebugMessage(
                $"AI response generation succeeded for turn {preparationResult.GeneratedPattern.turnId}. " +
                $"Steps={preparationResult.GeneratedPattern.StepCount}, sampleRate={preparationResult.GeneratedPattern.sampleRate}.");

            if (aiTurnPlayer == null)
            {
                MoveToError("GeneratingAiResponse failed because IT4ChuckTurnPlayer reference is missing.");
                return;
            }

            if (!aiTurnPlayer.IsReady)
            {
                MoveToError("GeneratingAiResponse failed because IT4ChuckTurnPlayer is not ready.");
                return;
            }

            try
            {
                EmitDebugMessage($"Triggering AI playback for turn {LastGeneratedAiPatternTurn.turnId}.");
                aiTurnPlayer.PlayTurn(LastGeneratedAiPatternTurn);
                EmitDebugMessage(
                    $"AI playback triggered for turn {LastGeneratedAiPatternTurn.turnId}. Advancing to PlayingAiResponse.");

                SetPhase(TurnPhase.PlayingAiResponse);
            }
            catch (Exception ex)
            {
                MoveToError($"Exception during IT4ChuckTurnPlayer.PlayTurn: {ex.Message}");
            }
        }

        private bool TryGetCurrentSampleTime(out long currentSamples)
        {
            ResolveReceiverReference();

            if (hitReceiver == null)
            {
                currentSamples = 0;
                return false;
            }

            return hitReceiver.TryGetCurrentSampleTime(out currentSamples);
        }

        private void ClearCaptureRuntimeState()
        {
            hasLastTriggerHit = false;
            startSamples = -1;
            endSamples = -1;
            captureClockWarningIssued = false;
        }

        private void ClearGeneratedAiResponseState()
        {
            if (LastGeneratedAiPatternTurn == null && !HasLastGeneratedAiPatternTurn)
            {
                return;
            }

            StoreGeneratedAiPattern(null);
        }

        private void ClearAnalysisAndPlanningRuntimeState()
        {
            ClearResponsePlanState();
            ClearAnalysisResultState();
        }

        private void ClearAnalysisResultState()
        {
            if (LastAnalysisResult == null && !HasLastAnalysisResult)
            {
                return;
            }

            lastAnalysisResult = null;
            hasLastAnalysisResult = false;
        }

        private void ClearResponsePlanState()
        {
            if (CurrentResponsePlan == null && !HasCurrentResponsePlan)
            {
                return;
            }

            currentResponsePlan = null;
            hasCurrentResponsePlan = false;
        }

        private void StoreCapturedTurnWindow(TurnWindow turnWindow)
        {
            lastTurnWindow = turnWindow;
            hasLastTurnWindow = true;
            OnHumanTurnCaptured?.Invoke(turnWindow);
        }

        private void StoreCompiledHumanPattern(PatternTurn pattern)
        {
            lastCompiledPatternTurn = pattern;
            hasLastCompiledPatternTurn = pattern != null;
            OnHumanPatternCompiled?.Invoke(pattern);
        }

        private void StoreAnalysisResult(TurnAnalysisResult analysis)
        {
            lastAnalysisResult = analysis;
            hasLastAnalysisResult = analysis != null;

            if (analysis != null)
            {
                OnHumanTurnAnalysed?.Invoke(analysis);
            }
        }

        private void StoreResponsePlan(ResponsePlan plan)
        {
            currentResponsePlan = plan;
            hasCurrentResponsePlan = plan != null;

            if (plan != null)
            {
                OnResponsePlanned?.Invoke(LastAnalysisResult, plan);
            }
        }

        private void StoreGeneratedAiPattern(PatternTurn pattern)
        {
            lastGeneratedAiPatternTurn = pattern;
            hasLastGeneratedAiPatternTurn = pattern != null;
            OnAiPatternGenerated?.Invoke(pattern);
        }

        private bool TryApplyMusicalTiming(MusicalTimingConfig newConfig, string reason)
        {
            if (!newConfig.IsValid(out string validationError))
            {
                Debug.LogWarning(
                    $"[TurnLoopController] Ignored musical timing update because it is invalid: {validationError}");
                return false;
            }

            if (HasSameTimingValues(currentMusicalTiming, newConfig))
            {
                EmitDebugMessage($"Musical timing update ignored because values are unchanged. {newConfig}");
                return true;
            }

            currentMusicalTiming = newConfig;
            long durationSamples = currentMusicalTiming.GetTurnDurationSamples();
            EmitDebugMessage($"{reason} {currentMusicalTiming}. turnDurationSamples={durationSamples}.");
            return true;
        }

        private static bool HasSameTimingValues(MusicalTimingConfig a, MusicalTimingConfig b)
        {
            return
                Mathf.Approximately(a.bpm, b.bpm) &&
                a.beatsPerBar == b.beatsPerBar &&
                a.barsPerTurn == b.barsPerTurn &&
                a.stepsPerQuarter == b.stepsPerQuarter &&
                a.sampleRate == b.sampleRate;
        }

        private string DescribeTimingState()
        {
            if (!currentMusicalTiming.IsValid(out _))
            {
                return "missing";
            }

            return $"{currentMusicalTiming}, turnDurationSamples={currentMusicalTiming.GetTurnDurationSamples()}";
        }

        private static string FormatResponsePlanSummary(ResponsePlan plan)
        {
            if (plan == null)
            {
                return "Response plan: none.";
            }

            return
                "Response plan: " +
                $"ResponseType={plan.Type}, " +
                $"TargetDensity={FormatPlanValue(plan.TargetDensity)}, " +
                $"TargetEnergy={FormatPlanValue(plan.TargetEnergy)}, " +
                $"PreserveAnchors={plan.PreserveAnchors}, " +
                $"MirrorEnding={plan.MirrorEnding}, " +
                $"VariationAmount={FormatPlanValue(plan.VariationAmount)}, " +
                $"SyncopationBias={FormatPlanValue(plan.SyncopationBias)}, " +
                $"ComplementarityBias={FormatPlanValue(plan.ComplementarityBias)}, " +
                $"TurnLengthSteps={plan.TurnLengthSteps}.";
        }

        private static string FormatPlanValue(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private AiResponsePreparationFlow CreateAiResponsePreparationFlow()
        {
            return new AiResponsePreparationFlow(turnAnalyser, responsePlanner, featureTransformer);
        }

        private void MoveToError(string reason)
        {
            if (turnCaptureController != null && turnCaptureController.IsCapturing)
            {
                turnCaptureController.CancelCapture();
            }

            isRunning = false;
            ClearCaptureRuntimeState();
            ClearAnalysisAndPlanningRuntimeState();
            ClearGeneratedAiResponseState();
            Debug.LogError($"[TurnLoopController] {reason}");
            SetPhase(TurnPhase.Error);
        }
    }
}
