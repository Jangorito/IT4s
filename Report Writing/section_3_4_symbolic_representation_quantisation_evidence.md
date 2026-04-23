# Section 3.4 - Symbolic Representation and Quantisation

Evidence extraction brief for writing, not polished dissertation prose.

Scope guard:
- Keep this section about representation and quantisation only.
- Do not re-explain the phase loop, turn-window segmentation, Bela/Unity/ChucK timing architecture, or FSM.
- Use the evidence below to answer: how is captured performance transformed into a symbolic representation suitable for computational reasoning and response generation?

---

## 1. Core Implementation Objects

### 1.1 `HitEvent` as pre-symbolic event input

Evidence:
- `GameEngine/Unity/My project/Assets/Scripts/Data/HitEvent.cs:9-23`
- Fields: `tSamples`, `pad`, `velocity`.
- Represents a detected drum hit as a timestamped event, not as audio.

Use for Section 3.4:
- `PatternCompiler` receives hits through a `TurnWindow`, then uses only `h.tSamples` and `h.velocity` during compilation.
- `h.pad` is present at event level but is not carried into `PatternTurn`.
- This is useful negative evidence: the symbolic rhythm representation becomes a single-stream onset/velocity grid.

### 1.2 `TurnWindow` as bounded raw event container

Evidence:
- `GameEngine/Unity/My project/Assets/Scripts/Data/TurnWindow.cs:10-18`
- Stores `turnId`, `startSamples`, `endSamples`, and an array of `HitEvent`.
- `Hits` exposes the captured event list; `DurationSamples` is `endSamples - startSamples`.

Use for Section 3.4:
- `TurnWindow` is the input to quantisation.
- It remains in absolute sample time; `PatternCompiler` converts each hit to a relative sample offset from `window.startSamples`.
- Do not dwell on how the window is created; use only as the raw event source for symbolic conversion.

---

## 2. `PatternTurn` Representation

Primary evidence:
- `GameEngine/Unity/My project/Assets/Scripts/Data/PatternTurn.cs:5-28`
- Class summary: "Quantised representation of a captured rhythm".

### 2.1 Field map

| Field | Implementation evidence | Meaning | Downstream use |
|---|---|---|---|
| `turnId` | `PatternTurn.cs:11`; assigned from `window.turnId` in `PatternCompiler.cs:65` | Identity of the compiled turn | Used in logs/debug messages, generated response tracing, and runtime state. |
| `bpm` | `PatternTurn.cs:14`; copied from `QuantisationSettings` in `PatternCompiler.cs:66` | Tempo basis used to interpret the grid | ChucK bridge recomputes `samplesPerStep` from `turn.bpm` in `IT4ChuckTurnPlayer.cs:123-126`; debug renderer prints it in `PatternTurnDebugRenderer.cs:111-115`. |
| `stepsPerQuarter` | `PatternTurn.cs:15`; copied in `PatternCompiler.cs:67` | Grid resolution: number of symbolic steps per quarter note | Default value is 12 in `TurnLoopBootstrap.cs:36-41`; ChucK bridge sends it to playback in `IT4ChuckTurnPlayer.cs:121-145`; skeleton generation uses it for metrical salience in `SkeletonMetricMapBuilder.cs:32-90`. |
| `sampleRate` | `PatternTurn.cs:17`; copied in `PatternCompiler.cs:68` | Sample-clock rate used for sample-to-grid and grid-to-sample conversion | ChucK bridge validates it and converts seconds per step to samples in `IT4ChuckTurnPlayer.cs:115-126`; debug renderer uses it for offset-marker scaling in `PatternTurnDebugRenderer.cs:261-270`. |
| `startSamples` | `PatternTurn.cs:20`; copied from `window.startSamples` in `PatternCompiler.cs:69` | Absolute source-window start in sample time | Preserves traceability to raw capture; debug duration uses `endSamples - startSamples` in `PatternTurnDebugRenderer.cs:111-115`; transformers clone it in `FeatureTransformer.cs:255-264`. |
| `endSamples` | `PatternTurn.cs:21`; copied from `window.endSamples` in `PatternCompiler.cs:70` | Absolute source-window end in sample time | Same traceability/duration role as `startSamples`; not used by most symbolic analysers. |
| `velocity[]` | `PatternTurn.cs:24`; allocated in `PatternCompiler.cs:29`; populated in `PatternCompiler.cs:55-60` | Main symbolic grid: one integer velocity per step; `0` means no hit, positive values mean active hit | Central input to density, energy, anchor, end-activity, transformation, skeleton source maps, playback, and debug rendering. |
| `offsetSamples[]` | `PatternTurn.cs:25`; allocated in `PatternCompiler.cs:30`; populated in `PatternCompiler.cs:49-60` | Per-step microtiming displacement, in samples, from the quantised grid position | Used by ChucK playback to reconstruct event time in `IT4_TurnPlayer.ck:115-121`; visualised by `PatternTurnDebugRenderer.cs:188-221`; cloned/zeroed by `FeatureTransformer.cs:181-185`, `FeatureTransformer.cs:208-214`, `FeatureTransformer.cs:238-242`, `FeatureTransformer.cs:255-275`. |
| `StepCount` | `PatternTurn.cs:27` | Length of `velocity[]`; the symbolic turn length in grid steps | Used throughout analysis and generation to loop over symbolic time. |

### 2.2 Central interface role

Evidence:
- Compilation creates `PatternTurn`: `TurnLoopController.cs:843-858`.
- Runtime state stores compiled human and generated AI `PatternTurn` objects: `TurnLoopRuntimeState.cs:23-36`, `TurnLoopController.cs:1003-1048`.
- Analysis consumes `PatternTurn`: `TurnAnalyser.cs:30-47`.
- Response preparation passes compiled `PatternTurn` through analysis, skeleton request creation, and transformation: `AiResponsePreparationFlow.cs:48-119`.
- Playback consumes `PatternTurn`: `IT4ChuckTurnPlayer.cs:79-159`.
- Debug rendering consumes `PatternTurn`: `PatternTurnDebugRenderer.cs:51-54`, `PatternTurnDebugRenderer.cs:102-115`.

Interpretation:
- `PatternTurn` is the central symbolic interface between captured performance and later computational reasoning.
- It is not just a storage object: its grid shape determines what analysis, planning, generation, playback, and debugging can inspect.

---

## 3. Quantisation Process

Primary evidence:
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/PatternCompiler.cs:6-75`
- Class summary: compiles a raw `TurnWindow` into a "fixed-BPM quantised pattern".

### 3.1 Grid construction

Code evidence:
- `secPerQuarter = 60.0 / q.bpm`: `PatternCompiler.cs:15-16`
- `secPerStep = secPerQuarter / q.stepsPerQuarter`: `PatternCompiler.cs:18-19`
- `samplesPerStep = secPerStep * q.sampleRate`: `PatternCompiler.cs:21-22`
- `stepCount = Ceil(windowSamples / samplesPerStep)`: `PatternCompiler.cs:24-27`
- minimum step count of 1: `PatternCompiler.cs:27`

Interpretation:
- The quantisation grid is defined by fixed BPM, fixed steps per quarter, and fixed sample rate.
- Step count is derived from the captured window duration, using ceiling so the symbolic array covers the source duration.
- For the default project settings, `TurnLoopBootstrap.cs:36-41` sets `bpm = 120`, `beatsPerBar = 4`, `barsPerTurn = 2`, `stepsPerQuarter = 12`, `sampleRate = 44100`.
- Synthetic diagnostics use `StepCount = 96`, `StepsPerQuarter = 12`, `Bpm = 120`, `SampleRate = 44100` in `SyntheticPatternTurnCases.cs:140-167`, consistent with two bars of 4/4 at 12 steps per quarter.

### 3.2 Mapping raw hits to steps

Code evidence:
- For each hit: `foreach (var h in window.Hits)`: `PatternCompiler.cs:36`
- Relative sample position: `relSamples = h.tSamples - window.startSamples`: `PatternCompiler.cs:38-40`
- Negative relative samples are skipped: `PatternCompiler.cs:40`
- Step index: `step = (int)Math.Round(relSamples / samplesPerStep)`: `PatternCompiler.cs:42-43`
- Out-of-range steps are skipped: `PatternCompiler.cs:44`

Interpretation:
- Raw sample timestamps become integer grid indices.
- The compiler uses nearest-step rounding, not flooring.
- No explicit `MidpointRounding` mode is supplied, so exact half-step cases use the platform/default .NET rounding behaviour rather than a custom musical rule.
- Hits that round outside the allocated array are dropped.

### 3.3 Velocity aggregation and collision handling

Code evidence:
- `velocity` initialized as one `int[stepCount]`: `PatternCompiler.cs:29`
- Collision rule comment: `// collision rule: max velocity wins`: `PatternCompiler.cs:55`
- Replacement only if `h.velocity > velocity[step]`: `PatternCompiler.cs:56-60`

Interpretation:
- Each symbolic step can represent at most one hit.
- If multiple hits map to the same step, the loudest hit wins.
- The stored `offsetSamples[step]` belongs to the winning hit only.
- Equal-velocity collisions do not replace the existing value because the comparison is strictly `>`.
- There is no sum, average, count, ordered list, flam model, or multi-hit representation per step.

### 3.4 Quantisation output

Code evidence:
- New `PatternTurn` is returned with timing basis, window grounding, velocity grid, and offset grid: `PatternCompiler.cs:63-73`.

Interpretation:
- The compiler converts `TurnWindow` from event list form into two aligned arrays.
- This is the key representational shift: performance becomes step-indexed symbolic data suitable for deterministic feature extraction.

---

## 4. Microtiming Representation

Primary evidence:
- `PatternCompiler.cs:46-52`
- `IT4_TurnPlayer.ck:100-121`
- `PatternTurnDebugRenderer.cs:188-221`

### 4.1 How offsets are computed

Code evidence:
- Grid point for selected step: `stepCenter = Round(step * samplesPerStep)`: `PatternCompiler.cs:46-47`
- Offset from quantised grid point: `off = relSamples - stepCenter`: `PatternCompiler.cs:49-50`
- Clamp range: `maxOff = Round(samplesPerStep / 2.0)`: `PatternCompiler.cs:51`
- Clamp to `[-maxOff, maxOff]`: `PatternCompiler.cs:52`

Interpretation:
- `velocity[step]` records that a hit belongs to a quantised step.
- `offsetSamples[step]` records how far the original sample timestamp deviated from that step.
- Offsets are stored in samples, not milliseconds or fractions of a beat.
- Microtiming is preserved only for the one retained hit per occupied step.

### 4.2 How offsets are used

Playback evidence:
- Unity sends `velocity` and `offsetSamples` arrays to ChucK: `IT4ChuckTurnPlayer.cs:128-145`.
- ChucK reconstructs event time as `i * samplesPerStep + offsetSamples[i]`: `IT4_TurnPlayer.ck:115-118`.
- Negative reconstructed times are clamped to zero: `IT4_TurnPlayer.ck:117-118`.
- ChucK sorts events by reconstructed time because offsets can reorder events: `IT4_TurnPlayer.ck:100-139`.
- ChucK schedules by sample delay: `IT4_TurnPlayer.ck:149-162`.

Debug evidence:
- Offset markers are scaled relative to half a step and drawn inside the active cell: `PatternTurnDebugRenderer.cs:188-221`.

Transformation evidence:
- `FeatureTransformer` requires both `velocity` and `offsetSamples`: `FeatureTransformer.cs:71-75`.
- It clones offsets for copied source material: `FeatureTransformer.cs:255-275`.
- New generated echo/fill/ornament hits receive `offsetSamples[...] = 0`: `FeatureTransformer.cs:181-185`, `FeatureTransformer.cs:208-214`, `FeatureTransformer.cs:238-242`.

Analysis evidence:
- `DensityAnalyser`, `EnergyAnalyser`, `AnchorAnalyser`, and `EndActivityAnalyser` use `pattern.velocity` and `pattern.StepCount`, but do not use `offsetSamples`.
- Examples: `DensityAnalyser.cs:18-27`, `EnergyAnalyser.cs:27-45`, `AnchorAnalyser.cs:36-47`, `EndActivityAnalyser.cs:14-45`.

Interpretation:
- Microtiming is preserved mainly for playback reconstruction and visual inspection.
- Most computational reasoning currently treats a hit's rounded grid step as its musical position.
- This means expressive timing can survive into sound output, but it does not strongly condition analysis or planning in the current implementation.

---

## 5. Design Decisions and Rationale Evidence

### 5.1 Fixed grid

Evidence:
- `PatternCompiler` explicitly says it compiles into a "fixed-BPM quantised pattern": `PatternCompiler.cs:6-8`.
- `QuantisationSettings` stores fixed `bpm`, `stepsPerQuarter`, and `sampleRate`: `QuantisisationSettings.cs:3-14`.
- `MusicalTimingConfig.ToQuantisationSettings()` passes those values directly into quantisation: `MusicalTimingConfig.cs:80-83`.
- Turn-analysis architecture notes state that analysis operates on quantised step-grid data and should be deterministic/lightweight/modular: `Misc/TurnAnalysis/TurnAnalysis_Codex_Treatment_UPDATED_v5.md:3-13`.

Rationale to use:
- The fixed grid makes later feature extraction simple, deterministic, and inspectable.
- It turns variable sample-time events into regular array positions that can be counted, scored, segmented, and transformed.

### 5.2 Why `stepsPerQuarter = 12`

Evidence:
- Scene default: `stepsPerQuarter = 12`: `TurnLoopBootstrap.cs:36-41`.
- `PatternTurn` comment: "`stepsPerQuarter` - e.g., 12 for flexible grid": `PatternTurn.cs:13-17`.
- `QuantisationSettings` comment: "`stepsPerQuarter` - e.g. 12 (flexible grid)": `QuantisisationSettings.cs:5-7`.
- Skeleton Builder spec: current project context uses 12 steps per quarter: `skeleton_builder_spec_unified.md:153-158`.
- Skeleton Builder spec says metric salience should align with the 12-step-per-quarter resolution: `skeleton_builder_spec_unified.md:522-530`.
- Skeleton metric map derives strong/medium/weak metrical positions by modulo relationships over `stepsPerQuarter`: `SkeletonMetricMapBuilder.cs:55-90`.

Rationale to use carefully:
- Explicit rationale in code is limited to "flexible grid".
- Implementation evidence supports an inference that 12 SPQ was selected as a practical resolution for a hierarchical rhythmic grid: it supports quarter positions, half-quarter subdivisions, and quarter-step subdivisions through deterministic modulo logic.
- Because 12 is divisible by 2, 3, 4, and 6, it can accommodate common binary and triplet-oriented subdivisions more flexibly than a very coarse grid.
- Do not overclaim that the code formally proves a music-theory rationale; present this as an implementation-supported inference.

### 5.3 Symbolic arrays instead of event lists

Evidence:
- `PatternCompiler` returns `int[] velocity` and `int[] offsetSamples`: `PatternCompiler.cs:29-30`, `PatternCompiler.cs:63-73`.
- `DensityAnalyser` counts active array slots: `DensityAnalyser.cs:18-27`.
- `EnergyAnalyser` sums and averages velocity values over active slots: `EnergyAnalyser.cs:27-45`.
- `AnchorAnalyser` computes per-step salience using velocity, local context, metrical position, and phrase role: `AnchorAnalyser.cs:36-47`.
- `SkeletonSourceMapBuilder` converts source velocity into a boolean occupied-step map: `SkeletonSourceMapBuilder.cs:64-83`.
- ChucK bridge sends arrays rather than an event object list: `IT4ChuckTurnPlayer.cs:128-145`.

Rationale to use:
- Arrays make computational reasoning cheap and transparent.
- A step-indexed grid lets each module interpret the same temporal structure consistently.
- `velocity[]` acts as both onset map and dynamic/accent information.

### 5.4 Separate `velocity[]` and `offsetSamples[]`

Evidence:
- Separate fields in `PatternTurn`: `PatternTurn.cs:23-25`.
- Compiler writes velocity and offset separately: `PatternCompiler.cs:55-60`.
- Analysis contracts emphasize `velocity[]` and `StepCount`: `TurnAnalysis_Codex_Treatment_UPDATED_v5.md:16-30`.
- Playback recombines step index and offset only when scheduling sound: `IT4_TurnPlayer.ck:115-121`.
- Debug renderer visualises offset as a marker within a step cell: `PatternTurnDebugRenderer.cs:188-221`.

Rationale to use:
- Separation keeps the main symbolic grid stable for reasoning while preserving timing deviation as a secondary layer.
- This allows density, energy, anchors, and source-occupancy maps to operate on discrete steps without destroying the possibility of microtimed playback.
- It also makes current limitations explicit: offsets exist, but most reasoning modules ignore them.

---

## 6. Downstream Uses of the Symbolic Representation

### 6.1 Analysis

Evidence:
- `TurnAnalyser.Analyze(PatternTurn pattern)` calls density, energy, anchor, end-activity, and segment-activity-profile analysers: `TurnAnalyser.cs:30-47`.
- Density treats `velocity[i] > 0` as active occupancy: `DensityAnalyser.cs:18-27`.
- Energy derives mean, peak, variance, and segment means from velocities: `EnergyAnalyser.cs:27-81`, `EnergyAnalyser.cs:84-139`.
- Anchor detection scores active steps using velocity, local accent, isolation, metrical placement, and phrase role: `AnchorAnalyser.cs:36-77`.
- End activity inspects the final 25 percent of grid steps: `EndActivityAnalyser.cs:14-50`.
- Segment activity profile classifies shapes using density and energy segment values: `SegmentActivityProfileAnalyser.cs:17-30`, `SegmentActivityProfileAnalyser.cs:32-82`.

Interpretation:
- Symbolic conversion is what makes these analyses possible.
- The analysers do not need raw timestamps or audio; they reason over grid occupancy and velocity.

### 6.2 Planning

Evidence:
- `ResponsePlanner.Plan` consumes `TurnAnalysisResult`, not raw audio or raw hit lists: `ResponsePlanner.cs:27-38`.
- Planning context includes source density, source energy, anchor count, ending descriptors, profile shape flags, and turn length steps: `PlanningContext.cs:5-63`.
- `BuildContext` normalizes and combines analysis-derived values: `ResponsePlanner.cs:49-107`.

Interpretation:
- Planning is one step removed from `PatternTurn`: it operates on features extracted from the symbolic grid.
- This reinforces that quantisation is not an endpoint; it is the interface that enables feature extraction for response decisions.

### 6.3 Response generation and transformation

Evidence:
- `AiResponsePreparationFlow.Prepare` requires a compiled `PatternTurn`: `AiResponsePreparationFlow.cs:48-59`.
- It analyses the pattern, plans a response, optionally builds a skeleton, and transforms the compiled pattern: `AiResponsePreparationFlow.cs:67-119`.
- Skeleton request includes `SourceTurn`, `SourceAnalysis`, `TurnLengthSteps`, and `StepsPerQuarter`: `AiResponsePreparationFlow.cs:122-137`, `SkeletonBuildRequest.cs:7-15`.
- `SkeletonBuilder` builds derived maps from source turn, source analysis, turn length, and steps per quarter: `SkeletonBuilder.cs:28-45`, `SkeletonDerivedMaps.cs:73-115`.
- `SkeletonBuilder` validates that source `velocity` exists and source grid matches request `StepsPerQuarter`: `SkeletonBuilder.cs:2378-2413`.
- `FeatureTransformer` analyses `PatternTurn` into active step count, density, max velocity, mean velocity, onset indices, and gaps: `FeatureTransformer.cs:25-38`, `FeatureTransformer.cs:66-144`.
- `FeatureTransformer` then writes new response material into `velocity[]` and `offsetSamples[]`: `FeatureTransformer.cs:166-246`.

Interpretation:
- Response generation depends on the grid abstraction: source occupancy, gaps, metric position, ending region, and anchors are all step-indexed.
- Current live generation still transforms the compiled human pattern directly; the skeleton path exists as a scaffold-oriented forward path.

### 6.4 Playback

Evidence:
- Unity validates `velocity`, `offsetSamples`, `bpm`, and `sampleRate` before playback: `IT4ChuckTurnPlayer.cs:87-126`.
- Unity computes `samplesPerStep` from `bpm`, `stepsPerQuarter`, and `sampleRate`: `IT4ChuckTurnPlayer.cs:121-126`.
- Unity pushes `stepCount`, `samplesPerStep`, `stepsPerQuarter`, `velocity[]`, and `offsetSamples[]` into ChucK: `IT4ChuckTurnPlayer.cs:139-145`.
- ChucK schedules only steps with `velocity[i] > 0`: `IT4_TurnPlayer.ck:110-121`.
- ChucK reconstructs sample event times from grid step plus offset: `IT4_TurnPlayer.ck:115-121`.

Interpretation:
- Playback is symbolic-to-audio rendering.
- The audio layer does not receive a waveform or high-level musical phrase object; it receives grid arrays.

### 6.5 Debugging and inspection

Evidence:
- `PatternTurnDebugRenderer` shows BPM, steps per quarter, total steps, and duration: `PatternTurnDebugRenderer.cs:111-115`.
- It draws step numbers, active cells, velocity values, and offset markers: `PatternTurnDebugRenderer.cs:124-241`.

Interpretation:
- The representation is intentionally inspectable: users/developers can see the symbolic grid and its microtiming deviations.

---

## 7. Trade-offs and Limitations

Each item below includes implementation evidence plus impact.

### 7.1 Quantisation vs expressive timing

Evidence:
- Hits are rounded to integer steps: `PatternCompiler.cs:42-44`.
- Original timing deviation is stored separately as `offsetSamples`: `PatternCompiler.cs:46-52`.
- Analysis modules use `velocity[]` but not `offsetSamples`: `DensityAnalyser.cs:18-27`, `EnergyAnalyser.cs:27-45`, `AnchorAnalyser.cs:36-47`.

Impact:
- Expressive timing is not completely discarded, because playback can use offsets.
- However, analysis and planning mostly reason about the rounded step, so laid-back/ahead-of-beat feel does not currently affect density, energy, anchor salience, or response type.

### 7.2 Fixed grid vs tempo flexibility

Evidence:
- `QuantisationSettings` stores fixed `bpm`, `stepsPerQuarter`, and `sampleRate`: `QuantisisationSettings.cs:3-14`.
- `PatternCompiler` computes one constant `samplesPerStep` for the whole turn: `PatternCompiler.cs:15-23`.
- `MusicalTimingConfig.ToQuantisationSettings()` passes fixed config values into quantisation: `MusicalTimingConfig.cs:80-83`.

Impact:
- The symbolic representation is stable and easy to compare across modules.
- It does not adapt to tempo drift, performer rubato, or intra-turn tempo changes; such variation becomes offsets or quantisation error.

### 7.3 Twelve-step grid flexibility vs unsupported subdivisions

Evidence:
- Default `stepsPerQuarter = 12`: `TurnLoopBootstrap.cs:36-41`.
- Comments describe 12 as a flexible grid: `PatternTurn.cs:15`, `QuantisisationSettings.cs:6`.
- Skeleton metric hierarchy uses divisions of `stepsPerQuarter` by 2 and 4: `SkeletonMetricMapBuilder.cs:55-90`.

Impact:
- 12 SPQ supports useful binary and triplet-oriented subdivision in one grid.
- Rhythms outside that resolution, such as finer tuplets or very dense continuous rolls, must be approximated, collided, or dropped.

### 7.4 Symbolic abstraction vs audio richness

Evidence:
- `PatternTurn` stores timing basis, sample bounds, `velocity[]`, and `offsetSamples[]`, but no waveform, spectrum, envelope, or timbre fields: `PatternTurn.cs:11-27`.
- ChucK playback receives arrays and triggers a snare sample scaled by velocity: `IT4_TurnPlayer.ck:49-57`, `IT4_TurnPlayer.ck:115-121`.

Impact:
- The representation is efficient for rhythmic reasoning.
- The system cannot reason about stick sound, tone colour, decay shape, buzz/roll texture, or audio-level nuance beyond velocity and timing.

### 7.5 Collision handling vs polyphonic or flam nuance

Evidence:
- One velocity slot per step: `PatternCompiler.cs:29`.
- Collision rule is max velocity wins: `PatternCompiler.cs:55-60`.
- Equal or softer later hits in the same step do not change the stored value.

Impact:
- The grid remains simple.
- Multiple close hits, flams, or dense ornaments inside one quantisation cell collapse into one event; event count and ordering inside that cell are lost.

### 7.6 Single-stream grid vs multi-pad representation

Evidence:
- `HitEvent` includes `pad`: `HitEvent.cs:11-13`.
- `PatternCompiler` uses `h.tSamples` and `h.velocity`, but does not use `h.pad`: `PatternCompiler.cs:36-60`.
- `PatternTurn` has no pad, instrument, or voice dimension: `PatternTurn.cs:11-27`.

Impact:
- The compiled representation treats the turn as one rhythmic stream.
- If multiple pads/instruments are captured upstream, their distinct identities are not represented in `PatternTurn`; response logic cannot distinguish snare/tom/cymbal roles from this object alone.

### 7.7 Discrete steps vs continuous time

Evidence:
- `StepCount` is the length of the velocity array: `PatternTurn.cs:27`.
- All major analysis loops iterate integer steps: `DensityAnalyser.cs:18-22`, `EnergyAnalyser.cs:27-38`, `AnchorAnalyser.cs:36-47`.
- Offsets are clamped to at most half a step: `PatternCompiler.cs:51-52`.

Impact:
- Computational reasoning becomes regular and bounded.
- Continuous timing is reduced to step index plus one bounded residual; no continuous timing curve or inter-onset timing sequence is retained.

### 7.8 Rounding and boundary loss

Evidence:
- Step index is rounded to nearest: `PatternCompiler.cs:42-43`.
- If rounded step is outside `[0, stepCount)`, the hit is skipped: `PatternCompiler.cs:44`.

Impact:
- Hits close to a grid point beyond the allocated final step can be ignored after rounding.
- This is deterministic, but it can remove late-window expressive events from the symbolic pattern.

### 7.9 Analysis tractability vs microtiming-aware listening

Evidence:
- Turn-analysis input contract requires `velocity[]` and `StepCount`: `TurnAnalysis_Codex_Treatment_UPDATED_v5.md:16-30`.
- Current analysis code ignores `offsetSamples`.
- Debug and playback use offsets: `PatternTurnDebugRenderer.cs:188-221`, `IT4_TurnPlayer.ck:115-121`.

Impact:
- The system can display and replay timing deviations but does not interpret them as groove, swing, rush/drag, looseness, or feel.
- Any dissertation claim about expressivity should distinguish "preserved for playback" from "analysed for response planning".

### 7.10 Generated hits tend toward grid alignment

Evidence:
- `FeatureTransformer` clones existing offsets when cloning a source pattern: `FeatureTransformer.cs:255-275`.
- Newly inserted echo/fill/ornament hits are assigned `offsetSamples[...] = 0`: `FeatureTransformer.cs:181-185`, `FeatureTransformer.cs:208-214`, `FeatureTransformer.cs:238-242`.
- Skeleton patterns select active step positions, not microtimed event placements: `SkeletonBuilder.cs:28-61`.

Impact:
- Existing microtiming can survive copied material.
- New generated material is currently grid-centered unless later modules add offsets, so response timing may sound straighter than the human source.

---

## 8. Negative Evidence: What Is Not Represented

### 8.1 No continuous waveform information

Evidence:
- `HitEvent` is already an event abstraction with timestamp, pad, and velocity: `HitEvent.cs:9-23`.
- `PatternTurn` contains no audio buffer or waveform field: `PatternTurn.cs:11-27`.

Behavioural consequence:
- The system cannot analyse raw audio shape, transient detail, spectral content, or background sound at the `PatternTurn` stage.
- Reasoning begins from symbolic hit events.

### 8.2 No timbral information

Evidence:
- `PatternTurn` has no timbre, sample identity, spectral descriptor, drum type, or articulation label: `PatternTurn.cs:11-27`.
- ChucK playback uses a snare sample and scales gain from velocity: `IT4_TurnPlayer.ck:19-22`, `IT4_TurnPlayer.ck:49-57`.

Behavioural consequence:
- Response decisions cannot be conditioned on tone colour or instrument identity inside `PatternTurn`.

### 8.3 No articulation modelling beyond velocity

Evidence:
- The only dynamic/per-hit expressive value in `PatternTurn` is `velocity[]`: `PatternTurn.cs:23-25`.
- Analysis energy features are computed from velocity values: `EnergyAnalyser.cs:27-81`.

Behavioural consequence:
- Accents and energy can be represented, but stick technique, muting, roll type, duration, and stroke articulation are absent.

### 8.4 No adaptive grid or tempo warping

Evidence:
- One `bpm`, one `stepsPerQuarter`, and one `sampleRate` per `PatternTurn`: `PatternTurn.cs:13-17`.
- One `samplesPerStep` value is computed for the whole compilation: `PatternCompiler.cs:15-23`.

Behavioural consequence:
- The system does not fit a performer-specific tempo curve or warp the grid to expressive timing.
- Timing deviations are residual offsets, not tempo-model updates.

### 8.5 No multi-resolution representation

Evidence:
- `PatternTurn` stores one grid resolution via `stepsPerQuarter` and one pair of arrays: `PatternTurn.cs:13-27`.
- Skeleton metric hierarchy is derived from modulo logic over the same grid: `SkeletonMetricMapBuilder.cs:55-90`.

Behavioural consequence:
- The system can infer stronger and weaker positions within one grid, but it does not store simultaneous coarse/fine grids or hierarchical rhythmic layers as separate representations.

### 8.6 No per-step event multiplicity

Evidence:
- `velocity[]` and `offsetSamples[]` are scalar arrays, not lists per step: `PatternTurn.cs:23-25`.
- Collision rule stores only max velocity: `PatternCompiler.cs:55-60`.

Behavioural consequence:
- The representation cannot distinguish one strong hit from several close hits if they quantise to the same step.

### 8.7 No pad/instrument dimension after quantisation

Evidence:
- `HitEvent` has `pad`: `HitEvent.cs:11-13`.
- `PatternCompiler` does not carry `pad` into `PatternTurn`: `PatternCompiler.cs:36-73`.
- `PatternTurn` has no pad/instrument field: `PatternTurn.cs:11-27`.

Behavioural consequence:
- The compiled representation is monophonic/single-stream at the symbolic level.

---

## 9. Claim Candidates

Use these as arguable claims, not final prose.

1. `PatternTurn` acts as the central symbolic interface between captured performance, analysis, response generation, playback, and debugging.
   - Evidence: `PatternTurn.cs:11-27`; `TurnAnalyser.cs:30-47`; `AiResponsePreparationFlow.cs:48-119`; `IT4ChuckTurnPlayer.cs:79-159`; `PatternTurnDebugRenderer.cs:102-115`.

2. Quantisation imposes discrete musical structure onto continuous sample-time performance events.
   - Evidence: `PatternCompiler.cs:15-27`, `PatternCompiler.cs:36-44`.

3. The system represents rhythmic activity primarily as grid occupancy plus velocity.
   - Evidence: `PatternTurn.cs:23-27`; `DensityAnalyser.cs:18-27`; `EnergyAnalyser.cs:27-45`.

4. Microtiming offsets preserve expressive deviation within a discrete grid framework.
   - Evidence: `PatternCompiler.cs:46-52`; `IT4_TurnPlayer.ck:115-121`; `PatternTurnDebugRenderer.cs:188-221`.

5. The separation of `velocity[]` and `offsetSamples[]` lets analysis remain step-based while playback can recover sub-step timing.
   - Evidence: `PatternTurn.cs:23-25`; `DensityAnalyser.cs:18-27`; `IT4_TurnPlayer.ck:115-121`.

6. The implemented analysis pipeline treats quantised grid position as musically primary and microtiming as secondary.
   - Evidence: `TurnAnalysis_Codex_Treatment_UPDATED_v5.md:16-30`; `DensityAnalyser.cs:18-27`; `AnchorAnalyser.cs:36-47`.

7. `stepsPerQuarter = 12` is an implementation-level compromise between resolution and simplicity.
   - Evidence: `TurnLoopBootstrap.cs:36-41`; `QuantisisationSettings.cs:5-7`; `skeleton_builder_spec_unified.md:153-158`, `skeleton_builder_spec_unified.md:522-530`.

8. Collision handling favours a clear symbolic onset map over preserving all performed events.
   - Evidence: `PatternCompiler.cs:55-60`.

9. The representation is deliberately symbolic rather than audio-rich.
   - Evidence: `PatternTurn.cs:11-27`; `IT4_TurnPlayer.ck:49-57`.

10. Response planning is made possible by transforming the grid into interpretable feature families.
    - Evidence: `TurnAnalyser.cs:30-47`; `ResponsePlanner.cs:49-107`; `PlanningContext.cs:5-63`.

11. Skeleton generation depends on shared step-grid alignment between source turn and response turn.
    - Evidence: `SkeletonBuildRequest.cs:7-15`; `SkeletonSourceMapBuilder.cs:64-83`; `SkeletonBuilder.cs:2407-2411`.

12. Playback reconstructs performance timing from symbolic arrays rather than receiving raw performance data.
    - Evidence: `IT4ChuckTurnPlayer.cs:128-145`; `IT4_TurnPlayer.ck:100-162`.

13. The current system preserves microtiming better than it understands microtiming.
    - Evidence: offsets are computed and played back in `PatternCompiler.cs:46-52` and `IT4_TurnPlayer.ck:115-121`, but ignored by analysis modules such as `DensityAnalyser.cs:18-27` and `EnergyAnalyser.cs:27-45`.

14. The compiled pattern is single-stream despite the earlier hit event containing a pad identifier.
    - Evidence: `HitEvent.cs:11-13`; `PatternCompiler.cs:36-73`; `PatternTurn.cs:11-27`.

15. The representational design trades low-level nuance for transparent, deterministic, inspectable reasoning.
    - Evidence: `TurnAnalysis_Codex_Treatment_UPDATED_v5.md:3-13`; `PatternTurnDebugRenderer.cs:124-241`; `PatternCompiler.cs:29-60`.

---

## 10. High-Value Evidence List

Most important code references:
- `GameEngine/Unity/My project/Assets/Scripts/Data/PatternTurn.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/PatternCompiler.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/QuantisisationSettings.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/MusicalTimingConfig.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/TurnAnalysis/TurnAnalyser.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/TurnAnalysis/Analysers/DensityAnalyser.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/TurnAnalysis/Analysers/EnergyAnalyser.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/TurnAnalysis/Analysers/AnchorAnalyser.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/Transformations/FeatureTransformer.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/Generation/Skeleton/SkeletonSourceMapBuilder.cs`
- `GameEngine/Unity/My project/Assets/Scripts/Rhythm/Generation/Skeleton/SkeletonMetricMapBuilder.cs`
- `GameEngine/Unity/My project/Assets/Scripts/ChunityAudio/IT4ChuckTurnPlayer.cs`
- `GameEngine/Unity/My project/Assets/StreamingAssets/ChucK/IT4_TurnPlayer.ck`
- `GameEngine/Unity/My project/Assets/Scripts/PatternTurnDebugRenderer.cs`

Most important local design/reference notes:
- `Misc/TurnAnalysis/TurnAnalysis_Codex_Treatment_UPDATED_v5.md`
- `Misc/Response/Skeleton Gen/skeleton_builder_spec_unified.md`
