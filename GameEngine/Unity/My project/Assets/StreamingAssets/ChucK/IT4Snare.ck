// IT4Snare.ck
// Minimal "trigger a snare sample" script for Chunity RunFile workflow.

global int ckReady;
global Event snareTrig;

// Load snare sample (keep snare.wav in the same folder as this .ck file)
SndBuf snare => Gain g => dac;
1.0 => g.gain;

// Build the file path relative to this script's directory
me.dir() + "snare.wav" => snare.read;

// Optional: prevent any initial sound
0 => snare.gain;

// Signal to Unity that ChucK finished init
1 => ckReady;

// Play function
fun void playSnare()
{
    // Reset playhead and trigger
    0 => snare.pos;

    // Basic hit shaping
    1.0 => snare.gain;

    // Let it ring; small wait prevents re-trigger issues if spammed
    10::ms => now;

    // (Optional) you can fade gain down here if you want tighter hits later
    // 0.9 => snare.gain;
    <<< "[CHUCK] Played snare hit." >>>;
}

while(true)
{
    snareTrig => now;
    spork ~ playSnare();
}
