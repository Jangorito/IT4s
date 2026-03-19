using System;
using IT4s.Data;
using IT4s.Input;
using IT4s.Rhythm;
using IT4s.Rhythm.Transformations;
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
    }

    /// <summary>
    /// Lightweight orchestration shell for the turn-taking system.
    /// This controller owns the state machine and dependency wiring, but deliberately does
    /// not implement capture, compilation, generation, or playback internals itself.
    /// That separation keeps each subsystem replaceable and makes the loop easier to
    /// reason about during design review, debugging, and later extension.
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

        [Header("Human Turn Timing")]
        [SerializeField]
        [Tooltip("Fixed capture duration for the human turn in Bela samples.")]
        private int fixedHumanTurnDurationSamples = 192000;

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

        // Only the controller may change phase. External systems can observe CurrentPhase,
        // but all transitions are funnelled through SetPhase(...) for consistency and logging.
        public TurnPhase CurrentPhase { get; private set; } = TurnPhase.Transition;

        // Running state is kept separate from CurrentPhase so the loop can be paused or stopped
        // without needing extra domain phases before they are truly justified.
        private bool isRunning;

        private HitEvent lastTriggerHit;
        private bool hasLastTriggerHit;
        private long startSamples = -1;
        private long endSamples = -1;
        private TurnWindow lastTurnWindow;
        private bool hasLastTurnWindow;
        private bool compilingPlaceholderLogged;
        private bool captureClockWarningIssued;
        private OscHitReceiver subscribedHitReceiver;

        // Events provide observability without forcing UI or debug tools to poll the controller.
        // They also make the orchestration layer easier to test because state changes are explicit.
        public event Action<TurnPhase> OnPhaseChanged;
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
            OscHitReceiver injectedHitReceiver = null)
        {
            turnCaptureController = injectedTurnCaptureController;
            hitBuffer = injectedHitBuffer;
            patternCompiler = injectedPatternCompiler;
            aiTurnPlayer = injectedTurnPlayer;
            featureTransformer = injectedFeatureTransformer;
            hitReceiver = injectedHitReceiver != null ? injectedHitReceiver : hitReceiver;

            ResolveReceiverReference();
            RefreshHitSubscription();

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
            ClearActiveTurnState();
            EmitDebugMessage($"Turn loop started. {DescribeDependencyState()}");
            SetPhase(TurnPhase.WaitingForHuman);
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
            ClearActiveTurnState();
            SetPhase(TurnPhase.Transition);
            EmitDebugMessage("Turn loop stopped.");
        }

        /// <summary>
        /// Central state-machine tick.
        /// The switch is intentionally skeletal for now: each case describes the responsibility
        /// that will be added later, but no timing, AI, compilation, or playback work happens yet.
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
                    // Waiting is event-driven in Chunk 2. Incoming hits are handled by OnHitReceived.
                    break;

                case TurnPhase.CapturingHuman:
                    TickCapturingHuman();
                    break;

                case TurnPhase.CompilingHumanTurn:
                    TickCompilingHumanTurn();
                    break;

                case TurnPhase.GeneratingAiResponse:
                    // Future implementation:
                    // Use FeatureTransformer or a later AI module to derive the machine response.
                    // The phase exists now so the orchestration contract is stable before logic arrives.
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

            if (CurrentPhase != TurnPhase.CompilingHumanTurn)
            {
                compilingPlaceholderLogged = false;
            }

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
                $"featureTransformer={(featureTransformer != null ? "set" : "missing")}.";
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

            if (fixedHumanTurnDurationSamples <= 0)
            {
                MoveToError(
                    $"Turn loop cannot start because fixedHumanTurnDurationSamples={fixedHumanTurnDurationSamples} is invalid.");
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

            if (triggerHit.tSamples > long.MaxValue - fixedHumanTurnDurationSamples)
            {
                MoveToError(
                    $"Cannot schedule human turn end because start={triggerHit.tSamples} would overflow with duration={fixedHumanTurnDurationSamples}.");
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
            endSamples = startSamples + fixedHumanTurnDurationSamples;
            captureClockWarningIssued = false;

            EmitDebugMessage($"Human turn started at sample {startSamples}.");
            EmitDebugMessage($"Human turn scheduled to end at sample {endSamples}.");
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

            lastTurnWindow = turnWindow;
            hasLastTurnWindow = true;

            int hitCount = CountHitsInWindow(turnWindow);
            EmitDebugMessage(
                $"Human turn ended at sample {turnWindow.endSamples}. Window={turnWindow.turnId}, hits={hitCount}.");

            SetPhase(TurnPhase.CompilingHumanTurn);
        }

        private void TickCompilingHumanTurn()
        {
            if (compilingPlaceholderLogged)
            {
                ClearActiveTurnState();
                SetPhase(TurnPhase.WaitingForHuman);
                return;
            }

            if (!hasLastTurnWindow)
            {
                Debug.LogWarning("[TurnLoopController] CompilingHumanTurn placeholder reached without a stored TurnWindow.");
            }
            else
            {
                EmitDebugMessage(
                    $"CompilingHumanTurn placeholder reached for turn {lastTurnWindow.turnId}. " +
                    "Compilation is intentionally deferred to a later chunk.");
            }

            compilingPlaceholderLogged = true;
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

        private int CountHitsInWindow(TurnWindow turnWindow)
        {
            ResolveReceiverReference();

            if (hitReceiver == null || hitReceiver.Buffer == null)
            {
                return 0;
            }

            return hitReceiver.Buffer.Slice(turnWindow.startSamples, turnWindow.endSamples).Count;
        }

        private void ClearActiveTurnState()
        {
            hasLastTriggerHit = false;
            startSamples = -1;
            endSamples = -1;
            compilingPlaceholderLogged = false;
            captureClockWarningIssued = false;
        }

        private void MoveToError(string reason)
        {
            if (turnCaptureController != null && turnCaptureController.IsCapturing)
            {
                turnCaptureController.CancelCapture();
            }

            isRunning = false;
            ClearActiveTurnState();
            Debug.LogError($"[TurnLoopController] {reason}");
            SetPhase(TurnPhase.Error);
        }
    }
}
