using System;
using System.Collections.Generic;

namespace IT4s.Data
{
    /// <summary>
    /// Self-contained raw captured turn in Bela sample time.
    /// </summary>
    [Serializable]
    public struct TurnWindow
    {
        public int turnId;
        public long startSamples;
        public long endSamples;
        private HitEvent[] hits;

        public long DurationSamples => endSamples - startSamples;
        public IReadOnlyList<HitEvent> Hits => hits ?? Array.Empty<HitEvent>();
        public int HitCount => hits?.Length ?? 0;

        public TurnWindow(int turnId, long startSamples, long endSamples, IReadOnlyList<HitEvent> hits)
        {
            this.turnId = turnId;
            this.startSamples = startSamples;
            this.endSamples = endSamples;

            if (hits == null || hits.Count == 0)
            {
                this.hits = Array.Empty<HitEvent>();
                return;
            }

            this.hits = new HitEvent[hits.Count];
            for (int i = 0; i < hits.Count; i++)
            {
                this.hits[i] = hits[i];
            }
        }

        public override string ToString()
            => $"TurnWindow(id={turnId}, start={startSamples}, end={endSamples}, dur={DurationSamples}, hits={HitCount})";
    }
}
