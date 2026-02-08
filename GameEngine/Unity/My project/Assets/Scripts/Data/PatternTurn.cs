using System;

namespace IT4s.Data
{
    /// <summary>
    /// Quantised representation of a captured rhythm (TODO: updated later by PatternCompiler).
    /// </summary>
    [Serializable]
    public class PatternTurn
    {
        public int turnId;

        // Timing basis
        public float bpm;
        public int stepsPerQuarter;     // e.g., 12 for flexible grid

        // Window grounding (so patterns remain traceable to raw events)
        public long startSamples;
        public long endSamples;

        // Grid data
        public int[] velocity;          // 0–127 per step
        public int[] offsetSamples;     // micro-timing offsets per step (optional; can be null)

        public int StepCount => velocity?.Length ?? 0;
    }
}
