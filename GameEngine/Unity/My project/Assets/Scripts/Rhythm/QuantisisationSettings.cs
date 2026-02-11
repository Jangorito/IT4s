namespace IT4s.Rhythm
{
    public struct QuantisationSettings
    {
        public float bpm;              // fixed BPM
        public int stepsPerQuarter;    // e.g. 12 (flexible grid)
        public int sampleRate;         // e.g. 48000

        public QuantisationSettings(float bpm, int stepsPerQuarter, int sampleRate)
        {
            this.bpm = bpm;
            this.stepsPerQuarter = stepsPerQuarter;
            this.sampleRate = sampleRate;
        }
    }
}
