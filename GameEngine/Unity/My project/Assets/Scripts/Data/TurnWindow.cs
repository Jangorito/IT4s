using System;

namespace IT4s.Data
{
    /// <summary>
    /// Unity capture window for a player's phase
    /// </summary>
    [Serializable]
    public struct TurnWindow
    {
        public int turnId;
        public long startSamples;
        public long endSamples;

        public long DurationSamples => endSamples - startSamples;

        public TurnWindow(int turnId, long startSamples, long endSamples)
        {
            this.turnId = turnId;
            this.startSamples = startSamples;
            this.endSamples = endSamples;
        }

        public override string ToString()
            => $"TurnWindow(id={turnId}, start={startSamples}, end={endSamples}, dur={DurationSamples})";
    }
}
