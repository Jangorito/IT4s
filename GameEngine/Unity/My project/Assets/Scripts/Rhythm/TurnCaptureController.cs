using UnityEngine;
using IT4s.Data;
using IT4s.Input;

namespace IT4s.Rhythm
{
    /// <summary>
    /// Capture service for opening/closing explicit turn windows and slicing the shared HitBuffer.
    /// Orchestration decides when a turn starts or ends; this controller only materialises that window.
    /// </summary>
    public class TurnCaptureController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private OscHitReceiver hitReceiver;

        [Header("Logging")]
        [SerializeField] private bool logHitsInTurn = false;
        [SerializeField] private int maxHitLogs = 30;

        private TurnManager _turnManager;
        public bool IsCapturing => _turnManager != null && _turnManager.IsActive;
        public OscHitReceiver HitReceiver => hitReceiver;

        private void Awake()
        {
            _turnManager = new TurnManager();

            if (hitReceiver == null)
            {
                Debug.LogWarning("[TurnCaptureController] No OscHitReceiver assigned yet. Capture will stay unavailable until one is provided.");
            }
        }

        public void SetHitReceiver(OscHitReceiver injectedHitReceiver)
        {
            hitReceiver = injectedHitReceiver;
        }

        public bool TryBeginCapture(long startSamples)
        {
            if (_turnManager == null)
            {
                _turnManager = new TurnManager();
            }

            if (hitReceiver == null || hitReceiver.Buffer == null)
            {
                Debug.LogWarning("[TurnCaptureController] Cannot begin capture because the hit receiver is missing.");
                return false;
            }

            if (_turnManager.IsActive)
            {
                Debug.LogWarning("[TurnCaptureController] Capture is already active.");
                return false;
            }

            if (startSamples < 0)
            {
                Debug.LogWarning($"[TurnCaptureController] Cannot begin capture with invalid startSamples={startSamples}.");
                return false;
            }

            _turnManager.StartTurn(startSamples);
            Debug.Log($"[TurnCaptureController] Begin capture for turn {_turnManager.CurrentTurnId} at {startSamples} samples");
            return true;
        }

        public bool TryEndCapture(long endSamples, out TurnWindow turnWindow)
        {
            turnWindow = default;

            if (_turnManager == null)
            {
                Debug.LogWarning("[TurnCaptureController] Cannot end capture because the TurnManager is missing.");
                return false;
            }

            if (!_turnManager.IsActive)
            {
                Debug.LogWarning("[TurnCaptureController] Cannot end capture because no capture is active.");
                return false;
            }

            if (hitReceiver == null || hitReceiver.Buffer == null)
            {
                Debug.LogWarning("[TurnCaptureController] Cannot end capture because the hit receiver is missing.");
                return false;
            }

            if (endSamples < _turnManager.ActiveStartSamples)
            {
                Debug.LogWarning(
                    $"[TurnCaptureController] Cannot end capture before it starts. " +
                    $"start={_turnManager.ActiveStartSamples}, end={endSamples}.");
                return false;
            }

            turnWindow = _turnManager.EndTurn(endSamples);
            var hits = hitReceiver.Buffer.Slice(turnWindow.startSamples, turnWindow.endSamples);

            Debug.Log(
                $"[TurnCaptureController] End capture for turn {turnWindow.turnId} at {endSamples} samples " +
                $"with {hits.Count} hits in window.");

            if (logHitsInTurn)
            {
                int n = Mathf.Min(hits.Count, maxHitLogs);
                for (int i = 0; i < n; i++)
                {
                    Debug.Log($"  {hits[i]}");
                }

                if (hits.Count > n)
                {
                    Debug.Log($"  ... +{hits.Count - n} more");
                }
            }

            return true;
        }

        public void CancelCapture()
        {
            if (_turnManager == null || !_turnManager.IsActive)
            {
                return;
            }

            Debug.Log($"[TurnCaptureController] Cancelling capture for turn {_turnManager.CurrentTurnId}.");
            _turnManager.CancelTurn();
        }
    }
}
