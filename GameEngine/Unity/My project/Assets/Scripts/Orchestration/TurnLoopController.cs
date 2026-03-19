using System;
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
        [Tooltip("Component responsible for managing human capture windows in later chunks.")]
        private TurnCaptureController turnCaptureController;

        [SerializeField]
        [Tooltip("Playback endpoint that will render AI responses once orchestration reaches that phase.")]
        private IT4ChuckTurnPlayer aiTurnPlayer;

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

        // Temporary debug gate for hit acceptance. This exists now so a developer can simulate
        // enabling/disabling human input before the full capture policy is implemented.
        private bool hitsEnabled = true;

        // Events provide observability without forcing UI or debug tools to poll the controller.
        // They also make the orchestration layer easier to test because state changes are explicit.
        public event Action<TurnPhase> OnPhaseChanged;
        public event Action<string> OnDebugMessage;

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
            FeatureTransformer injectedFeatureTransformer)
        {
            turnCaptureController = injectedTurnCaptureController;
            hitBuffer = injectedHitBuffer;
            patternCompiler = injectedPatternCompiler;
            aiTurnPlayer = injectedTurnPlayer;
            featureTransformer = injectedFeatureTransformer;

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

            isRunning = true;
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

            isRunning = false;
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
                    // Future implementation:
                    // Listen for the conditions that should begin a human turn, such as a valid hit,
                    // a transport cue, or another start signal defined by the performance design.
                    break;

                case TurnPhase.CapturingHuman:
                    // Future implementation:
                    // Coordinate with TurnCaptureController and HitBuffer while the human turn is open.
                    // This is where capture windows, input gating, and turn completion rules will live.
                    break;

                case TurnPhase.CompilingHumanTurn:
                    // Future implementation:
                    // Ask PatternCompiler to turn the captured material into a structured rhythmic model
                    // that downstream systems can transform, analyse, and eventually play back.
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
            hitsEnabled = enabled;
            EmitDebugMessage(
                $"Debug hit validity set to {(hitsEnabled ? "enabled" : "disabled")}. " +
                "Later capture logic will use this flag to accept or ignore hits.");
        }

        /// <summary>
        /// Single transition gateway for the controller.
        /// Centralising phase changes keeps observability consistent across runtime, UI, and tests.
        /// </summary>
        private void SetPhase(TurnPhase newPhase)
        {
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
                $"hitBuffer={(hitBuffer != null ? "set" : "missing")}, " +
                $"compiler={(patternCompiler != null ? "set" : "missing")}, " +
                $"turnPlayer={(aiTurnPlayer != null ? "set" : "missing")}, " +
                $"featureTransformer={(featureTransformer != null ? "set" : "missing")}.";
        }
    }
}
