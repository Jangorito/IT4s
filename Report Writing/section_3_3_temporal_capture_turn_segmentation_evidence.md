# Section 3.3 - Temporal Capture and Turn Segmentation

## Codex Evidence Extraction

Purpose: implementation evidence for how continuous human performance is converted into bounded computational units.

Scope: input capture, OSC transport, Unity hit handling, turn segmentation, turn window construction, timing representation, precision decisions, limitations, and claim candidates.

Not included: polished dissertation prose, repeated explanation of the broader closed-loop architecture, or general real-time interaction discussion already covered in Sections 3.1 and 3.2.

---

## A. Input Capture Pipeline

Physical hit -> Bela analog input -> filtered magnitude -> discrete hit event -> OSC message -> Unity `HitEvent` -> `HitBuffer` -> `OnHitReceived`.

| Stage | Implementation Evidence | Transformation |
|---|---|---|
| Physical sensor source | `AudioEngine/src/render.cpp:13` defines `PIEZO_CH = 0`. | Reads one piezo channel from Bela analog input. |
| Detection constants | `render.cpp:15-19` defines `THRESH`, `PEAK_WIN_MS`, `REFRACT_MS`, `VEL_GAIN`, `BASE_ALPHA`. | Capture behaviour is controlled by fixed threshold, peak-window, refractory, velocity, and baseline parameters. |
| Audio/analog timing setup | `render.cpp:134-142` stores `context->analogSampleRate`, derives `gAudioPerAnalog`, converts peak/refractory milliseconds to analog sample counts. | Millisecond design parameters become sample-count windows. |
| Raw signal read | `render.cpp:186-187` calls `analogRead(context, analogFrame, PIEZO_CH)`. | Continuous piezo voltage enters the system as analog samples. |
| DC removal | `render.cpp:189-192` computes `dc = x - gLastAnalog + (DC_R * gPrevDC)`. | Slow offset drift is removed before hit detection. |
| Rectification | `render.cpp:195-197` uses `fabs(dc)`. | Bipolar sensor movement becomes magnitude. |
| Baseline tracking | `render.cpp:199-207` updates `gBaseline` only when not peaking and not refractory, then subtracts it from the signal. | Noise floor is estimated and removed outside detected hit periods. |
| Refractory logic | `render.cpp:210-215` decrements `gRefractCount` and skips detection while refractory is active. | Prevents one physical strike from being detected repeatedly. |
| Threshold detection | `render.cpp:217-225` starts `gPeaking` when `s >= THRESH`. | Continuous magnitude becomes a candidate hit. |
| Peak tracking | `render.cpp:228-235` updates `gPeak`, counts samples, and finalises once `gPeakCount >= gPeakWinSamples`. | The system waits through a short peak window before committing the event. |
| Velocity mapping | `render.cpp:237-249` maps `gPeak * VEL_GAIN` to `0..1`, then to MIDI-style `0..127`. | Analog hit strength becomes integer velocity. |
| Timestamping | `render.cpp:246` computes `tSamples = context->audioFramesElapsed + n`. | Hit time is represented in Bela audio sample time. |
| OSC event object | `render.cpp:253-257` stores `tHigh`, `tLow`, `pad`, `vel`; `pad` is currently `0`. | Hit becomes transport-ready event data. |
| Queue handoff | `render.cpp:44-83` implements a fixed-size queue; `render.cpp:259` calls `enqueueHit(ev)`. | Audio thread hands events to an OSC sender thread without blocking. |
| OSC sender | `render.cpp:86-103` dequeues events and sends `/it4/hit` with four integers. | Local hit event becomes network message. |
| Unity OSC contract | `OscHitReceiver.cs:14-18` documents `/it4/hit <tHigh:int32> <tLow:int32> <pad:int32> <vel:int32>`. | Unity expects split 64-bit sample timestamp plus pad and velocity. |
| OSC binding | `OscHitReceiver.cs:66` binds `address` to `OnHitMessage`; default address is `/it4/hit` at `OscHitReceiver.cs:24`. | Unity subscribes to the Bela hit stream. |
| Message validation | `OscHitReceiver.cs:93-97` ignores malformed messages with fewer than four args. | Transport data must contain complete timestamp/pad/velocity payload. |
| Timestamp reconstruction | `OscHitReceiver.cs:99-105` reads high/low words; `OscHitReceiver.cs:125-129` combines them using `((long)high << 32) | (uint)low`. | Unity reconstructs Bela's 64-bit sample time. |
| Velocity clamp | `OscHitReceiver.cs:110` clamps velocity to `0..127`. | Transport value is normalised into accepted hit velocity range. |
| Unity hit object | `OscHitReceiver.cs:112` creates `new HitEvent(tSamples, pad, vel)`. | OSC payload becomes the Unity-side data object. |
| Buffering | `OscHitReceiver.cs:113` calls `_buffer.Add(hitEvent)`. | Incoming hits are stored in append-only memory. |
| Event dispatch | `OscHitReceiver.cs:114` invokes `OnHitReceived`. | The turn controller is notified after the hit is stored. |

Key data structures:

| Data Structure | Evidence | Role |
|---|---|---|
| Bela `HitEvent` | `render.cpp:36-42` | Transport record containing split timestamp, pad, velocity. |
| `HitEvent` in Unity | `HitEvent.cs:5-24` | Single detected drum hit: `tSamples`, `pad`, `velocity`. |
| `HitBuffer` | `HitBuffer.cs:6-39` | Append-only storage of incoming hit events. |
| `TurnWindow` | `TurnWindow.cs:7-42` | Bounded raw captured turn in Bela sample time. |

---

## B. Turn Segmentation Mechanism

### How a Turn Begins

| Mechanism | Evidence | Logic |
|---|---|---|
| Loop arming | `TurnLoopController.cs:257-277` starts the loop and sets `CurrentPhase` to `WaitingForHuman`. | Capture cannot start until the loop is running and waiting for a human turn. |
| Event-driven waiting | `TurnLoopController.cs:352-356` leaves `WaitingForHuman` idle in `Tick()`; comment states incoming hits are handled by `HandleHitReceived`. | Waiting phase does not poll for start; it reacts to hit events. |
| Receiver subscription | `TurnLoopController.cs:612-635` subscribes `HandleHitReceived` to `hitReceiver.OnHitReceived`. | The controller receives Unity hit events from `OscHitReceiver`. |
| Phase gate | `TurnLoopController.cs:728-733` returns unless `isRunning` and `CurrentPhase == WaitingForHuman`. | Hits outside the waiting phase cannot start a turn. |
| Timestamp validity | `TurnLoopController.cs:735-739` rejects negative `tSamples`. | Capture start requires valid sample timestamp. |
| Debug start gate | `TurnLoopController.cs:648-661` checks `debugHitValidityEnabled` and optional hold key. | Additional temporary gate can allow/ignore hits. |
| Start command | `TurnLoopController.cs:746-747` emits a debug message and calls `TryStartHumanTurn(hitEvent)`. | Valid hit becomes turn-start trigger. |
| Capture flow start | `HumanTurnCaptureFlow.cs:38-77` runs `TryStartCapture`. | Start coordination is delegated to a plain C# flow. |
| Duration calculation | `HumanTurnCaptureFlow.cs:61` calls `timing.GetTurnDurationSamples()`. | Turn length is computed from configured musical timing. |
| Capture opening | `HumanTurnCaptureFlow.cs:70` calls `turnCaptureController.TryBeginCapture(triggerHit.tSamples)`. | Trigger hit sample time becomes capture start. |
| Runtime state | `TurnLoopRuntimeState.cs:38-44` records `LastTriggerHit`, `CaptureStartSamples = triggerHit.tSamples`, and `CaptureEndSamples = endSamples`. | Start and end bounds are stored explicitly. |
| Phase transition | `TurnLoopController.cs:772-780` records capture start and sets phase to `CapturingHuman`. | Segmentation begins as a phase transition. |

### How a Turn Ends

| Mechanism | Evidence | Logic |
|---|---|---|
| Fixed duration formula | `MusicalTimingConfig.cs:69-78` computes turn duration from `beatsPerBar`, `barsPerTurn`, `bpm`, and `sampleRate`. | End time is predetermined from musical timing, not detected from performance. |
| End sample | `HumanTurnCaptureFlow.cs:75` sets `endSamples = triggerHit.tSamples + turnDurationSamples`. | Turn end is absolute Bela sample time. |
| Frame-driven checking | `TurnLoopController.cs:188-194` calls `Tick()` in Unity `Update()`; `TurnLoopController.cs:785-792` ticks capture. | Unity frame loop checks whether the sample-time end has arrived. |
| Current sample time | `HumanTurnCaptureFlow.cs:106-109` calls `TryGetCurrentSampleTime`; if unavailable returns `WaitingForClock`. | Capture cannot finish until Unity has a sample-clock estimate. |
| Wait condition | `HumanTurnCaptureFlow.cs:111-114` returns `WaitingForEnd` while `currentSamples < endSamples`. | End is enforced by sample-count comparison. |
| End capture | `HumanTurnCaptureFlow.cs:116-122` calls `TryEndCapture(endSamples, out turnWindow)` when current sample estimate reaches the end. | The window is materialised only after the scheduled end. |
| Active-state guard | `TurnCaptureController.cs:79-83` refuses to end capture if no capture is active. | Prevents producing windows from inactive state. |
| End-before-start guard | `TurnCaptureController.cs:91-97` rejects `endSamples < ActiveStartSamples`. | Invalid temporal bounds are refused. |
| Turn manager end | `TurnManager.cs:34-44` returns `turnId` and `startSamples`, sets inactive, increments turn id. | Turn identity and start boundary are recovered from the active turn. |
| Phase transition after capture | `TurnLoopController.cs:813-820` stores captured window and sets phase to `CompilingHumanTurn`. | Capture output becomes the input to compilation. |

### Boundary Enforcement

| Boundary Rule | Evidence | Effect |
|---|---|---|
| Start inclusive | `HitBuffer.cs:28` checks `h.tSamples >= startSamples`. | Trigger hit is included because it was buffered before event dispatch. |
| End exclusive | `HitBuffer.cs:28` checks `h.tSamples < endSamples`. | Hits exactly on the scheduled end belong outside the turn. |
| Single active capture | `TurnCaptureController.cs:52-56` rejects `TryBeginCapture` if `_turnManager.IsActive`. | Prevents overlapping human turn windows. |
| Invalid start rejected | `TurnCaptureController.cs:58-62` rejects `startSamples < 0`. | Negative sample-time windows cannot begin. |
| Overflow guard | `HumanTurnCaptureFlow.cs:63-68` checks `triggerHit.tSamples > long.MaxValue - turnDurationSamples`. | Prevents wraparound when calculating end sample. |

---

## C. Temporal Representation

| Timing Domain | Representation | Evidence | Notes |
|---|---|---|---|
| Bela audio sample time | `int64_t tSamples = context->audioFramesElapsed + n` | `render.cpp:246` | Primary hit timestamp. |
| Bela analog processing time | Analog frames with audio-to-analog mapping | `render.cpp:134-142`, `render.cpp:178-187` | Detection runs when the current audio frame maps to an analog frame. |
| OSC transport time | Split 64-bit sample timestamp: `tHigh`, `tLow` | `render.cpp:253-257`, `OscHitReceiver.cs:14-18` | Avoids relying on OSC/Unity arrival time as musical time. |
| Unity event time | `HitEvent.tSamples` | `HitEvent.cs:11` | Unity stores Bela sample time directly. |
| Unity current sample estimate | `_lastSamples + elapsedSeconds * sampleRate` | `OscHitReceiver.cs:149-159` | Derived from last received Bela timestamp and `Time.realtimeSinceStartup`. |
| Turn window time | `startSamples`, `endSamples`, `DurationSamples` | `TurnWindow.cs:12-18` | Raw captured turn is bounded in Bela sample time. |
| Musical phrase duration | `beatsPerBar * barsPerTurn * 60 / bpm * sampleRate` | `MusicalTimingConfig.cs:69-78` | Converts configured musical timing into sample-count duration. |
| Quantisation grid | `samplesPerStep = (60/bpm)/stepsPerQuarter * sampleRate` | `PatternCompiler.cs:15-23` | Converts hit times into grid steps. |
| Microtiming | `offsetSamples[step] = relSamples - stepCenter` | `PatternCompiler.cs:46-60` | Preserves timing displacement after quantisation. |
| Playback timing | ChucK uses `dt::samp => now` | `IT4_TurnPlayer.ck:149-162` | Response playback scheduling is sample-based inside ChucK. |
| AI playback loop closure | `Time.unscaledTime` plus estimated duration | `TurnLoopController.cs:518-533` | Return to waiting is timer-estimated in Unity. |

### Timing Domain Mismatches

| Mismatch | Evidence | Consequence |
|---|---|---|
| Bela sample clock vs Unity realtime | `OscHitReceiver.cs:157-158` estimates samples from `Time.realtimeSinceStartup`. | Current sample time is approximate after the last hit. |
| Sample-accurate bounds vs frame-polled phase changes | `TurnLoopController.cs:188-194` ticks in `Update()`. | The window boundary remains sample-based, but state transition occurs on a Unity frame. |
| Bela sample time vs ChucK playback sample time | Capture timestamps come from Bela; playback uses ChucK `now` and `::samp`. | Timing is sample-based in both places, but clocks are not shown to be synchronised. |
| ChucK playback completion vs Unity estimate | `TurnLoopController.cs:232-236` states completion is timer-based because ChucK bridge lacks playback-complete callbacks. | Loop re-arming is based on predicted duration, not actual playback completion. |

---

## D. Design Decisions Recovered

| Design Decision | Evidence | Inference |
|---|---|---|
| Use sample timestamps for hits | `render.cpp:246`, `OscHitReceiver.cs:104-105`, `HitEvent.cs:11` | Musical timing is preserved independently of OSC arrival time and Unity frame timing. |
| Split 64-bit timestamp into two 32-bit words | `render.cpp:253-255`, `OscHitReceiver.cs:125-129` | OSC message remains integer-based while preserving long sample counters. |
| Use event-driven capture | `OscHitReceiver.cs:114`, `TurnLoopController.cs:728-747` | Unity orchestration starts from explicit hit events, not continuous polling or raw audio analysis. |
| Buffer before dispatch | `OscHitReceiver.cs:112-114` | Trigger hit is already in `HitBuffer` when the turn begins, so slicing can include it. |
| Fixed turn duration | `MusicalTimingConfig.cs:69-78`, `HumanTurnCaptureFlow.cs:75` | Bounded computation is prioritised over adaptive phrase ending. |
| Start at trigger sample | `HumanTurnCaptureFlow.cs:70`, `TurnLoopRuntimeState.cs:42` | The performer's first valid action defines the temporal origin of the unit. |
| Use half-open windows | `HitBuffer.cs:28` | Boundary ownership is deterministic and avoids double-counting at exact end samples. |
| Keep raw window before quantisation | `TurnWindow.cs:7-18`, `PatternCompiler.cs:11-13` | Captured data remains traceable before symbolic grid conversion. |
| Preserve microtiming after quantisation | `PatternCompiler.cs:49-60`, `PatternTurn.cs:24-25` | Quantisation does not erase all timing nuance; per-step offsets retain sample displacement. |
| Estimate current sample clock in Unity | `OscHitReceiver.cs:149-159` | Avoids requiring continuous clock messages, but introduces approximation. |

---

## E. Trade-offs and Limitations

| Trade-off / Limitation | Evidence | Explanation |
|---|---|---|
| Fixed-duration segmentation vs adaptive phrasing | `HumanTurnCaptureFlow.cs:61-75`; `MusicalTimingConfig.cs:69-78` | Turn end is predetermined by bars, beats, BPM, and sample rate. This is reliable for computation but cannot respond to natural phrase endings, pauses, or extensions. |
| Trigger-based start vs context-aware detection | `TurnLoopController.cs:728-747` | A valid hit in `WaitingForHuman` starts capture. The system does not inspect musical context, pickup intention, phrase shape, or prior silence before starting. |
| Sample timing vs cross-system synchronisation | `render.cpp:246`; `OscHitReceiver.cs:157-158` | Hit timestamps are precise in Bela sample time, but Unity estimates current sample time using its own realtime clock. This preserves event timing but not full clock synchronisation. |
| Real-time simplicity vs expressive nuance | OSC payload is only `tHigh`, `tLow`, `pad`, `vel`; see `render.cpp:94-99` and `OscHitReceiver.cs:14-18`. | The capture representation is small and fast, but discards waveform, timbre, envelope shape, stick technique, and sub-threshold gestures. |
| Peak-window robustness vs onset precision | `render.cpp:217-235`; timestamp assigned at `render.cpp:246` during finalisation. | Peak tracking improves velocity stability, but timestamp is assigned when the peak window completes rather than at threshold-crossing or actual acoustic onset. |
| Refractory protection vs fast articulation | `REFRACT_MS = 25.0f`; `render.cpp:210-215`. | Double-trigger prevention can suppress intentionally close hits, rolls, or flams within the lockout period. |
| Non-blocking audio thread vs silent event loss | `enqueueHit` returns `false` when queue is full at `render.cpp:59-60`; call at `render.cpp:259` does not check return value. | Audio thread avoids blocking, but queue overflow can drop hits without reporting them. |
| Frame-polled closure vs sample-exact state transition | Unity `Update()` calls `Tick()` at `TurnLoopController.cs:188-194`. | The stored `endSamples` is sample-based, but the capture-complete phase transition occurs only when a Unity frame checks it. |
| Quantised analysis vs performed timing | `PatternCompiler.cs:42-60` rounds each hit to a grid step and stores one velocity per step. | Structured grid analysis becomes possible, but multiple close hits on the same step collapse under the max-velocity collision rule. |
| Half-open boundary determinism vs musical boundary ambiguity | `HitBuffer.cs:28` includes start and excludes end. | Deterministic slicing avoids double counting, but a hit exactly at `endSamples` is excluded even if musically heard as turn closure. |
| Unity loop closure estimate vs actual ChucK completion | `TurnLoopController.cs:232-236`, `TurnLoopController.cs:526-529` | Re-arming can happen based on calculated duration plus padding rather than an audio-engine completion event. |

---

## F. Negative Evidence

| Not Implemented | Evidence | Impact |
|---|---|---|
| No phrase boundary detection | Turn end comes only from `GetTurnDurationSamples()` and `triggerHit.tSamples + duration`; see `MusicalTimingConfig.cs:69-78` and `HumanTurnCaptureFlow.cs:75`. | Natural phrase endings are not detected; segmentation is imposed. |
| No tempo inference during capture | BPM is a configured value in `MusicalTimingConfig.cs:13`; `QuantisationSettings.cs` stores fixed `bpm`; `PatternCompiler.cs:16` uses `60.0 / q.bpm`. | The system does not derive tempo from the performer during capture. |
| No adaptive segmentation | `CaptureEndSamples` is recorded once at start in `TurnLoopRuntimeState.cs:38-44`. | Turn length cannot adapt to density, silence, late entries, or phrase continuation. |
| No multi-turn temporal context in capture/segmentation | `TurnLoopRuntimeState.cs:20-36` stores last/current artefacts, not a history collection. | Segmentation and response preparation do not use accumulated multi-turn timing history. |
| No raw-audio semantic analysis front end | OSC payload has only timestamp, pad, velocity; `OscHitReceiver.cs:14-18`. | Later stages cannot recover timbral or gestural information discarded by Bela hit detection. |
| No multi-pad Bela capture currently | Bela-side `pad` is set to `0` in `render.cpp:256`. | OSC schema supports pad encoding, but current Bela implementation sends only one pad id. |
| No ChucK playback-complete callback | `TurnLoopController.cs:232-236` states automatic loop closure is timer-based because the bridge lacks completion callbacks. | Return-to-waiting may be early/late if playback timing differs from estimate. |
| No capture-time metrical inference | `TurnWindow` stores raw hits and bounds; quantisation occurs later in `PatternCompiler.Compile`. | Capture produces bounded data, not interpreted metrical structure. |

---

## G. Claim Candidates

1. Turn segmentation imposes computational structure onto continuous performance.
2. The first valid hit functions as both musical event and segmentation boundary.
3. Sample-level timestamps preserve performance timing across the Bela-Unity boundary.
4. OSC latency affects arrival time, but not the encoded hit time.
5. The system converts sensor continuity into sparse symbolic events before Unity analysis.
6. A `TurnWindow` is the first bounded computational unit of human performance.
7. Temporal boundaries are sample-based even though orchestration is frame-polled.
8. Segmentation is temporally rigid but computationally reliable.
9. The capture design privileges deterministic windowing over adaptive phrase recognition.
10. Microtiming is preserved after quantisation through `offsetSamples`, not through raw hit streams.
11. Unity estimates Bela time rather than sharing a synchronised hardware clock.
12. The trigger hit is included in the captured turn because buffering precedes event dispatch.
13. The system treats timing as data: sample counts become window bounds, grid positions, offsets, and playback delays.
14. Real-time capture is intentionally lossy: velocity and timing survive, richer gesture information does not.
15. Turn analysis operates on a bounded reconstruction of performance, not on continuous improvisational context.

---

## High-Value Evidence Pointers

| File | Why It Matters |
|---|---|
| `AudioEngine/src/render.cpp` | Bela thresholding, baseline removal, peak detection, refractory logic, velocity mapping, sample timestamping, OSC payload construction. |
| `GameEngine/Unity/My project/Assets/Scripts/Input/OscHitReceiver.cs` | OSC receive contract, timestamp reconstruction, velocity clamp, buffer append, event dispatch, Unity sample-clock estimate. |
| `GameEngine/Unity/My project/Assets/Scripts/Input/HitBuffer.cs` | Append-only hit storage and half-open sample slicing. |
| `GameEngine/Unity/My project/Assets/Scripts/Data/HitEvent.cs` | Unity representation of one detected hit in Bela sample time. |
| `GameEngine/Unity/My project/Assets/Scripts/Data/TurnWindow.cs` | Bounded raw captured turn representation. |
| `GameEngine/Unity/My project/Assets/Scripts/Rhythm/MusicalTimingConfig.cs` | Fixed turn duration calculation and quantisation settings derivation. |
| `GameEngine/Unity/My project/Assets/Scripts/Orchestration/HumanTurnCaptureFlow.cs` | Start/end capture scheduling and sample-clock completion checks. |
| `GameEngine/Unity/My project/Assets/Scripts/Rhythm/TurnCaptureController.cs` | Materialises `TurnWindow` from `HitBuffer.Slice(start,end)`. |
| `GameEngine/Unity/My project/Assets/Scripts/Orchestration/TurnLoopController.cs` | Phase gating, hit-triggered capture start, capture tick, captured-window event publication, timer-based AI playback loop closure. |
| `GameEngine/Unity/My project/Assets/Scripts/Rhythm/PatternCompiler.cs` | Converts bounded raw turn into fixed-BPM grid with velocity and microtiming offsets. |
| `GameEngine/Unity/My project/Assets/StreamingAssets/ChucK/IT4_TurnPlayer.ck` | Playback-side sample scheduling using `offsetSamples` and `dt::samp => now`. |
