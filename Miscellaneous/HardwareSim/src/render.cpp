/*
 * render.cpp — Bela piezo disc sensor input
 *
 * Hardware:
 *   Piezo disc → 100k series R → Bela analog in 0 (channel 0)
 *                             → 1MΩ bleed R → GND
 *   Bela GND  ←→  breadboard GND rail  ←→  piezo black wire
 *
 * Signal chain (per analog frame, at 22.05 kHz):
 *   1. Raw read       analogRead() → 0.0–1.0
 *   2. DC removal     first-order high-pass IIR (R = DC_FILTER_COEFF)
 *   3. Rectification  full-wave absolute value
 *   4. Smoothing      low-pass IIR envelope follower
 *   5. Onset detect   threshold + refractory window
 *
 * Bela SDK refs:
 *   analogRead()     — docs.bela.io/group__iofunctions.html
 *   Analog example   — docs.bela.io/Analog_2analog-input_2render_8cpp-example.html
 *   Piezo example    — docs.bela.io/Audio_2sample-piezo-trigger_2render_8cpp-example.html
 */

#include <Bela.h>
#include <cmath>
#include <cstdio>

#include "config.h"

// ─── Module state (file-scope, zero-initialised) ─────────────────────────────

// Ratio: how many audio frames advance before one new analog sample arrives.
// Typical value: 2  (audio at 44.1 kHz, analog at 22.05 kHz).
static int   gAudioFramesPerAnalogFrame = 0;

// DC-offset filter state
static float gPrevRaw       = 0.0f;
static float gPrevDcOut     = 0.0f;

// Envelope follower state
static float gSmoothed      = 0.0f;

// Onset refractory counter (counts down after each detected onset)
static int   gRefractorySamples = 0;

// Debug print counter
static int   gDebugCounter  = 0;

// ─── setup() — called once before rendering starts ───────────────────────────

bool setup(BelaContext* context, void* /* userData */)
{
    if (context->analogFrames == 0) {
        rt_printf("[piezo] ERROR: Analog inputs not enabled.\n"
                  "        Enable them in the Bela IDE → Settings → Analog.\n");
        return false;
    }

    gAudioFramesPerAnalogFrame =
        static_cast<int>(context->audioFrames) /
        static_cast<int>(context->analogFrames);

    rt_printf("[piezo] Audio sample rate    : %.0f Hz\n", context->audioSampleRate);
    rt_printf("[piezo] Analog sample rate   : %.0f Hz\n", context->analogSampleRate);
    rt_printf("[piezo] Analog frames/block  : %u\n",      context->analogFrames);
    rt_printf("[piezo] Audio/analog ratio   : %d\n",      gAudioFramesPerAnalogFrame);
    rt_printf("[piezo] Analog input channel : %d\n",      PIEZO_ANALOG_CHANNEL);
    rt_printf("[piezo] Onset threshold      : %.4f\n",    ONSET_THRESHOLD);
    rt_printf("[piezo] Setup complete.\n");

    return true;
}

// ─── render() — called every audio block (real-time, no allocation) ──────────

void render(BelaContext* context, void* /* userData */)
{
    // Iterate over audio frames; analog samples arrive at a sub-multiple rate.
    for (unsigned int n = 0; n < context->audioFrames; ++n) {

        // Only process analog when a new analog sample is available.
        if (gAudioFramesPerAnalogFrame == 0 ||
            (n % static_cast<unsigned int>(gAudioFramesPerAnalogFrame)) != 0) {
            continue;
        }

        const int analogFrame =
            static_cast<int>(n) / gAudioFramesPerAnalogFrame;

        // ── 1. Raw read ───────────────────────────────────────────────────────
        // Range: 0.0 (0 V) to 1.0 (4.096 V).
        // The 100k series R limits transient current; the 1 MΩ bleed provides
        // a DC return path so the node does not float between strikes.
        const float raw = analogRead(context, analogFrame, PIEZO_ANALOG_CHANNEL);

        // ── 2. DC-offset removal (first-order high-pass IIR) ─────────────────
        // y[n] = R * y[n-1]  +  x[n]  -  x[n-1]
        // Removes the ~0.5 V bias the bleed resistor centres the signal at.
        const float dcOut = raw - gPrevRaw + DC_FILTER_COEFF * gPrevDcOut;
        gPrevRaw  = raw;
        gPrevDcOut = dcOut;

        // ── 3. Full-wave rectification ────────────────────────────────────────
        const float rectified = (dcOut < 0.0f) ? -dcOut : dcOut;

        // ── 4. Envelope follower (low-pass) ───────────────────────────────────
        gSmoothed = SMOOTHING_ALPHA * rectified +
                    (1.0f - SMOOTHING_ALPHA) * gSmoothed;

        // ── 5. Onset detection ────────────────────────────────────────────────
        if (gRefractorySamples > 0) {
            --gRefractorySamples;
        }

        const bool onset = (rectified > ONSET_THRESHOLD) &&
                           (gRefractorySamples == 0);

        if (onset) {
            gRefractorySamples = ONSET_REFRACTORY_SAMPLES;
            const float voltage = raw * 4.096f;
            rt_printf("[piezo] ONSET  raw=%.4f  voltage=%.3fV  smoothed=%.4f\n",
                      raw, voltage, gSmoothed);

            // ── Insert your response here ─────────────────────────────────────
            // Examples:
            //   audioWrite(context, n, 0, 1.0f);  // click on L channel
            //   gOscPhase = 0.0f;                  // re-trigger an oscillator
            //   postEvent(kCustomEventType, 0, 0); // signal the auxiliary task
        }

        // ── 6. Debug print (non-onset, throttled) ─────────────────────────────
#if DEBUG_PRINT_INTERVAL > 0
        if (++gDebugCounter >= DEBUG_PRINT_INTERVAL) {
            gDebugCounter = 0;
            rt_printf("[piezo] raw=%.4f  dc=%.4f  rect=%.4f  smooth=%.4f\n",
                      raw, dcOut, rectified, gSmoothed);
        }
#endif
    }

    // Silence audio output (this sketch does not drive audio).
    for (unsigned int n = 0; n < context->audioFrames; ++n) {
        for (unsigned int ch = 0; ch < context->audioOutChannels; ++ch) {
            audioWrite(context, n, ch, 0.0f);
        }
    }
}

// ─── cleanup() — called when rendering stops ─────────────────────────────────

void cleanup(BelaContext* /* context */, void* /* userData */)
{
    // Nothing to free — no dynamic allocation used.
}
