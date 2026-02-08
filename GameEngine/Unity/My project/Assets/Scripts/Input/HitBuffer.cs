using System.Collections.Generic;
using IT4s.Data;

namespace IT4s.Input
{
    /// <summary>
    /// Append-only storage of incoming HitEvents from Bela.
    /// </summary>
    public class HitBuffer
    {
        private readonly List<HitEvent> _hits = new();

        public int Count => _hits.Count;

        public void Add(HitEvent e) => _hits.Add(e);

        /// <summary>
        /// Returns a new list containing hits with tSamples in [startSamples, endSamples].
        /// </summary>
        public List<HitEvent> Slice(long startSamples, long endSamples)
        {
            var result = new List<HitEvent>();

            // linear scan, TODO: later binary search?
            for (int i = 0; i < _hits.Count; i++)
            {
                var h = _hits[i];
                if (h.tSamples >= startSamples && h.tSamples < endSamples)
                    result.Add(h);
            }

            return result;
        }

        /// <summary>
        /// Clears stored hits.
        /// </summary>
        public void Clear() => _hits.Clear();
    }
}
