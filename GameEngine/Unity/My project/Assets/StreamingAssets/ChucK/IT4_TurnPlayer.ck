// IT4_TurnPlayer.ck
// RunFile-style turn playback: Unity sets arrays + broadcasts playTurn.
// Playback timing and metronome timing are both owned by ChucK.

global int ckReady;
global Event playTurn;

// Data pushed from Unity:
global int stepCount;
global int samplesPerStep;
global int stepsPerQuarter;
global int metronomeEnabled;
global int velocity[0];       // length = stepCount
global int offsetSamples[0];  // length = stepCount (microtiming offset from grid point)

// --- turn audio ---
SndBuf snare => Gain g => dac;
0.0 => g.gain;
me.dir() + "snare.wav" => snare.read;
0 => snare.gain; // keep silent until triggered

// --- metronome audio ---
SndBuf metronome => Gain mGain => dac;
me.dir() + "metronome.wav" => metronome.read;
0 => mGain.gain;


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
    0 => snare.pos;
    amp => snare.gain;

    // tiny wait to avoid edge cases on rapid retrigger
    5::ms => now;
}

fun void metroClick(int accented)
{
    <<< "Metronome click (accented:" + accented + ")" >>>;
    if(accented != 0)
    {
        // 2300 => clkRes.freq;
        25 => mGain.gain;
    }
    else
    {
        // 1600 => clkRes.freq;
        10 => mGain.gain;         
    }

    1 => clkImp.next;
    // TODO: found out why the we can't hear any metronome clicks, inspect timing/now functionality
}

fun void playMetronomeForTurn(int turnLengthSamples, int quarterSamples)
{
    if(turnLengthSamples <= 0 || quarterSamples <= 0) return;

    0 => int elapsed;
    0 => int beatIdx;

    while(elapsed < turnLengthSamples)
    {
        if(beatIdx % 4 == 0) metroClick(1);
        else metroClick(0);

        quarterSamples::samp => now;
        elapsed + quarterSamples => elapsed;
        beatIdx++;
    }
}

// Build an event list (time in samples, vel) from step arrays,
// sort by time (offset can reorder), then schedule sample-accurately.
fun void playTurnNow()
{
    if(stepCount <= 0 || samplesPerStep <= 0) return;

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
            vels << velocity[i];
        }
    }

    // insertion sort by time (simple + fine for modest patterns)
    for(1 => int i; i < times.size(); i++)
    {
        times[i] => int keyT;
        vels[i] => int keyV;
        i - 1 => int j;

        while(j >= 0 && times[j] > keyT)
        {
            times[j] => times[j + 1];
            vels[j] => vels[j + 1];
            j--;
        }
        keyT => times[j + 1];
        keyV => vels[j + 1];
    }

    // optionally run metronome for this turn, time-owned by ChucK
    if(metronomeEnabled != 0)
    {
        (stepCount * samplesPerStep) => int turnLen;
        (samplesPerStep * stepsPerQuarter) => int quarter;
        spork ~ playMetronomeForTurn(turnLen, quarter);
    }

    // schedule turn hits from "now"
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
