using System;

namespace IT4s.Data
{
    /// <summary>
    /// A single drum hit detected on Bela, timestamped in Bela sample time.
    /// </summary>
    [Serializable]
    public struct HitEvent
    {
        public long tSamples;   // Bela clock in samples
        public int pad;         // pad id
        public int velocity;    // Hit velocity

        public HitEvent(long tSamples, int pad, int velocity)
        {
            this.tSamples = tSamples;
            this.pad = pad;
            this.velocity = velocity;
        }

        public override string ToString()
            => $"Hit(tSamples={tSamples}, pad={pad}, vel={velocity})";
    }
}
