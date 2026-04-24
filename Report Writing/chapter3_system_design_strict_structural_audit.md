# Strict Structural Audit: Chapter 3 - System Design

Source: `Intelli-Trading Fours_ Final Report (3).txt`  
Scope audited: `L417-L630`  
Rule enforced: if a paragraph contains any implementation detail, it is `[IMPLEMENTATION]`.  
Method note: section headings were excluded from classification; standalone flow/state blocks were counted.

## 1. Global Assessment

- Verdict: this is not a clean design chapter.
- Only the opening of `3.1` stays near design level. `3.2` to `3.5` are mostly Chapter 4 material disguised as design.
- Strict classification count: `[DESIGN] 14/57`, `[THEORY] 2/57`, `[IMPLEMENTATION] 41/57`.
- Main structural failure: the chapter is organised as a subsystem walkthrough. That is implementation logic, not design rationale.
- Main drift into implementation: controller/state descriptions, named components, named data structures, hardware/software stack references, timing-domain mechanics, signal-processing steps, quantisation rules, planner parameters, generation logic, and debug tooling.
- Repetition is heavy. The same claims about closed-loop interaction, interpretability, symbolic abstraction, timing precision, and trade-offs are repeated across multiple sections.
- Abstraction is unstable. `3.1` argues what and why. `3.2-3.5` switch to how. The chapter repeatedly collapses into internal documentation.

## 2. Paragraph Classification

### Intro + 3.1

| ID | Lines | Class | Structural judgement |
| --- | --- | --- | --- |
| P01 | `L417-L418` | `[DESIGN]` | Valid chapter framing. |
| P02 | `L424` | `[DESIGN]` | Real-time responsiveness stated as a design constraint. |
| P03 | `L427` | `[DESIGN]` | Turn-based interaction justified at design level. |
| P04 | `L430` | `[DESIGN]` | Reactive-versus-generative framing is valid design positioning. |
| P05 | `L433` | `[DESIGN]` | Interpretability stated as a design principle. |
| P06 | `L438` | `[DESIGN]` | Rationale for interpretability remains design-level. |
| P07 | `L441` | `[DESIGN]` | Symbolic-over-audio choice is a valid design decision. |
| P08 | `L444` | `[DESIGN]` | Human-AI dialogue framing is valid chapter content. |
| P09 | `L447` | `[IMPLEMENTATION]` | Explicit interaction-cycle/pipeline description. |
| P10 | `L450` | `[DESIGN]` | High-level trade-offs belong here. |
| P11 | `L453` | `[DESIGN]` | Acceptable design-level summary. |

### 3.2 Closed-Loop Interaction Architecture

| ID | Lines | Class | Structural judgement |
| --- | --- | --- | --- |
| P12 | `L457` | `[IMPLEMENTATION]` | State-driven loop and control-flow description. |
| P13 | `L460-L466` | `[IMPLEMENTATION]` | Names controller and runtime phases. Pure implementation. |
| P14 | `L469` | `[IMPLEMENTATION]` | Entry/exit conditions and deterministic progression. |
| P15 | `L472` | `[IMPLEMENTATION]` | Standalone execution-flow arrow chain. |
| P16 | `L475` | `[IMPLEMENTATION]` | Runtime transition triggers. |
| P17 | `L478` | `[IMPLEMENTATION]` | State-specific operational behaviour. |
| P18 | `L481` | `[IMPLEMENTATION]` | Full data-flow description with technologies and data structures. |
| P19 | `L484` | `[IMPLEMENTATION]` | Analysis-to-playback pipeline with named internals. |
| P20 | `L487` | `[IMPLEMENTATION]` | Named subsystems/modules. |
| P21 | `L490` | `[IMPLEMENTATION]` | Timing-domain implementation detail. |
| P22 | `L493` | `[IMPLEMENTATION]` | More timing-domain mechanics. |
| P23 | `L496` | `[IMPLEMENTATION]` | Debugging/tooling references. Disallowed in Chapter 3. |
| P24 | `L501` | `[DESIGN]` | Summary paragraph returns to acceptable abstraction. |

### 3.3 Temporal Capture and Turn Segmentation

| ID | Lines | Class | Structural judgement |
| --- | --- | --- | --- |
| P25 | `L509` | `[DESIGN]` | Valid design challenge and rationale. |
| P26 | `L512` | `[IMPLEMENTATION]` | Sensor, hardware, and signal-processing steps. |
| P27 | `L515` | `[IMPLEMENTATION]` | Sample-clock, OSC transport, Unity reconstruction. |
| P28 | `L518` | `[IMPLEMENTATION]` | Trigger logic, fixed-duration window, `TurnWindow`. |
| P29 | `L521` | `[IMPLEMENTATION]` | Concrete segmentation behaviour and edge cases. |
| P30 | `L524-L525` | `[IMPLEMENTATION]` | Cross-system timing-domain detail. |
| P31 | `L528-L529` | `[IMPLEMENTATION]` | `TurnWindow` as computational unit. Still implementation-level. |
| P32 | `L534` | `[IMPLEMENTATION]` | Low-level limitations of detection/tempo handling. |
| P33 | `L537` | `[THEORY]` | Standalone literature explanation. Belongs in Chapter 2. |
| P34 | `L540` | `[IMPLEMENTATION]` | Proposed pipeline extension with named analysis components. |
| P35 | `L543` | `[IMPLEMENTATION]` | Explicitly framed as current implementation. |

### 3.4 Symbolic Representation and Quantisation

| ID | Lines | Class | Structural judgement |
| --- | --- | --- | --- |
| P36 | `L549` | `[IMPLEMENTATION]` | `PatternTurn` object description. |
| P37 | `L552` | `[IMPLEMENTATION]` | Array-level/data-structure detail. |
| P38 | `L555-L556` | `[IMPLEMENTATION]` | Quantisation algorithm and collision rule. |
| P39 | `L559` | `[IMPLEMENTATION]` | Playback reconstruction and representation internals. |
| P40 | `L562` | `[IMPLEMENTATION]` | Downstream module behaviour tied to named representation. |
| P41 | `L565` | `[IMPLEMENTATION]` | Representation-specific trade-offs. Still too concrete. |
| P42 | `L568` | `[DESIGN]` | High-level representational trade-off summary. |

### 3.5 Analysis and Response Generation

| ID | Lines | Class | Structural judgement |
| --- | --- | --- | --- |
| P43 | `L574-L577` | `[DESIGN]` | Valid high-level statement of response goals. |
| P44 | `L580` | `[IMPLEMENTATION]` | Explicit staged pipeline language. |
| P45 | `L586` | `[IMPLEMENTATION]` | System-specific feature set. |
| P46 | `L589` | `[IMPLEMENTATION]` | Named analyser and internal representation logic. |
| P47 | `L595` | `[IMPLEMENTATION]` | Response-type taxonomy and selection logic. |
| P48 | `L598` | `[IMPLEMENTATION]` | `ResponsePlan` and parameter-level detail. |
| P49 | `L601` | `[THEORY]` | Standalone theoretical framing. Belongs in Chapter 2. |
| P50 | `L607` | `[IMPLEMENTATION]` | `SkeletonBuilder` and parameter-level generation logic. |
| P51 | `L610` | `[IMPLEMENTATION]` | Score-and-select algorithm and `SkeletonPattern`. |
| P52 | `L612` | `[IMPLEMENTATION]` | Runtime gap between planning artefacts and output module. |
| P53 | `L615` | `[IMPLEMENTATION]` | Concrete description of hybrid architecture. |
| P54 | `L618` | `[IMPLEMENTATION]` | Future integration path expressed via current modules. |
| P55 | `L624` | `[IMPLEMENTATION]` | Concrete algorithmic limitations and weighting rules. |
| P56 | `L627` | `[IMPLEMENTATION]` | Implementation limitations and missing capabilities. |
| P57 | `L630` | `[IMPLEMENTATION]` | Ends with pipeline restatement and debug-output reference. |

## 3. Leakage Detection

### Pipeline / execution-flow leaks

- `L447`: capture -> analyse -> plan -> realise -> return.
- `L457`: listening -> reasoning -> responding -> re-arming.
- `L469-L475`: finite progression and runtime transition logic.
- `L472`: `Waiting -> Capture -> Compile -> Generate -> Play -> Waiting`.
- `L481-L485`: hardware input -> buffering -> compilation -> analysis -> planning -> scaffold -> playback.
- `L518-L529`: trigger hit -> fixed turn window -> bounded unit -> later symbolic stages.
- `L574-L580`: interpret -> intend -> realise, then explicit staged pipeline statement.
- `L610-L618`: score/select generation -> current output route -> future integration path.
- `L630`: analysis -> planning -> structural scaffolding restated again.

### System-component leaks

- `L460-L478`: `TurnLoopController`, `WaitingForHuman`, `CapturingHuman`, `CompilingHumanTurn`, `GeneratingAiResponse`, `PlayingAiResponse`, `Transition`, `Error`.
- `L481`, `L518`, `L528-L529`, `L549-L562`: `TurnWindow`, `PatternCompiler`, `PatternTurn`.
- `L484`, `L607`, `L610`: `SkeletonBuilder`, `SkeletonPattern`.
- `L487`: `OscHitReceiver`, `HitBuffer`, `ResponsePlanner`.
- `L496`: `TurnLoopDebugPresenter`, renderer classes.
- `L589`: `TurnAnalyser`.
- `L598`, `L612`: `ResponsePlan`, `FeatureTransformer`.
- `L552`, `L559`: `velocity[]`, `offsetSamples[]`.

### Technology-stack leaks

- `L481`, `L512`, `L515`, `L524-L525`: `Bela`.
- `L481`, `L515`: `OSC`.
- `L481`, `L515`, `L524-L525`: `Unity`.
- `L484`, `L490`, `L524-L525`: `ChucK`.
- `L512`: piezoelectric sensor and analogue signal chain.

### Debugging / tooling leaks

- `L496`: debug presenter and renderer classes.
- `L630`: on-screen debug output for the prototype pipeline.

## 4. Repetition Analysis

| Repeated concept | Where it repeats | Audit judgement |
| --- | --- | --- |
| Closed-loop turn cycle | `L447`, `L457`, `L469-L475`, `L501`, `L580`, `L630` | The chapter keeps restating the same loop, then re-explaining it with more machinery. Bloated and structurally sloppy. |
| Interpretability / inspectable intermediates | `L433`, `L438`, `L496`, `L589`, `L615` | The same justification appears as principle, tooling, analyser logic, and architecture commentary. |
| Symbolic abstraction over raw performance | `L441`, `L509`, `L528-L529`, `L549`, `L568` | Same representational idea is introduced, reintroduced, then documented. |
| Real-time timing precision | `L424`, `L490-L493`, `L515`, `L524-L525`, `L543` | Same temporal point repeated first as a design constraint, then as timing-domain internals. |
| Analysis / planning / generation separation | `L433`, `L447`, `L478`, `L481-L485`, `L562`, `L580`, `L598`, `L630` | One idea repeated across four sections with rising implementation detail. |
| Trade-offs / limitations / future work | `L450`, `L521`, `L534`, `L565`, `L618`, `L624`, `L627`, `L630` | Limitations are scattered everywhere instead of being consolidated once. |

### Duplicated explanations by section

- `3.1` already explains the closed-loop idea. `3.2` repeats it and then turns it into a state-machine walkthrough.
- `3.2` explains mixed timing domains. `3.3` repeats the same point with even more transport-level detail.
- `3.1` justifies symbolic representation. `3.3` reopens the same bridge-from-performance argument. `3.4` then documents the representation internals.
- `3.1` argues for interpretability. `3.2` re-argues it through observability. `3.5` re-argues it through feature extraction and planning.

## 5. Abstraction Failures

- `L447`: even the design-principles section starts leaking operational flow.
- `L460-L496`: the chapter drops from design rationale into software architecture specification. This reads like implementation documentation.
- `L512-L515`: sensor chain, filtering, thresholding, peak tracking, sample clocks, transport layers. This is Chapter 4, full stop.
- `L518-L534`: turn triggering, fixed windows, `TurnWindow`, BPM dependency, refractory-period behaviour. Still implementation, not design.
- `L549-L562`: `PatternTurn`, arrays, grid indexing, rounding, collision rules, playback reconstruction. This is data-structure/API documentation.
- `L586-L618`: feature taxonomies, planner artefacts, response categories, scoring logic, module gaps. This is algorithm/module documentation.
- `L496` and `L630`: debug interfaces and prototype pipeline display have no business in a design chapter.
- Overall failure pattern: the chapter does not hold one abstraction level. It oscillates between conceptual motivation and internal mechanics, which wrecks structure.

## 6. Redraft Map

### Keep in Chapter 3

- Real-time responsiveness as the primary design constraint.
- Turn-based exchange as the chosen interaction model.
- Reactive collaborator framing versus autonomous generator framing.
- Interpretability as a governing design principle.
- Symbolic rhythmic representation as a high-level design choice.
- High-level aim that responses should be conditioned on performer input rather than produced in isolation.
- High-level trade-off: expressiveness vs tractability.
- High-level trade-off: deterministic control vs variability.
- High-level trade-off: discrete turn-taking vs fluid co-improvisation.
- One short high-level statement that the system is designed as a closed loop, without enumerating stages, phases, or runtime flow.

### Move to Chapter 4

- Any state-machine description, phase list, or transition logic.
- Any pipeline or execution-flow narrative.
- All named components, classes, modules, services, and data structures.
- All hardware/software stack references.
- Sensor capture, filtering, onset detection, timestamping, and transport mechanics.
- Turn segmentation mechanics and timing-domain handling.
- Quantisation details, grid rules, collision handling, and representation layout.
- Feature set definitions and analyser behaviour.
- Response-type taxonomy and planner parameters.
- Skeleton generation logic and score/selection behaviour.
- Output-realisation gap described through current module arrangement.
- Debug presenters, renderers, and on-screen pipeline displays.

### Move to Chapter 2

- Standalone literature recap on temporal alignment, entrainment, and tempo extraction (`L537`).
- Standalone theoretical framing of interactive systems as input-output mappings (`L601`).
- Any theory-only explanation that is not immediately tied to a concrete design choice.

### Remove or Compress

- Repeated explanations of the closed-loop turn cycle.
- Repeated claims about interpretability and observability.
- Repeated justification for symbolic abstraction.
- Repeated discussion of mixed timing domains.
- Repeated limitations and future-work commentary.
- Repeated reintroduction of analysis -> planning -> generation as if it were new each time.

### Structural reset required

- Rebuild the chapter around design decisions, not subsystems.
- Stop sectioning by internal pipeline stage. That structure guarantees implementation leakage.
- Keep Chapter 3 at the level of system purpose, design rationale, and trade-offs only.

## 7. Quality Score

- Abstraction quality: `2/10`
- Structural clarity: `4/10`
- Repetition: `8/10` where high = bad

### Score justification

- `2/10` abstraction quality because most of the chapter explains internals rather than design intent.
- `4/10` structural clarity because there is a visible section order, but it is the wrong order: it follows implementation decomposition instead of design reasoning.
- `8/10` repetition because the same interaction-loop, interpretability, timing, and trade-off claims recur across multiple sections with only superficial variation.
