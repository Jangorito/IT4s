# Chapter 4 Implementation Audit

Date: 2026-04-24

Scope:
- Audit the implemented Intelli-Trading Fours codebase against the current dissertation draft, planning notes, marking guidance, and implementation/design notes.
- Extract implementation evidence and possible Chapter 4 structures.
- Do not write dissertation prose.

Reviewed context:
- `Intelli-Trading Fours_ Final Report (4).docx`
- `Dissertation_Writeup_Spec.md`
- `Report Writing/Marking Guidance/working_marking_notes.md`
- `Report Writing/RemainingSections.md`
- `chapter3_system_design_strict_structural_audit.md`
- `Report Writing/section_3_3_temporal_capture_turn_segmentation_evidence.md`
- `Report Writing/section_3_4_symbolic_representation_quantisation_evidence.md`
- `Report Writing/section_3_5_analysis_response_generation_evidence.md`
- `Report Writing/section_3_5_analysis_response_generation_design_audit.md`
- `Misc/Response/Planning/ResponsePlanner_Spec.md`
- `Misc/Response/Skeleton Gen/skeleton_builder_spec_unified.md`
- `Misc/TurnAnalysis/TurnAnalysis_Codex_Treatment_UPDATED_v5.md`
- `Report Writing/Marking Guidance/Final+Year+(3rd+Year)+Project+Handbook.pdf`
- `Report Writing/Marking Guidance/dissertationMarkingRubric.pdf`

Context implications:
- The current report draft already frames the project as a closed-loop prototype with implemented capture, representation, analysis, planning, playback, and loop closure, while explicitly treating final response realisation as incomplete (`Intelli-Trading Fours_ Final Report (4).docx`, paras 19-45, 191-220, 291-359).
- Chapter 3 is now conceptual. The old subsystem/mechanics material was explicitly identified as Chapter 4 material (`chapter3_system_design_strict_structural_audit.md:13-16`, `184-197`).
- Marking guidance pressures Chapter 4 toward visible technical achievement, justified engineering choices, concise structure, and honesty about limitations, rather than a code walkthrough (`working_marking_notes.md:5-14`; handbook BCS summary; rubric KU1/KU2/CT1-CT5).
- Chapter 4 should prove what was built. Chapter 5 should prove how well it behaves. Do not mix those jobs.

## 1. Whole-System Runtime Chain

| Stage | Implementation layer | Component / file | Responsibility | Input | Output | Status | Notes for dissertation relevance |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Physical hardware | Drum pad / piezo -> Bela analog A0 via `AudioEngine/src/render.cpp:13` | Sense physical strike as analog voltage | Physical hit | Analog sample stream | Live | Bela is part of the implemented system, not a peripheral footnote. |
| 2 | Bela detection | `render.cpp:184-235` | DC removal, rectification, baseline subtraction, thresholding, peak tracking, refractory lockout | Analog samples | Candidate hit with peak magnitude | Live | This is the actual onset extraction layer. |
| 3 | Bela event encoding | `render.cpp:237-259` | Map peak to MIDI-style velocity, timestamp in Bela sample time, split 64-bit sample count, enqueue event | Detected peak | `HitEvent { tHigh, tLow, pad, vel }` | Live | Shows how physical timing is preserved across the hardware/software boundary. |
| 4 | Bela transport | `render.cpp:52-103` | Non-blocking queue handoff and OSC sending on `/it4/hit` | Bela hit event | OSC message | Live | Important engineering decision: keep OSC send work off the audio thread. |
| 5 | Unity OSC receive | `Assets/Scripts/Input/OscHitReceiver.cs:13-20,84-160` | Bind to `/it4/hit`, validate payload, rebuild 64-bit timestamp, clamp velocity | OSC message | Unity `HitEvent` | Live | Concrete Bela -> Unity contract. |
| 6 | Unity buffering | `Assets/Scripts/Input/HitBuffer.cs:9-38` | Append incoming hits for later slicing | `HitEvent` | Buffered hit stream | Live | Separates continuous event arrival from later turn construction. |
| 7 | Turn start gate | `Assets/Scripts/Orchestration/TurnLoopController.cs:744-799` | Accept hits only in `WaitingForHuman`, then start human turn | Buffered / dispatched hit | Capture start request | Live | Makes the trading-fours interaction model concrete. |
| 8 | Turn duration scheduling | `Assets/Scripts/Orchestration/HumanTurnCaptureFlow.cs:38-77`, `Assets/Scripts/Rhythm/MusicalTimingConfig.cs:69-83` | Compute fixed turn duration in samples and open capture | Trigger hit + timing config | `CaptureStartSamples`, `CaptureEndSamples` | Live | Shows fixed-duration turn segmentation. |
| 9 | Timestamp reconstruction for capture end | `OscHitReceiver.cs:150-160` | Estimate current sample time from last Bela timestamp plus Unity realtime | Last received Bela timestamp | Approximate current sample time | Live | Important limitation: Unity does not share a continuous Bela clock. |
| 10 | Turn materialisation | `HumanTurnCaptureFlow.cs:79-123`, `TurnCaptureController.cs:70-123` | End capture at scheduled sample, slice buffer into bounded raw turn | Capture bounds + buffered hits | `TurnWindow` | Live | First bounded computational unit of performance. |
| 11 | Symbolic compilation | `Assets/Scripts/Rhythm/PatternCompiler.cs:11-74` | Quantise raw hits into fixed grid with per-step velocity and microtiming offset | `TurnWindow` + `QuantisationSettings` | `PatternTurn` | Live | Central representation bridge into analysis, planning, generation, playback, and debug. |
| 12 | Analysis | `Assets/Scripts/Rhythm/TurnAnalysis/TurnAnalyser.cs:30-47` plus analyser classes | Extract density, energy, anchors, end activity, segment profile | Human `PatternTurn` | `TurnAnalysisResult` | Live but not audible | Runs in the live loop but does not currently shape the final sound path. |
| 13 | Planning | `Assets/Scripts/Rhythm/ResponsePlanning/ResponsePlanner.cs:27-38,49-131` | Choose response type and derive control parameters | `TurnAnalysisResult` | `ResponsePlan` | Live but not audible | Real implemented reasoning layer; current sound path does not consume it directly. |
| 14 | Prototype structural generation | `Assets/Scripts/Rhythm/Generation/Skeleton/SkeletonBuilder.cs:28-62` | Build structural scaffold from plan, source turn, and analysis | `SkeletonBuildRequest` | `SkeletonPattern` | Live but not audible prototype | Executes live, but output is currently stored as debug state rather than realised in sound. |
| 15 | Current audible response generation | `Assets/Scripts/Rhythm/Transformations/FeatureTransformer.cs:46-279` | Heuristic transformation of the compiled human pattern | Human `PatternTurn` | Generated AI `PatternTurn` | Live | This is the actual current response realiser. |
| 16 | Playback handoff | `TurnLoopController.cs:885-943`, `Assets/Scripts/ChunityAudio/IT4ChuckTurnPlayer.cs:80-160` | Pass final generated `PatternTurn` to ChucK bridge | Generated AI `PatternTurn` | ChucK globals + `playTurn` event | Live | This is where the live sound path becomes fixed: only `PatternTurn` crosses the boundary. |
| 17 | ChucK playback | `Assets/StreamingAssets/ChucK/IT4_TurnPlayer.ck:100-182` | Build timed event list, sort by microtiming, trigger snare/metronome with sample delays | `velocity[]`, `offsetSamples[]`, timing globals | Audible output | Live | ChucK owns short-term sample scheduling. |
| 18 | Loop closure | `TurnLoopController.cs:507-590` | Estimate playback duration from `PatternTurn`, wait plus padding, clear capture-local state, return to waiting | Generated AI `PatternTurn` + Unity realtime | `WaitingForHuman` re-armed | Live | Closed-loop orchestration is implemented, but completion is timer-estimated rather than callback-confirmed. |

## 2. Subsystem Evidence Register

### A. Bela Hardware and Sensor Input
- Purpose: Convert physical drum-pad strikes into timestamped hit events before Unity sees anything.
- Key files / classes: `AudioEngine/src/render.cpp:13-29,52-103,126-265`.
- Responsibilities: analog read, DC removal, rectification, baseline tracking, threshold detection, peak window, refractory lockout, velocity mapping, timestamping, OSC event creation.
- Inputs / outputs: piezo voltage -> `/it4/hit` payload with split sample timestamp, pad id, velocity.
- Dissertation value: proves the system begins at physical interaction, not just simulated Unity input.
- Chapter 4 placement: main text.
- Do not over-explain: DSP theory, every arithmetic line, Bela setup boilerplate.

### B. OSC Transport and Unity Input
- Purpose: Preserve Bela timing across the Bela -> Unity boundary and store hits for later turn slicing.
- Key files / classes: `OscHitReceiver.cs:13-20,84-160`, `HitBuffer.cs:9-38`, `HitEvent.cs:9-24`, `InputModeBootstrap.cs:6-107`.
- Responsibilities: bind OSC address, validate payload, reconstruct 64-bit sample time, clamp velocity, append to buffer, expose event callback, select live vs fake/emulated input rigs.
- Inputs / outputs: OSC `/it4/hit` -> Unity `HitEvent` + buffered hit stream.
- Dissertation value: makes the hardware/software interface explicit and testable.
- Chapter 4 placement: main text.
- Do not over-explain: extOSC internals, inspector minutiae, every bind/unbind detail.

### C. Turn Capture and Temporal Segmentation
- Purpose: Turn a continuous hit stream into a bounded human turn.
- Key files / classes: `HumanTurnCaptureFlow.cs:38-123`, `TurnCaptureController.cs:40-123`, `TurnLoopController.cs:744-877`, `MusicalTimingConfig.cs:69-83`, `TurnWindow.cs:10-42`.
- Responsibilities: start on first valid hit, schedule fixed end sample, poll estimated sample clock, slice buffer, create `TurnWindow`.
- Inputs / outputs: trigger `HitEvent` + timing config + buffer -> `TurnWindow`.
- Dissertation value: concrete implementation of the trading-fours turn boundary.
- Chapter 4 placement: main text.
- Do not over-explain: every controller helper method or runtime-state reset path.

### D. Symbolic Representation and Quantisation
- Purpose: Convert raw timestamped hits into a deterministic symbolic rhythm representation.
- Key files / classes: `PatternCompiler.cs:11-74`, `PatternTurn.cs:9-28`, `QuantisisationSettings.cs`.
- Responsibilities: compute samples-per-step, map hits to nearest grid step, keep max-velocity collision rule, store microtiming offsets.
- Inputs / outputs: `TurnWindow` -> `PatternTurn`.
- Dissertation value: this is the bridge from performed rhythm to computational reasoning.
- Chapter 4 placement: main text.
- Do not over-explain: array boilerplate or obvious property accessors.

### E. TurnLoop / Orchestration
- Purpose: Own the live turn-taking state machine and connect all runtime collaborators.
- Key files / classes: `TurnLoopBootstrap.cs:16-145`, `TurnLoopController.cs:17-1160`, `TurnLoopRuntimeState.cs:8-109`.
- Responsibilities: dependency wiring, phase control, hit gating, capture/compile/generate/play transitions, error handling, optional closed-loop re-arm.
- Inputs / outputs: scene references + subsystem outputs -> repeated interaction loop.
- Dissertation value: core technical achievement; makes the system a closed-loop interaction prototype rather than a disconnected set of modules.
- Chapter 4 placement: main text.
- Do not over-explain: every event, every debug message, every setter/getter.

### F. Analysis Layer
- Purpose: Compute interpretable rhythmic descriptors from the compiled human turn.
- Key files / classes: `TurnAnalyser.cs:8-47`, `DensityAnalyser.cs`, `EnergyAnalyser.cs`, `AnchorAnalyser.cs:9-247`, `EndActivityAnalyser.cs:7-52`, `SegmentActivityProfileAnalyser.cs:8-88`.
- Responsibilities: density, velocity-based energy, anchor salience, final-quarter end activity, four-segment activity-shape classification.
- Inputs / outputs: `PatternTurn` -> `TurnAnalysisResult`.
- Dissertation value: strong evidence for interpretable analysis rather than opaque end-to-end generation.
- Chapter 4 placement: main text, but detailed formulae and threshold tables can go to appendix.
- Do not over-explain: full derivation of every weight or threshold in prose.

### G. Response Planner
- Purpose: Convert analysed features into explicit response intent.
- Key files / classes: `ResponsePlanner.cs:27-38,49-131,221-380`, `ResponsePlan.cs:6-52`, `ResponseType.cs:3-11`, `ResponsePlannerConfig.cs:3-41`, `ResponsePlannerDebugSnapshot.cs`.
- Responsibilities: derive planning context, score response types, resolve ties, emit `ResponsePlan`, keep inspectable decision snapshot.
- Inputs / outputs: `TurnAnalysisResult` -> `ResponsePlan`.
- Dissertation value: this is the implemented decision layer the dissertation can legitimately claim.
- Chapter 4 placement: main text.
- Do not over-explain: every branch of scoring logic line by line; put large score tables or full rule matrix in appendix if needed.

### H. Skeleton / Structural Generation
- Purpose: Convert plan intent into a sparse structural scaffold over the response timeline.
- Key files / classes: `SkeletonBuilder.cs:28-62,110-180`, `SkeletonBuilderConfig.cs:3-21`, `SkeletonPattern.cs:6-56`, `SkeletonDebugSnapshot.cs:9-173`, `Misc/Response/Skeleton Gen/skeleton_builder_spec_unified.md`.
- Responsibilities: derive metrical/source/anchor/ending maps, score steps, perform constrained selection, produce debug-rich `SkeletonPattern`.
- Inputs / outputs: `SkeletonBuildRequest` -> `SkeletonPattern`.
- Dissertation value: technically meaningful prototype structural generator.
- Chapter 4 placement: main text, but fine-grained score decomposition and selection heuristics are appendix material.
- Do not over-explain: every helper function inside the builder.

### I. Current Audible Response Generation
- Purpose: Produce the actual played AI pattern in the current system.
- Key files / classes: `FeatureTransformer.cs:46-279`, unused `SimpleTransformer.cs:11-135`.
- Responsibilities: compute lightweight local features and apply one of three heuristic transforms (`EchoAccent`, `EndFill`, `SparseOrnament`) via `Auto`.
- Inputs / outputs: compiled human `PatternTurn` -> generated AI `PatternTurn`.
- Dissertation value: necessary to explain current sound output honestly.
- Chapter 4 placement: main text.
- Do not over-explain: heuristic details as though they were the intended final generator.

### J. ChucK / Chunity Playback
- Purpose: Turn the generated symbolic pattern back into sound.
- Key files / classes: `IT4ChuckTurnPlayer.cs:7-160`, `IT4_TurnPlayer.ck:1-182`.
- Responsibilities: load ChucK script, poll readiness, push arrays and timing globals, broadcast `playTurn`, schedule sample-accurate snare hits and optional metronome, stop playback.
- Inputs / outputs: generated AI `PatternTurn` -> audible playback.
- Dissertation value: proves the loop closes with real audio, not just symbolic output.
- Chapter 4 placement: main text.
- Do not over-explain: ChucK syntax tutorials or low-level sample-player boilerplate.

### K. Debugging, Observability and Diagnostics
- Purpose: Make internal state visible during runtime and support exportable inspection.
- Key files / classes: `TurnLoopDebugPresenter.cs:11-220`, `PatternTurnDebugRenderer.cs:6-200`, `ResponsePlannerDebugRenderer.cs`, `SkeletonDebugRenderer.cs`, `TurnAnalysisSummaryFormatter.cs:8-197`, `EvaluationTraceLogger.cs`.
- Responsibilities: show phases, pattern grids, analysis summaries, planner decisions, skeleton annotations, panel export screenshots.
- Inputs / outputs: runtime events and model objects -> on-screen UI / exported PNGs / logs.
- Dissertation value: strong evidence for interpretability and engineering maturity.
- Chapter 4 placement: short main-text section plus appendix screenshots.
- Do not over-explain: GUI layout constants or every rendering helper.

### L. Testing and Validation Support
- Purpose: Provide deterministic evidence for subsystem correctness and controlled batch evaluation.
- Key files / classes: `Assets/Tests/EditMode/Editor/*.cs`, `TemporaryTurnAnalysisPlannerBatchRunner.cs:16-130`, `TemporaryTimingAnalysisRunner.cs:23-150`, generated outputs under `DiagnosticsOutput/` and `Evaluation/`.
- Responsibilities: edit-mode tests for analysis, planning, skeleton generation, capture flow, debug re-arm, loop closure, and AI preparation; editor-only batch runners for planner/skeleton/timing datasets.
- Inputs / outputs: synthetic patterns / test fixtures -> assertions, CSV/JSON/Markdown reports, timing tables.
- Dissertation value: directly supports marking criteria around technical achievement, evidence, and evaluation discipline.
- Chapter 4 placement: brief main-text coverage; detailed results belong in Chapter 5 and appendices.
- Do not over-explain: every test case or every CSV column in the main text.

## 3. Live vs Prototype vs Deferred Classification

| Part | Classification | Evidence | Dissertation wording recommendation |
| --- | --- | --- | --- |
| Bela piezo input and hit detection | LIVE | `render.cpp:13-29,184-259` reads analog piezo, detects hits, timestamps, sends OSC. | "Implemented live physical input layer." |
| OSC transport and `OscHitReceiver` | LIVE | `render.cpp:94-99`; `OscHitReceiver.cs:84-160`. | "Implemented live Bela-to-Unity event transport." |
| `HitBuffer`, `TurnCaptureController`, `HumanTurnCaptureFlow` | LIVE | `HitBuffer.cs:20-33`; `TurnCaptureController.cs:40-123`; `HumanTurnCaptureFlow.cs:38-123`. | "Implemented live turn-capture pipeline." |
| `PatternCompiler` / `PatternTurn` | LIVE | `PatternCompiler.cs:11-74`; `PatternTurn.cs:9-28`. | "Implemented live symbolic representation layer." |
| `TurnLoopController` / `TurnLoopBootstrap` | LIVE | `TurnLoopBootstrap.cs:86-145`; `TurnLoopController.cs:268-287,356-397,885-943`. | "Implemented live closed-loop orchestration." |
| `TurnAnalyser` and analysis modules | LIVE-BUT-NOT-AUDIBLE | `TurnAnalyser.cs:30-47` executes in live prep path; current sound path does not consume its outputs directly. | "Implemented live interpretable analysis; current audible output does not directly realise these features." |
| `ResponsePlanner` | LIVE-BUT-NOT-AUDIBLE | `AiResponsePreparationFlow.cs:67-85` runs every live response; `FeatureTransformer` ignores its output. | "Implemented live planning layer whose output is currently inspectable rather than directly sonified." |
| `ResponsePlan` | LIVE-BUT-NOT-AUDIBLE | Stored in runtime state and debug output (`TurnLoopController.cs:1035-1043`), not consumed by playback boundary. | "Live control object for reasoning, not yet a direct audio contract." |
| `SkeletonBuilder` / `SkeletonPattern` | LIVE-BUT-NOT-AUDIBLE | `AiResponsePreparationFlow.cs:87-101` builds skeleton in live path; `TurnLoopController.cs:1045-1057` stores snapshot only. | "Live prototype structural generator; outputs are not yet the source of audible rhythm." |
| `FeatureTransformer` | LIVE | `AiResponsePreparationFlow.cs:110-119`; `FeatureTransformer.cs:46-279`. | "Current interim audible response generator." |
| `IT4ChuckTurnPlayer` + `IT4_TurnPlayer.ck` | LIVE | `TurnLoopController.cs:936-942`; `IT4ChuckTurnPlayer.cs:80-160`; `IT4_TurnPlayer.ck:100-182`. | "Implemented live playback bridge and audio renderer." |
| Timer-based loop closure | LIVE | `TurnLoopController.cs:507-590`. | "Implemented live loop closure using duration estimation rather than confirmed playback callback." |
| `TurnLoopDebugPresenter`, pattern/planner/skeleton renderers | DIAGNOSTIC | `TurnLoopDebugPresenter.cs:11-220` and related renderers read controller state and draw/export panels. | "Runtime observability and debugging infrastructure, not musical generation." |
| `FakeBelaOscSender`, `KeyboardBelaOscEmulator`, `HitBufferSmokeTest` | DIAGNOSTIC | `InputModeBootstrap.cs:6-107`; fake/emulator scripts. | "Alternate test-input rigs for development and validation." |
| `TemporaryTurnAnalysisPlannerBatchRunner` | DIAGNOSTIC | Explicitly editor-only and bypasses live capture, playback, and Play Mode (`TemporaryTurnAnalysisPlannerBatchRunner.cs:16-20`). | "Editor-only diagnostic/evaluation harness." |
| `TemporaryTimingAnalysisRunner` | DIAGNOSTIC | Editor-only; simulates playback payload prep instead of driving live playback (`TemporaryTimingAnalysisRunner.cs:23-29,213-238`). | "Editor-only timing-analysis harness, not a live runtime path." |
| `SimpleTransformer` | PROTOTYPE | Exists but repository search finds no production reference; code is standalone (`SimpleTransformer.cs:11-135`). | "Unused older transformation prototype; not part of the live system." |
| Full plan/skeleton-to-sound realiser | DEFERRED | Design/spec documents expect downstream realiser stages; no production bridge exists. | "Deferred full response realisation." |
| Long-term memory / multi-turn adaptation | DEFERRED | `TurnLoopRuntimeState.cs:14-36` stores only last/current artefacts; no history structure or learning path exists. | "Deferred multi-turn memory and adaptation." |
| Tempo inference / adaptive phrasing | DEFERRED | `MusicalTimingConfig.cs:69-83` and `PatternCompiler.cs:15-27` use fixed tempo and fixed grid; no inference step. | "Deferred tempo-adaptive capture and representation." |

## 4. Implementation Divergence Audit

| Intended design | Current implementation | Exact divergence | Consequence for dissertation claims | Where to discuss |
| --- | --- | --- | --- | --- |
| `ResponsePlan -> SkeletonBuilder -> downstream realiser -> PatternTurn` (`ResponsePlanner_Spec.md:23-35`; `skeleton_builder_spec_unified.md:32-65`) | `TurnAnalyser -> ResponsePlanner -> SkeletonBuilder -> FeatureTransformer.Transform(compiledPattern)` (`AiResponsePreparationFlow.cs:67-119`) | The reasoning branch and the audible branch run side by side, then split. The played pattern is not created from `ResponsePlan` or `SkeletonPattern`. | Do not claim that planned structure is directly realised in sound. | Chapter 4 explicitly; Chapter 5 for fidelity consequences; future work for repair path. |
| Planned structural generation should feed richer motif / ending / constraint stages (`ResponsePlanner_Spec.md:23-35`; `skeleton_builder_spec_unified.md:32-47`) | Current sound path uses three lightweight transform rules in `FeatureTransformer` (`FeatureTransformer.cs:147-247`) | The current audible generator is an interim heuristic transformer, not the intended fuller realisation stack. | Chapter 4 must call the audible path interim, not mature generative response realisation. | Chapter 4 and future work. |
| Intended system could support richer musical continuation and adaptive response over time | `TurnLoopRuntimeState.cs:14-36` stores only the current/last turn artefacts; no history, memory, adaptation, or learning system exists | The implemented system is turn-local. | Do not claim long-term memory, adaptive collaboration, or learned stylistic continuity. | Briefly in Chapter 4; critically in Chapter 5; future work. |
| A musically responsive system might infer tempo, adapt phrase length, or use flexible segmentation | `MusicalTimingConfig.cs:69-83` fixes turn length from configured bars/beats/BPM; `PatternCompiler.cs:15-27` fixes grid from configured BPM and SPQ | Turn timing is imposed, not inferred. Quantisation is fixed-grid and fixed-tempo. | Do not imply tempo tracking, adaptive phrase endings, or groove-aware analysis. | Chapter 4 as implementation assumption; Chapter 5 as limitation. |
| OSC schema and Unity-side hit model support multi-pad input (`HitEvent.pad`) | Bela sender hardcodes `ev.pad = 0` in `render.cpp:256`; keyboard emulator maps multiple pads but live Bela path does not | Live hardware path is currently single-pad / single-stream even though the software contract is wider. | Do not overstate drum-kit voice awareness or multi-pad rhythmic dialogue. | Chapter 4 and future work. |
| Hardware-clock precision should ideally remain shared end to end | Unity estimates current sample time from the last received Bela timestamp (`OscHitReceiver.cs:150-160`) rather than receiving a continuous shared hardware clock | Turn-end checking is sample-referenced but clock reconstruction between hits is approximate. | Claim sample-referenced capture, not fully synchronised shared-clock operation. | Chapter 4; Chapter 5 only if timing evidence is presented. |
| Robust live sensing should minimise silent event loss and articulation suppression | Bela queue overflow is ignored (`render.cpp:59-60,259`), refractory lockout is fixed at 25 ms (`render.cpp:17,210-215`), and peak timestamp is assigned at window finalisation (`render.cpp:233-246`) | Fast articulations can be suppressed; queue overflow can silently drop hits; onset timestamp is pragmatic rather than analytically ideal. | Be careful with claims of hardware/input robustness; describe this as prototype-level sensing suitable for bounded tests. | Chapter 4 limitations; Chapter 5 if empirically evaluated. |
| Closed-loop return should ideally be driven by confirmed playback completion from audio layer | `TurnLoopController` estimates end time from generated `PatternTurn` duration plus padding (`TurnLoopController.cs:507-590`) | Unity re-arms on a timer, not a ChucK callback. | Claim closed-loop orchestration with timer-estimated completion, not audio-confirmed completion. | Chapter 4 explicitly; Chapter 5 timing discussion; future work. |

## 5. Candidate Chapter 4 Structures

### Option A: Runtime Pipeline Structure

Structure:
- `4.1 Implementation Overview`
  Purpose: one-page map from Chapter 3 design to implemented runtime path, with live/prototype/diagnostic scope table.
- `4.2 Bela Input and OSC Capture`
  Purpose: show how physical hits are detected, encoded, and received.
- `4.3 Unity Turn Orchestration`
  Purpose: explain turn start, capture windowing, controller phases, and hand-off between stages.
- `4.4 Representation and Quantisation`
  Purpose: explain `TurnWindow -> PatternTurn`.
- `4.5 Analysis and Planning Implementation`
  Purpose: explain implemented analysers, planner, and plan object.
- `4.6 Response Generation and Output Coupling`
  Purpose: explain skeleton generation and the current plan/skeleton vs audible-output split.
- `4.7 Playback and Loop Closure`
  Purpose: explain ChucK bridge, playback scheduling, and timer-based return to waiting.
- `4.8 Observability, Testing and Diagnostics`
  Purpose: explain debug UI, tests, editor runners, and exported evidence.
- `4.9 Implementation Limitations`
  Purpose: consolidate fixed-grid, turn-local, single-pad, and callback/fidelity limits.

Strengths:
- Follows actual execution order.
- Keeps Bela visible as the first real subsystem.
- Easy for markers to understand as a concrete system.

Risks:
- Can slip into a class-by-class walkthrough if each stage is treated too literally.
- Analysis/planning/generation split may look cleaner than it really is unless the coupling problem is stated bluntly.

Fit:
- Runtime order.

### Option B: Chapter 3 Mirror Structure

Structure:
- `4.1 Implementation Framing and System Composition`
- `4.2 Implementing the Interaction Model and Temporal Structure`
- `4.3 Implementing Symbolic Representation`
- `4.4 Implementing Interpretable Analysis and Planning`
- `4.5 Implementing Structural Generation and Current Output Coupling`
- `4.6 Playback, Observability, and Supporting Infrastructure`
- `4.7 Implementation Limitations and Deviations`

Section purposes:
- Each section follows a Chapter 3 design claim and shows what code/hardware actually realises it.

Strengths:
- Tight conceptual linkage back to Chapter 3.
- Reduces risk of repeating design justification if kept disciplined.

Risks:
- Too easy to repeat Chapter 3 headings with only slightly more detail.
- Bela hardware can get buried inside a generic "temporal structure" section.
- Runtime flow becomes less legible.

Fit:
- Mirrors Chapter 3.

### Option C: Hybrid Structure

Structure:
- `4.1 Implementation Overview and Scope Map`
  Purpose: establish live/prototype/diagnostic/deferred boundaries early.
- `4.2 Bela Input, OSC Transport, and Unity Capture`
  Purpose: keep the physical front end and transport together as one implemented layer.
- `4.3 Turn Orchestration and Temporal Segmentation`
  Purpose: explain how runtime phases and fixed turn windows actually operate.
- `4.4 Symbolic Representation and Quantisation`
  Purpose: explain the main computational data contract.
- `4.5 Analysis, Planning, and Prototype Structural Generation`
  Purpose: group the reasoning stack together because it shares one symbolic contract.
- `4.6 Current Audible Response Path and Output Coupling`
  Purpose: isolate the exact divergence between reasoning architecture and live sound output.
- `4.7 Playback, Loop Closure, and Runtime Integration`
  Purpose: explain ChucK handoff and repeated-loop re-arming.
- `4.8 Observability, Testing, and Diagnostic Infrastructure`
  Purpose: show technical achievement and evidence infrastructure without moving into evaluation results.
- `4.9 Implementation Limitations and Deviations`
  Purpose: collect scope boundaries once instead of scattering them.

Strengths:
- Preserves runtime clarity without duplicating Chapter 3 section-for-section.
- Gives the plan/skeleton vs audible-output divergence its own dedicated section.
- Makes Bela a first-class implementation layer.

Risks:
- Needs discipline so `4.5` does not become a long internals dump.
- Needs clear section boundaries to avoid leaking Chapter 5 evaluation results into `4.8` and `4.9`.

Fit:
- Hybrid: runtime order with a deliberate design-boundary section at the analysis/planning/output break.

## 6. Recommended Structure From Codex

Recommendation:
- Option C: Hybrid Structure.

Reason:
- It is the clearest way to avoid repeating Chapter 3 while still showing real technical achievement.
- It keeps Bela visibly inside the implemented system rather than hiding it in a generic input subsection.
- It gives the most important architectural truth its own space: the live planner/skeleton stack exists, but the current audible output still comes from an interim transformation path.
- It aligns best with marking pressure for clear architecture, justified engineering choices, observability, and honest limitation framing.
- It stays easier to keep concise under the word limit than a pure runtime walkthrough or a strict Chapter 3 mirror.

How Chapter 4 should respond to the current report claims:
- Substantiate the draft's claims about implemented capture, representation, analysis, planning, playback, and loop closure with concrete subsystem evidence.
- Explicitly support the draft's honesty about incomplete response realisation rather than softening it.
- Preserve the draft's closed-loop claim, but state that loop closure is timer-estimated.

How Chapter 4 should respond to marking guidance:
- Make technical achievement visible through the implemented subsystem chain, not through long prose about intentions.
- Show engineering decisions with practical consequences: why sample timestamps, why fixed turn windows, why symbolic arrays, why timer-estimated closure.
- Use observability/testing as evidence of engineering maturity, but keep behavioural results and value judgments for Chapter 5.
- Be concise. Padding and low-signal code narration hurt more than they help.

## 7. Content Allocation Rules

Allocation below is for the recommended Option C structure.

| Section | Main text content | Diagram / table suggestion | Reserve for Chapter 5 | Move to appendix | Do not include |
| --- | --- | --- | --- | --- | --- |
| `4.1 Implementation Overview and Scope Map` | One end-to-end paragraph, implemented/live/prototype/deferred scope table, one sentence on Chapter 3 -> Chapter 4 handoff. | Runtime-chain table plus scope matrix. | Any claim about how well the system performs. | Full dependency inventory. | Re-arguing design principles from Chapter 3. |
| `4.2 Bela Input, OSC Transport, and Unity Capture` | Piezo channel, hit detection stages, `/it4/hit` contract, timestamp reconstruction, hit buffering, alternate input rigs only as support tools. | Hardware-to-OSC-to-Unity pipeline figure. | Reliability metrics or listening claims. | Raw threshold tables, IP details, extOSC screenshots. | A full DSP tutorial. |
| `4.3 Turn Orchestration and Temporal Segmentation` | `TurnLoopController` phases, hit gating, fixed-duration turn capture, sample-time end scheduling, `TurnWindow` creation. | State-and-timeline figure showing trigger hit to bounded window. | Transition test results, runtime traces, timing benchmarks. | Large event logs and exhaustive controller traces. | Line-by-line controller walkthrough. |
| `4.4 Symbolic Representation and Quantisation` | `PatternTurn` fields, fixed grid, max-velocity collision rule, microtiming offsets, one compact raw-to-grid example. | Annotated `TurnWindow -> PatternTurn` example table. | Claims about musical adequacy or perceptual naturalness. | Full field dump / code listing. | Repeating input-capture detail. |
| `4.5 Analysis, Planning, and Prototype Structural Generation` | What each analysis family contributes, what `ResponsePlan` contains, how `SkeletonBuilder` works at a high level, and that all of this runs live. | `PatternTurn -> TurnAnalysisResult -> ResponsePlan -> SkeletonPattern` contract figure. | Planner distributions, decision margins, skeleton divergence metrics. | Full threshold tables, score formulas, deep builder helper breakdowns. | Claiming that these outputs already determine the sound. |
| `4.6 Current Audible Response Path and Output Coupling` | Exact divergence point in `AiResponsePreparationFlow`, current `FeatureTransformer` modes, boundary between reasoning and sound. | Current-vs-intended generation diagram. | Plan-to-output fidelity analysis and quantitative error tables. | Transform pseudocode or extra case dumps. | Any sentence implying the skeleton is what is played. |
| `4.7 Playback, Loop Closure, and Runtime Integration` | `IT4ChuckTurnPlayer`, ChucK scheduling, readiness polling, timer-based return to waiting. | Unity -> ChucK -> return-to-waiting sequence diagram. | Timing/latency measurements and critique of closure accuracy. | Full `.ck` listing and Chunity setup details. | Broad discussion of interactive music timing theory. |
| `4.8 Observability, Testing, and Diagnostic Infrastructure` | Debug panels, exported PNGs, edit-mode test families, editor-only batch runners, generated CSV/JSON/MD artefacts. | Tooling/evidence table and one debug UI screenshot. | Test outcomes, planner charts, timing tables, structural metrics. | Extra screenshots, full test catalogues, report schemas. | Copy-pasted logs or long test code excerpts. |
| `4.9 Implementation Limitations and Deviations` | Single-pad live input, fixed tempo/grid, turn-local memory, decoupled audible path, timer-estimated closure, hardware/input caveats. | Implemented-vs-intended summary table. | Extended critical interpretation of consequences. | Larger future-work backlog. | Generic apologies or vague "could be improved" filler. |

Bottom line:
- Chapter 4 should read as an evidence-backed implementation chapter for a closed-loop rhythmic interaction prototype.
- It should be explicit that the reasoning architecture is ahead of the audible realisation layer.
- It should make the physical Bela front end, the Unity orchestration core, and the diagnostics/testing infrastructure all visible as real technical work.
