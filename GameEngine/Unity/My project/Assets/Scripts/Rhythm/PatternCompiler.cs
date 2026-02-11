using System;
using System.Collections.Generic;
using IT4s.Data;

namespace IT4s.Rhythm
{
    /// <summary>
    /// Compiles a TurnWindow + raw HitEvents into a fixed-BPM quantised pattern.
    /// </summary>
    public class PatternCompiler
    {
        public PatternTurn Compile(
            TurnWindow window,
            List<HitEvent> hits,
            QuantisationSettings q)
        {
            // seconds per quarter note
            double secPerQuarter = 60.0 / q.bpm;

            // seconds per step
            double secPerStep = secPerQuarter / q.stepsPerQuarter;

            // samples per step
            double samplesPerStep = secPerStep * q.sampleRate;

            // total steps covering the window (ceil so we don't truncate)
            long windowSamples = window.endSamples - window.startSamples;
            int stepCount = (int)Math.Ceiling(windowSamples / samplesPerStep);
            if (stepCount < 1) stepCount = 1;

            var velocity = new int[stepCount];
            var offsetSamples = new int[stepCount];

            // initialise offsets to 0
            for (int i = 0; i < stepCount; i++)
                offsetSamples[i] = 0;

            foreach (var h in hits)
            {
                // time relative to window start (in samples)
                long relSamples = h.tSamples - window.startSamples;
                if (relSamples < 0) continue;

                // step index
                int step = (int)Math.Round(relSamples / samplesPerStep);
                if (step < 0 || step >= stepCount) continue;

                // center of this step (in samples)
                long stepCenter = (long)Math.Round(step * samplesPerStep);

                // microtiming offset (samples from step center)
                int off = (int)(relSamples - stepCenter);

                // collision rule: max velocity wins
                if (h.velocity > velocity[step])
                {
                    velocity[step] = h.velocity;
                    offsetSamples[step] = off;
                }
            }

            return new PatternTurn
            {
                turnId = window.turnId,
                bpm = q.bpm,
                stepsPerQuarter = q.stepsPerQuarter,
                startSamples = window.startSamples,
                endSamples = window.endSamples,
                velocity = velocity,
                offsetSamples = offsetSamples
            };
        }
    }
}
