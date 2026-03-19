using System.Collections.Generic;
using UnityEngine;
using IT4s.Data;
using IT4s.Input;
using IT4s.Rhythm.Transformations;

namespace IT4s.Rhythm
{
    /// <summary>
    /// Scene-level controller for starting/ending capture windows and slicing HitBuffer.
    /// Uses the most recent received hit timestamp as "now" in Bela time.
    /// </summary>
    public class TurnCaptureController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private OscHitReceiver hitReceiver;
        [SerializeField] private IT4ChuckTurnPlayer turnPlayer;

        [Header("Controls")]
        [SerializeField] private KeyCode startKey = KeyCode.S;
        [SerializeField] private KeyCode endKey = KeyCode.E;

        [Header("Logging")]
        [SerializeField] private bool logHitsInTurn = false;
        [SerializeField] private int maxHitLogs = 30;

        [Header("Quantisation")]
        [SerializeField] private float fixedBpm = 92f;
        [SerializeField] private int stepsPerQuarter = 12;
        [SerializeField] private int sampleRate = 48000;

        private TurnManager _turnManager;
        private PatternCompiler _compiler;
        // FeatureTransformer conceptually belongs in the higher-level orchestration layer.
        // For now, while TurnLoopController is not yet composing the full graph, this controller
        // creates a concrete instance so transformation is still an explicit collaborator rather
        // than a static call hidden inside capture logic.
        private FeatureTransformer _featureTransformer;


        // "now" = latest Bela sample time observed.
        private long _latestSamples;
        private bool _hasClock;

        private void Awake()
        {
            _turnManager = new TurnManager();
            _compiler = new PatternCompiler();
            // Practical interim composition: keep the dependency explicit today without adding
            // a DI framework. A future composition root or TurnLoopController can inject this.
            _featureTransformer = new FeatureTransformer();


            if (hitReceiver == null)
            {
                Debug.LogError("[TurnCaptureController] No OscHitReceiver assigned.");
                enabled = false;
            }

            if (turnPlayer == null)
            {
                // turnPlayer = new IT4ChuckTurnPlayer();
                turnPlayer = FindObjectOfType<IT4ChuckTurnPlayer>();
            }
        }

        private void Update()
        {
            // Pull clock forward from the receiver (best effort).
            if (TryGetLatestSampleTime(out var t))
            {
                _latestSamples = t;
                _hasClock = true;
            }

            if (UnityEngine.Input.GetKeyDown(startKey))
                TryStartTurn();

            if (UnityEngine.Input.GetKeyDown(endKey))
                TryEndTurn();
        }

        private bool TryGetLatestSampleTime(out long tSamples)
        {
            if (hitReceiver != null && hitReceiver.HasLastSamples)
            {
                tSamples = hitReceiver.LastSamples;
                return true;
            }

            tSamples = 0;
            return false;
        }

        private void TryStartTurn()
        {
            if (_turnManager.IsActive)
            {
                Debug.LogWarning("[TurnCaptureController] Turn already active.");
                return;
            }

            if (!_hasClock)
            {
                Debug.LogWarning("[TurnCaptureController] No Bela clock yet. Trigger some hits first.");
                return;
            }

            _turnManager.StartTurn(_latestSamples);
            Debug.Log($"[TurnCaptureController] START turn {_turnManager.CurrentTurnId} at {_latestSamples} samples");
        }

        private void TryEndTurn()
        {
            if (!_turnManager.IsActive)
            {
                Debug.LogWarning("[TurnCaptureController] No active turn to end.");
                return;
            }

            if (!_hasClock)
            {
                Debug.LogWarning("[TurnCaptureController] No Bela clock available.");
                return;
            }

            TurnWindow window = _turnManager.EndTurn(_latestSamples);

            // Slice hits for this window
            List<HitEvent> hits = hitReceiver.Buffer.Slice(window.startSamples, window.endSamples);

            var q = new QuantisationSettings(fixedBpm, stepsPerQuarter, sampleRate);
            PatternTurn pattern = _compiler.Compile(window, hits, q);

            Debug.Log(
                $"[PatternCompiler] turn={pattern.turnId} " +
                $"steps={pattern.StepCount} bpm={pattern.bpm}"
            );

            if (logHitsInTurn)
            {
                int n = Mathf.Min(hits.Count, maxHitLogs);
                for (int i = 0; i < n; i++)
                    Debug.Log($"  {hits[i]}");
                if (hits.Count > n)
                    Debug.Log($"  ... +{hits.Count - n} more");
            }

            // Log derived rhythm representation
            for (int i = 0; i < pattern.StepCount; i++)
            {
                if (pattern.velocity[i] > 0)
                    Debug.Log($"  step {i}: vel={pattern.velocity[i]} off={pattern.offsetSamples[i]}");
            }
            

            Debug.Log($"[TurnCaptureController] END turn {window.turnId}: {hits.Count} hits | pattern: {string.Join(", ", pattern.velocity)}");

            if (turnPlayer == null)
            {
                Debug.LogWarning("[TurnCaptureController] No IT4ChuckTurnPlayer assigned/found. Skipping playback.");
                return;
            }


            Debug.Log($"[TurnCaptureController] Sending turn {pattern.turnId} to turnPlayer.");
            // turnPlayer.Play(pattern);
            // SimpleTransformer.Transform(pattern, SimpleTransformer.Mode.Triplah);
            // var ai = SimpleTransformer.Transform(pattern, SimpleTransformer.Mode.Triplets);
            // Temporary placement: transformation is still happening here until orchestration
            // moves fully into TurnLoopController, but it now does so through a real collaborator.
            var ai = _featureTransformer.Transform(pattern, FeatureTransformer.Mode.Auto);

            turnPlayer.Play(ai);

        }
    }
}
