using IT4s.Input;
using IT4s.Orchestration;
using IT4s.Rhythm;
using IT4s.Rhythm.Transformations;
using UnityEngine;

namespace IT4s.Bootstrap
{
    /// <summary>
    /// Scene composition root for the Intelli-Trading 4s turn-taking loop.
    /// This component wires the existing collaborators together, injects them into the
    /// TurnLoopController, and starts the loop. It deliberately contains no loop logic.
    /// </summary>
    public class TurnLoopBootstrap : MonoBehaviour
    {
        [Header("Scene Components")]
        [SerializeField] private TurnLoopController turnLoopController;
        [SerializeField] private TurnCaptureController turnCaptureController;
        [SerializeField] private OscHitReceiver oscHitReceiver;
        [SerializeField] private IT4ChuckTurnPlayer aiTurnPlayer;

        private PatternCompiler patternCompiler;
        private FeatureTransformer featureTransformer;

        private void Awake()
        {
            if (!ValidateSceneReferences())
            {
                enabled = false;
                return;
            }

            patternCompiler = new PatternCompiler();
            featureTransformer = new FeatureTransformer();
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            if (oscHitReceiver.Buffer == null)
            {
                Debug.LogError("[TurnLoopBootstrap] OscHitReceiver buffer is unavailable.");
                enabled = false;
                return;
            }

            turnLoopController.InjectDependencies(
                turnCaptureController,
                oscHitReceiver.Buffer,
                patternCompiler,
                aiTurnPlayer,
                featureTransformer,
                oscHitReceiver);

            Debug.Log("[TurnLoopBootstrap] Turn loop collaborators wired.");
            turnLoopController.StartLoop();
            Debug.Log("[TurnLoopBootstrap] Turn loop started.");
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
    }
}
