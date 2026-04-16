# Skeleton Builder Specification Notes (Implementation-Facing)

## 1. Purpose of this document

This document captures the current implementation-facing specification for the **Skeleton Builder** module in the Intelli-Trading 4s response-generation system.

It is intended to guide future coding work and later Codex prompts by defining:

- module responsibility
- system position
- input and output contracts
- field-level structures
- behavioural expectations
- placeholder sections for scoring, selection, stochasticity, and debug outputs

This document is intentionally more concrete than the report-facing notes, but it still leaves some sections open where the next design decisions have not yet been fully locked.

---

## 2. Pipeline position

Current agreed pipeline:

`ResponsePlan -> SkeletonBuilder -> MotifTransformer -> EndingAdjuster -> ConstraintPass -> PatternTurn`

### Skeleton Builder responsibility

Skeleton Builder is the **first generative stage** after planning.

Its job is to:

- consume planner intent plus source-turn context
- score step positions across the response timeline
- select a sparse set of active structural positions
- output an intermediate structural scaffold, **not** the final performance pattern

### Non-responsibilities

Skeleton Builder should **not**:
- decide the overall response type
- perform motif-realisation in detail
- fully author the ending phrase
- perform final cleanup / constraint correction
- depend on orchestration-layer state
- depend on rendering, audio, or UI classes

---

## 3. Working definition

> Skeleton Builder converts planner intent into a sparse structural step pattern by selecting active time positions over the response turn timeline using metrical weighting, target density, source-relationship bias, anchor handling, phrase-shape cues, and lightweight constraints.

---

## 4. High-level design stance

Current agreed generation style:

- **hybrid**
- **score-and-select**
- optionally informed by **light phrase-shape priors**
- not pure template lookup
- not heavy search
- not opaque end-to-end generation

This means the module conceptually operates in two broad phases:

1. **Step scoring**
   - assign desirability to each step

2. **Step selection**
   - choose a subset of steps under density and spacing constraints

Later modules are then free to transform, shape, or validate that scaffold.

---

## 5. Public module contract

Suggested public interface:

```text
ISkeletonBuilder
- SkeletonPattern BuildSkeleton(SkeletonBuildRequest request)
```

Concrete class name can remain flexible, but the behavioural shape above is the current target.

---

## 6. Input contract

### `SkeletonBuildRequest`

Current agreed request object:

```text
SkeletonBuildRequest
- ResponsePlan Plan
- PatternTurn SourceTurn
- TurnAnalysisResult SourceAnalysis
- int TurnLengthSteps
- int StepsPerQuarter
- SkeletonBuilderConfig Config
```

### Input field intent

#### `Plan`
High-level structural intention derived by the planner.

#### `SourceTurn`
The human/source pattern being responded to.

#### `SourceAnalysis`
Previously computed turn-analysis results for the source turn.

#### `TurnLengthSteps`
Response turn length in grid steps.

#### `StepsPerQuarter`
Metric resolution information. Current project context uses 12 steps per quarter.

#### `Config`
Builder-specific policy and tuning values.

---

## 7. Builder-facing `ResponsePlan` fields

Only the plan fields relevant to structural time placement should be considered required for the builder.

### Current v1 plan view

```text
ResponsePlan
- ResponseType ResponseType
- float TargetDensity
- float ComplementarityBias
- bool PreserveAnchors
- EndingMode EndingMode
- int TurnLengthSteps
```

### Field meanings

#### `ResponseType`
High-level relationship category.

Current working response-type set:
- Mirror
- Complement
- Simplify
- Intensify
- Contrast
- Fill

#### `TargetDensity`
Normalised target density for the response scaffold.

Recommended interpretation:
- `0.0` = no active steps
- `1.0` = all steps active

Actual target count is derived from this value and `TurnLengthSteps`.

#### `ComplementarityBias`
Continuous control for source-aligned vs source-avoiding placement.

Recommended range:
- `-1.0` = strong mirroring tendency
- `0.0` = neutral
- `+1.0` = strong complementarity tendency

#### `PreserveAnchors`
Indicates that source anchors should significantly influence structural selection.

#### `EndingMode`
Lightweight ending-intent control.

Current working values:
- Neutral
- Taper
- Punch
- Fill
- Open
- Mirror

#### `TurnLengthSteps`
The intended structural response length.

---

## 8. Configuration contract

### `SkeletonBuilderConfig`

Current proposed v1 fields:

```text
SkeletonBuilderConfig
- float MetricStrengthWeight
- float MirrorWeight
- float ComplementWeight
- float AnchorInfluenceWeight
- float EndingInfluenceWeight
- float StrongBeatPreferenceWeight
- int MinimumStepSpacing
- int MaximumClusterSize
- float DensityTolerance
- float SelectionJitter
- bool AllowOffbeatClusters
- bool RebalanceAcrossSegments
```

### Field meanings

#### `MetricStrengthWeight`
Weight for metrical salience in step scoring.

#### `MirrorWeight`
Weight for alignment / proximity to source activity.

#### `ComplementWeight`
Weight for source avoidance / interstitial behaviour.

#### `AnchorInfluenceWeight`
Weight for anchor-driven influence.

#### `EndingInfluenceWeight`
Weight for ending-region bias.

#### `StrongBeatPreferenceWeight`
Additional preference for stronger beat positions beyond general metric weighting.

#### `MinimumStepSpacing`
Minimum allowed spacing between selected steps unless explicitly overridden by mode / cluster policy.

#### `MaximumClusterSize`
Maximum locally dense burst size allowed during selection.

#### `DensityTolerance`
Permissible deviation from the derived target active-step count.

#### `SelectionJitter`
Magnitude of bounded stochastic perturbation added before ranking or tie-breaking.

Recommended interpretation:
- `0.0` = deterministic
- larger values = slightly more varied outputs

#### `AllowOffbeatClusters`
Whether weaker-position local clusters are permitted when style/response mode supports them.

#### `RebalanceAcrossSegments`
Whether selection is allowed to rebalance activity across turn regions to avoid excessive concentration.

---

## 9. Output contract

### `SkeletonPattern`

Current preferred output shape:

```text
SkeletonPattern
- int TurnLengthSteps
- bool[] ActiveSteps
- float[] SelectionScores
- SkeletonStepMeta[] StepMeta
- int[] SelectedStepIndices
- SkeletonPatternSummary Summary
```

### Output field meanings

#### `TurnLengthSteps`
Length of the skeleton in steps.

#### `ActiveSteps`
Boolean activation map across the turn.

#### `SelectionScores`
Final or exposed selection score per step.

#### `StepMeta`
Per-step traceability structure for debug and interpretability.

#### `SelectedStepIndices`
Convenience index list of active steps.

#### `Summary`
Aggregate diagnostic information.

---

## 10. Step metadata contract

### `SkeletonStepMeta`

Current proposed structure:

```text
SkeletonStepMeta
- int StepIndex
- float MetricScore
- float SourceRelationScore
- float AnchorScore
- float EndingScore
- float SpacingPenalty
- float JitterOffset
- float FinalScore
- bool Selected
- bool SourceOccupied
- bool SourceAnchor
- bool InEndingRegion
- bool Protected
- SkeletonReasonFlags ReasonFlags
```

### Field meanings

#### `StepIndex`
Absolute step position in the response turn.

#### `MetricScore`
Contribution from metrical weighting only.

#### `SourceRelationScore`
Contribution from source-aligned / source-avoiding logic.

#### `AnchorScore`
Contribution from anchor-related influence.

#### `EndingScore`
Contribution from ending-region bias.

#### `SpacingPenalty`
Penalty applied because of neighbourhood or clustering rules.

#### `JitterOffset`
Small bounded stochastic perturbation visible for debugging.

#### `FinalScore`
Combined value used by selection logic.

#### `Selected`
Whether this step was chosen into the skeleton.

#### `SourceOccupied`
Whether the corresponding source step is active.

#### `SourceAnchor`
Whether the source step is marked as an anchor.

#### `InEndingRegion`
Whether the step falls inside the ending-sensitive window.

#### `Protected`
Whether the step should be treated as resistant to downstream removal / heavy change.

#### `ReasonFlags`
Compact explanation flags.

### Candidate `SkeletonReasonFlags`

```text
None
MetricStrong
MetricWeak
MirrorBoosted
ComplementBoosted
AnchorBoosted
EndingBoosted
SpacingSuppressed
SelectedByTieBreak
ProtectedAnchor
```

This set can remain small in v1.

---

## 11. Pattern summary contract

### `SkeletonPatternSummary`

Current suggested structure:

```text
SkeletonPatternSummary
- int ActiveCount
- float AchievedDensity
- int SourceOverlapCount
- int AnchorAlignedCount
- bool DensityTargetMet
- bool UsedStochasticTieBreak
```

### Purpose

This object is primarily diagnostic. It should help:

- debug generation behaviour
- compare achieved vs intended output
- support later evaluation tooling
- expose key stats in a planner / generator debug UI

---

## 12. Behavioural contract

Skeleton Builder should satisfy the following high-level expectations.

### 12.1 Density conformity
Convert normalised `TargetDensity` into a target active-step count and produce an output close to that count within tolerance.

### 12.2 Relationship fidelity
Reflect `ResponseType` and `ComplementarityBias` in step placement.

Expected tendencies:
- **Mirror** -> more source-aligned placements
- **Complement** -> more source-avoiding placements
- **Simplify** -> fewer hits, stronger metric positions
- **Intensify** -> more activity while keeping structure
- **Contrast** -> redistributed emphasis away from source patterns
- **Fill** -> more interstitial connective placements

### 12.3 Anchor policy compliance
If `PreserveAnchors` is enabled, source anchors should significantly shape score profiles and selected output.

### 12.4 Rhythmic plausibility
Selections should avoid structurally implausible scatter or ugly over-clustering unless the response mode and config intentionally allow it.

### 12.5 Downstream readiness
The output must remain a structural scaffold, not a fully authored performance pattern.

---

## 13. Internal processing model (current)

This is not final pseudocode yet, but the current intended shape is:

1. validate request
2. derive helper maps from source turn and analysis
3. compute per-step features
4. compute per-step weighted scores
5. derive target active-step count
6. select steps under spacing / density / cluster constraints
7. optionally rebalance across segments
8. construct `SkeletonPattern`
9. expose summary and metadata for debug visibility

---

## 14. Helper data likely needed internally

These do not necessarily need to appear in the public request object, but the implementation will probably derive them:

- `bool[] sourceOccupiedSteps`
- `bool[] sourceAnchorSteps`
- `bool[] endingRegionSteps`
- metric-strength map per step
- step-to-segment mapping if segment rebalance is enabled

Current preference:
- keep the public request object lean
- derive helper maps internally

---

## 15. Sections intentionally left open for next design pass

The next design stage should fill these sections. They are included here now so future edits can extend them rather than recreate the document.

### 15.1 Step scoring dimensions
To define:
- exact metric score model
- exact source relation score model
- exact anchor influence model
- exact ending-region model
- exact final score combination

### 15.2 Selection algorithm
To define:
- target count derivation
- ranking and candidate acceptance logic
- spacing enforcement method
- cluster handling
- rebalance strategy
- protected-step handling

### 15.3 Stochasticity policy
To define:
- where jitter enters the pipeline
- whether it is applied before or after constraints
- whether it only resolves close calls
- how determinism should be handled for tests / debug

### 15.4 Debug snapshot contract
To define:
- exact builder debug snapshot object
- how it plugs into current UI/debug tooling
- whether summary and per-step breakdown are exposed separately

### 15.5 Testing strategy
To define:
- deterministic config cases
- relationship-mode cases
- density cases
- anchor-preservation cases
- ending-bias cases
- spacing and cluster cases

---

## 16. Constraints on implementation style

Current implementation direction should preserve the following properties:

- pure logic module
- no Unity UI/audio dependencies
- easy to unit test
- deterministic when jitter is zero
- configurable without hardcoding project-specific values into algorithm logic
- inspectable outputs for debug UI
- limited scope: do not collapse downstream responsibilities back into this class

---

## 17. Provisional summary for implementation

At this stage, the Skeleton Builder should be implemented as a controllable, inspectable, score-and-select structural generator that:

- consumes planner intent plus source context
- scores all step positions
- selects a sparse scaffold matching intended density and relationship
- emits rich metadata for later debugging and evaluation
- leaves local variation and final polish to downstream modules