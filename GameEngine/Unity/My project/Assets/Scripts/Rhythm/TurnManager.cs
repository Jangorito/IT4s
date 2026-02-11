using IT4s.Data;
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
        /// Ends the active turn and returns the resulting TurnWindow.
        /// </summary>
        public TurnWindow EndTurn(long endSamples)
        {
            Debug.Log($"[TurnManager] Ending turn {_nextTurnId} at {endSamples} samples");

            var window = new TurnWindow(_nextTurnId, _startSamples, endSamples);

            _isActive = false;
            _nextTurnId++;

            return window;
        }
    }
}
