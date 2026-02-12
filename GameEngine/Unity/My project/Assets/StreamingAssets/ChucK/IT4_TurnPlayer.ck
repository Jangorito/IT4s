// IT4_TurnPlayer.ck
// RunFile-style turn playback: Unity sets arrays + broadcasts playTurn.
// Plays a single snare sample for any step with velocity > 0.

global int ckReady;
global Event playTurn;

// Data pushed from Unity:
global int stepCount;
global int samplesPerStep;
global int velocity[0];       // length = stepCount
global int offsetSamples[0];  // length = stepCount (microtiming offset from grid point)

// --- audio ---
SndBuf sn => Gain g => dac;
1.0 => g.gain;

me.dir() + "snare.wav" => sn.read;
0 => sn.gain; // keep silent until triggered

1 => ckReady;

// helper: clamp
fun int clampInt(int x, int lo, int hi)
{
    if(x < lo) return lo;
    if(x > hi) return hi;
    return x;
}

// play a snare hit (velocity 0..127)
fun void hit(int v)
{
    (clampInt(v, 0, 127) / 127.0) => float amp;

    0 => sn.pos;
    amp => sn.gain;

    // tiny wait to avoid edge cases on rapid retrigger
    5::ms => now;

    // optional: fade down a bit (keeps tail more controlled)
    // (amp * 0.9) => sn.gain;
}

// Build an event list (time in samples, vel) from step arrays,
// sort by time (offset can reorder), then schedule sample-accurately.
fun void playTurnNow()
{
    <<< "[.ck] Attempting to play turn with stepCount:", stepCount, "samplesPerStep:", samplesPerStep >>>;
    <<< "[.ck] velocity array size:", velocity.size(), "offsetSamples array size:", offsetSamples.size() >>>;

    if(stepCount <= 0 || samplesPerStep <= 0) {
        return;
    }
    // maybe here we initialise size of velocity/offset arrays if not already done? their size should match stepCount, but we can't be sure until Unity pushes data. For now we just check sizes when building event list and skip if mismatch.
    // might entail stopping function until a broadcasted flag confirms data is ready.

    int times[0];
    int vels[0];

    // collect events
    for(0 => int i; i < stepCount; i++)
    {
        if(i >= velocity.size() || i >= offsetSamples.size()) break;

        if(velocity[i] > 0)
        {
            (i * samplesPerStep + offsetSamples[i]) => int t;
            if(t < 0) 0 => t; // clamp negative microtiming to start
            times << t;
            vels  << velocity[i];
        }
    }

    // nothing to play
    if(times.size() == 0) 
    {
        <<< "[.ck] No hits to play for this turn." >>>;
        return;
    }

    // insertion sort by time (simple + fine for small patterns)
    for(1 => int i; i < times.size(); i++)
    {
        times[i] => int keyT;
        vels[i]  => int keyV;
        i - 1 => int j;

        while(j >= 0 && times[j] > keyT)
        {
            times[j] => times[j + 1];
            vels[j]  => vels[j + 1];
            j--;
        }
        keyT => times[j + 1];
        keyV => vels[j + 1];
    }

    // schedule from "now"
    0 => int lastT;
    for(0 => int k; k < times.size(); k++)
    {
        (times[k] - lastT) => int dt;
        if(dt > 0) dt::samp => now;
        times[k] => lastT;

        spork ~ hit(vels[k]);
    }
}

while(true)
{
    playTurn => now;
    spork ~ playTurnNow();
}
