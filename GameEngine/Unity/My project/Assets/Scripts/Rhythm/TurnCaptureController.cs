using System.Collections.Generic;
using UnityEngine;
using IT4s.Data;
using IT4s.Input;

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

        [Header("Controls")]
        [SerializeField] private KeyCode startKey = KeyCode.S;
        [SerializeField] private KeyCode endKey = KeyCode.E;

        [Header("Logging")]
        [SerializeField] private bool logHitsInTurn = false;
        [SerializeField] private int maxHitLogs = 30;

        private TurnManager _turnManager;

        // "now" = latest Bela sample time observed.
        private long _latestSamples;
        private bool _hasClock;

        private void Awake()
        {
            _turnManager = new TurnManager();

            if (hitReceiver == null)
            {
                Debug.LogError("[TurnCaptureController] No OscHitReceiver assigned.");
                enabled = false;
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

            Debug.Log($"[TurnCaptureController] END turn {window.turnId}: {hits.Count} hits | {window}");

            if (logHitsInTurn)
            {
                int n = Mathf.Min(hits.Count, maxHitLogs);
                for (int i = 0; i < n; i++)
                    Debug.Log($"  {hits[i]}");
                if (hits.Count > n)
                    Debug.Log($"  ... +{hits.Count - n} more");
            }
        }
    }
}
