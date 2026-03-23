using System;

namespace IT4s.Rhythm
{
    /// <summary>
    /// Central musical timing model for the turn-taking system.
    /// Both quantisation settings and phrase duration derive from this one configuration
    /// so turn-taking remains musically coherent as tempo or phrase structure changes.
    /// </summary>
    [Serializable]
    public struct MusicalTimingConfig
    {
        public float bpm;
        public int beatsPerBar;
        public int barsPerTurn;
        public int stepsPerQuarter;
        public int sampleRate;

        public MusicalTimingConfig(
            float bpm,
            int beatsPerBar,
            int barsPerTurn,
            int stepsPerQuarter,
            int sampleRate)
        {
            this.bpm = bpm;
            this.beatsPerBar = beatsPerBar;
            this.barsPerTurn = barsPerTurn;
            this.stepsPerQuarter = stepsPerQuarter;
            this.sampleRate = sampleRate;
        }

        public bool IsValid(out string reason)
        {
            if (bpm <= 0f)
            {
                reason = $"bpm={bpm} must be greater than zero.";
                return false;
            }

            if (beatsPerBar <= 0)
            {
                reason = $"beatsPerBar={beatsPerBar} must be greater than zero.";
                return false;
            }

            if (barsPerTurn <= 0)
            {
                reason = $"barsPerTurn={barsPerTurn} must be greater than zero.";
                return false;
            }

            if (stepsPerQuarter <= 0)
            {
                reason = $"stepsPerQuarter={stepsPerQuarter} must be greater than zero.";
                return false;
            }

            if (sampleRate <= 0)
            {
                reason = $"sampleRate={sampleRate} must be greater than zero.";
                return false;
            }

            reason = null;
            return true;
        }

        public long GetTurnDurationSamples()
        {
            // Phrase duration comes from bars, beats, tempo, and sample rate.
            // stepsPerQuarter controls quantisation resolution, not phrase length.
            double beatsPerTurn = (double)beatsPerBar * barsPerTurn;
            double secondsPerBeat = 60.0 / bpm;
            double durationSeconds = beatsPerTurn * secondsPerBeat;
            long durationSamples = (long)Math.Round(durationSeconds * sampleRate);
            return Math.Max(1L, durationSamples);
        }

        public QuantisationSettings ToQuantisationSettings()
        {
            return new QuantisationSettings(bpm, stepsPerQuarter, sampleRate);
        }

        public override string ToString()
        {
            return
                $"bpm={bpm}, beatsPerBar={beatsPerBar}, barsPerTurn={barsPerTurn}, " +
                $"stepsPerQuarter={stepsPerQuarter}, sampleRate={sampleRate}";
        }
    }
}
