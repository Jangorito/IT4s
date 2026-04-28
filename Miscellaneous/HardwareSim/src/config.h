#pragma once

// ─── Analog input ────────────────────────────────────────────────────────────
// Bela analog input channel connected to the piezo signal line.
// Channel 0 = first analog header pin on the Bela board.
#define PIEZO_ANALOG_CHANNEL    0

// ─── DC-offset filter ────────────────────────────────────────────────────────
// First-order high-pass IIR coefficient.
// Higher value → slower decay, better at removing very low-frequency drift.
// 0.99 is a standard starting point for piezo conditioning.
#define DC_FILTER_COEFF         0.99f

// ─── Onset detection ─────────────────────────────────────────────────────────
// Threshold applied to the rectified, DC-filtered signal.
// Values are normalized (0.0–1.0) relative to the Bela 4.096V ADC range.
// Start at 0.02 and adjust based on sensor placement and strike force.
#define ONSET_THRESHOLD         0.02f

// Minimum gap between consecutive onsets, in analog samples (~22.05 kHz).
// 2205 samples ≈ 100 ms — prevents re-triggering during the same strike.
#define ONSET_REFRACTORY_SAMPLES  2205

// ─── Smoothing (optional envelope follower) ──────────────────────────────────
// Low-pass alpha for smoothed amplitude envelope (0 = frozen, 1 = instant).
// Use the smoothed value to drive a continuous parameter (volume, filter freq).
#define SMOOTHING_ALPHA         0.05f

// ─── Debug output ────────────────────────────────────────────────────────────
// Print raw and processed values to the Bela terminal every N analog frames.
// Set to 0 to disable printing (reduces CPU load in production).
#define DEBUG_PRINT_INTERVAL    2205    // ~100 ms at 22.05 kHz analog rate
