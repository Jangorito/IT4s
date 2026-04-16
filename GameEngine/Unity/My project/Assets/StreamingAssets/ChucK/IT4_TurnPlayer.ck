// IT4_TurnPlayer.ck
// RunFile-style turn playback: Unity sets arrays + broadcasts playTurn.
// Playback timing and metronome timing are both owned by ChucK.

global int ckReady;
global Event playTurn;
global Event stopTurn;
global int playbackGeneration;

// Data pushed from Unity:
global int stepCount;
global int samplesPerStep;
global int stepsPerQuarter;
global int metronomeEnabled;
global int velocity[0];       // length = stepCount
global int offsetSamples[0];  // length = stepCount (microtiming offset from grid point)

// --- turn audio ---
SndBuf snare => Gain g => dac;
1.0 => g.gain;
me.dir() + "snare.wav" => snare.read;
0 => snare.gain; // keep silent until triggered

// --- metronome audio ---
SndBuf metronome => Gain mGain => dac;
me.dir() + "metronome.wav" => metronome.read;
0 => mGain.gain;
0 => metronome.gain;


1 => ckReady;

// helper: clamp
fun int clampInt(int x, int lo, int hi)
{
    if(x < lo) return lo;
    if(x > hi) return hi;
    return x;
}

// silence any currently ringing debug playback voices
fun void silencePlayback()
{
    0 => snare.gain;
    0 => metronome.gain;
    0 => mGain.gain;
}

// play a snare hit (velocity 0..127)
fun void hit(int v, int generation)
{
    if(generation != playbackGeneration) return;

    (clampInt(v, 0, 127) / 127.0) => float amp;
    0 => snare.pos;
    amp => snare.gain;

    // tiny wait to avoid edge cases on rapid retrigger
    5::ms => now;

    if(generation != playbackGeneration) silencePlayback();
}

fun void metroClick(int accented, int generation)
{
    if(generation != playbackGeneration) return;

    <<< "Metronome click (accented:" + accented + ")" >>>;
    if(accented != 0)
    {
        1.0 => mGain.gain;
    }
    else
    {
        0.7 => mGain.gain;
    }

    0 => metronome.pos;
    1 => metronome.gain;
}

fun void playMetronomeForTurn(int turnLengthSamples, int quarterSamples, int generation)
{
    if(turnLengthSamples <= 0 || quarterSamples <= 0) return;

    0 => int elapsed;
    0 => int beatIdx;

    while(elapsed < turnLengthSamples && generation == playbackGeneration)
    {
        if(beatIdx % 4 == 0) metroClick(1, generation);
        else metroClick(0, generation);

        quarterSamples::samp => now;
        elapsed + quarterSamples => elapsed;
        beatIdx++;
    }
}

// Build an event list (time in samples, vel) from step arrays,
// sort by time (offset can reorder), then schedule sample-accurately.
fun void playTurnNow(int generation)
{
    if(stepCount <= 0 || samplesPerStep <= 0) return;
    if(generation != playbackGeneration) return;

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
        spork ~ playMetronomeForTurn(turnLen, quarter, generation);
    }

    // schedule turn hits from "now"
    0 => int lastT;
    for(0 => int k; k < times.size(); k++)
    {
        if(generation != playbackGeneration) return;

        (times[k] - lastT) => int dt;
        if(dt > 0) dt::samp => now;
        times[k] => lastT;

        if(generation != playbackGeneration) return;

        spork ~ hit(vels[k], generation);
    }
}

fun void listenForStopTurn()
{
    while(true)
    {
        stopTurn => now;
        playbackGeneration++;
        silencePlayback();
    }
}

spork ~ listenForStopTurn();

while(true)
{
    playTurn => now;
    playbackGeneration => int generation;
    spork ~ playTurnNow(generation);
}
