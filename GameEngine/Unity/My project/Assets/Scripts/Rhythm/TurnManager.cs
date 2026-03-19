using UnityEngine;

namespace IT4s.Rhythm
{
    /// <summary>
    /// Unity-owned turn boundaries.
    /// "turn" = a window in Bela sample time.
    /// </summary>
    public class TurnManager
    {
        private int _nextTurnId = 0;

        private bool _isActive;
        private long _startSamples;

        public bool IsActive => _isActive;
        public int CurrentTurnId => _nextTurnId;
        public long ActiveStartSamples => _startSamples;

        /// <summary>
        /// Starts a new turn at the given sample time.
        /// </summary>
        public void StartTurn(long startSamples)
        {
            Debug.Log($"[TurnManager] Starting turn {_nextTurnId} at {startSamples} samples");

            _isActive = true;
            _startSamples = startSamples;
        }

        /// <summary>
        /// Ends the active turn and returns the resulting timing metadata.
        /// </summary>
        public void EndTurn(long endSamples, out int turnId, out long startSamples)
        {
            Debug.Log($"[TurnManager] Ending turn {_nextTurnId} at {endSamples} samples");

            turnId = _nextTurnId;
            startSamples = _startSamples;

            _isActive = false;
            _startSamples = 0;
            _nextTurnId++;
        }

        /// <summary>
        /// Cancels the current turn without producing a TurnWindow.
        /// </summary>
        public void CancelTurn()
        {
            if (!_isActive)
            {
                return;
            }

            Debug.Log($"[TurnManager] Cancelling turn {_nextTurnId} that started at {_startSamples} samples");

            _isActive = false;
            _startSamples = 0;
        }
    }
}
