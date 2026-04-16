# Skeleton Builder Specification (Unified Implementation-Facing)

## 1. Purpose of this document

This document captures the current implementation-facing specification for the **Skeleton Builder** module in the Intelli-Trading 4s response-generation system.

It consolidates the original specification notes with the newly locked design decisions on:

- metrical-priority weighting
- raw score decomposition
- relative score-term priority
- greedy, constraint-aware selection
- dynamic score suppression
- the distinction between pre-selection desirability and selection-time legality

It is intended to guide future coding work and future Codex prompts by defining:

- module responsibility
- pipeline position
- input and output contracts
- field-level structures
- behavioural expectations
- scoring logic
- selection logic
- stochasticity policy
- debug visibility expectations

This document replaces the need to consult a separate addendum for the currently agreed Skeleton Builder design.

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
- perform motif realisation in detail
- fully author the ending phrase
- perform final cleanup or constraint correction
- depend on orchestration-layer state
- depend on rendering, audio, or UI classes

---

## 3. Working definition

> Skeleton Builder converts planner intent into a sparse structural step pattern by selecting active time positions over the response turn timeline using metrical weighting, target density, source-relationship bias, anchor handling, phrase-shape cues, lightweight stochasticity, and selection-time structural constraints.

---

## 4. High-level design stance

Current agreed generation style:

- **hybrid**
- **score-and-select**
- optionally informed by **light phrase-shape priors**
- not pure template lookup
- not heavy search
- not opaque end-to-end generation

The module therefore operates in two broad phases:

### 4.1 Step scoring

Assign pre-selection desirability to each step.

### 4.2 Step selection

Choose a subset of steps under density, spacing, clustering, and optional rebalance constraints.

Later modules are then free to transform, shape, or validate that scaffold.

---

## 5. Core design priority: metric structure must dominate the score landscape

A newly locked implementation decision is that **metrical weighting must materially dominate the initial score landscape**.

This means metric salience is not a cosmetic modifier. Instead:

- the base attractiveness of a step should be strongly influenced by its metrical position
- weaker positions should need additional justification from source relation, anchor behaviour, or ending behaviour before they outrank strong positions
- response-specific behaviours such as complementarity or fill should reshape a metrically grounded landscape rather than replacing it

### Practical implication

`MetricStrengthWeight` and `StrongBeatPreferenceWeight` should be treated as high-importance controls when tuning the system.

### Design intention

The builder should feel like it is generating over a **hierarchical grid**, not a flat grid.

---

## 6. Public module contract

Suggested public interface:

```text
ISkeletonBuilder
- SkeletonPattern BuildSkeleton(SkeletonBuildRequest request)
```

Concrete class naming can remain flexible, but the behavioural shape above is the current target.

---

## 7. Input contract

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

## 8. Builder-facing `ResponsePlan` fields

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

## 9. Configuration contract

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
Weight for alignment or proximity to source activity.

#### `ComplementWeight`
Weight for source avoidance or interstitial behaviour.

#### `AnchorInfluenceWeight`
Weight for anchor-driven influence.

#### `EndingInfluenceWeight`
Weight for ending-region bias.

#### `StrongBeatPreferenceWeight`
Additional preference for stronger beat positions beyond general metric weighting.

#### `MinimumStepSpacing`
Minimum allowed spacing between selected steps unless explicitly overridden by mode or cluster policy.

#### `MaximumClusterSize`
Maximum locally dense burst size allowed during selection.

#### `DensityTolerance`
Permissible deviation from the derived target active-step count.

#### `SelectionJitter`
Magnitude of bounded stochastic perturbation added to break near-ties and avoid identical repetitions.

Recommended interpretation:

- `0.0` = deterministic
- larger values = slightly more varied outputs

#### `AllowOffbeatClusters`
Whether weaker-position local clusters are permitted when style or response mode supports them.

#### `RebalanceAcrossSegments`
Whether selection is allowed to rebalance activity across turn regions to avoid excessive concentration.

---

## 10. Output contract

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
Exposed final score per step after selection-time updates.

#### `StepMeta`
Per-step traceability structure for debug and interpretability.

#### `SelectedStepIndices`
Convenience index list of active steps.

#### `Summary`
Aggregate diagnostic information.

---

## 11. Step metadata contract

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
Contribution from source-aligned or source-avoiding logic.

#### `AnchorScore`
Contribution from anchor-related influence.

#### `EndingScore`
Contribution from ending-region bias.

#### `SpacingPenalty`
Penalty or suppression applied because of neighbourhood or clustering rules.

#### `JitterOffset`
Small bounded stochastic perturbation visible for debugging.

#### `FinalScore`
The effective score used by selection logic after composing raw desirability and selection-time updates.

#### `Selected`
Whether this step was chosen into the skeleton.

#### `SourceOccupied`
Whether the corresponding source step is active.

#### `SourceAnchor`
Whether the source step is marked as an anchor.

#### `InEndingRegion`
Whether the step falls inside the ending-sensitive window.

#### `Protected`
Whether the step should be treated as resistant to downstream removal or heavy change.

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

## 12. Pattern summary contract

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
- expose key stats in a planner or generator debug UI

---

## 13. Behavioural contract

Skeleton Builder should satisfy the following high-level expectations.

### 13.1 Density conformity

Convert normalised `TargetDensity` into a target active-step count and produce an output close to that count within tolerance.

### 13.2 Relationship fidelity

Reflect `ResponseType` and `ComplementarityBias` in step placement.

Expected tendencies:

- **Mirror** -> more source-aligned placements
- **Complement** -> more source-avoiding placements
- **Simplify** -> fewer hits on stronger metric positions
- **Intensify** -> more activity while preserving structural emphasis
- **Contrast** -> redistributed emphasis away from source-focus regions
- **Fill** -> more interstitial connective placements

### 13.3 Anchor policy compliance

If `PreserveAnchors` is enabled, source anchors should significantly shape score profiles and selected output.

### 13.4 Rhythmic plausibility

Selections should avoid structurally implausible scatter or ugly over-clustering unless the response mode and config intentionally allow it.

### 13.5 Downstream readiness

The output must remain a structural scaffold, not a fully authored performance pattern.

---

## 14. Raw scoring model

The builder now has a locked conceptual scoring decomposition.

For each step `s`, compute:

`RawScore(s) = MetricScore(s) + SourceRelationScore(s) + AnchorScore(s) + EndingScore(s) + LightPhraseBalancePrior(s) + LightDensityShaping(s) + Jitter(s)`

This defines the **pre-selection score landscape** from which selection operates.

### Important implementation note

`RawScore(s)` expresses how attractive a step is **before** hard legality checks, dynamic suppression, and final selection-time updates are applied.

The following should **not** be treated as primary raw-score terms:

- exact target-count satisfaction
- hard spacing legality
- maximum-cluster legality
- global segment-balancing outcomes

These belong mainly to the selection stage.

---

## 15. Locked scoring dimensions

### 15.1 `MetricScore(s)`

Metric score is the primary structural term.

Recommended conceptual form:

`MetricScore(s) = MetricStrengthWeight * MetricalSalience(s) + StrongBeatPreferenceWeight * StrongBeatBonus(s)`

#### Required behaviour

- `MetricalSalience(s)` should reflect the hierarchical strength of the step within the response grid
- `StrongBeatBonus(s)` should provide extra reward for especially important structural positions
- tuning should ensure metric structure remains evident after other score terms are added

#### Implementation guidance

The exact salience map can be formalised later, but it should be reusable and deterministic for a given grid specification. Since the project currently uses 12 steps per quarter, the salience model should align with that resolution.

### 15.2 `SourceRelationScore(s)`

This term makes the scaffold responsive to the source pattern.

It should account for factors such as:

- exact source-step overlap
- near-source proximity
- source-step absence or gap occupancy
- local source-activity context

#### Behavioural intention by response type

- **Mirror**: reward exact or near overlap
- **Complement**: reward non-overlap and gap occupancy
- **Simplify**: reward structurally condensed positions, often on stronger beats
- **Intensify**: preserve major source emphases while allowing extra support
- **Contrast**: reward redistributed emphasis away from source-focus regions
- **Fill**: reward connective or interstitial positions

#### Important priority rule

This term should reshape a metrically grounded score landscape, not dominate it.

### 15.3 `AnchorScore(s)`

This term gives extra structural weight to source anchors.

Recommended conceptual form:

`AnchorScore(s) = AnchorInfluenceWeight * AnchorAffinity(s) * AnchorPolicyFactor`

#### Required behaviour

- if `PreserveAnchors` is active, anchor-related steps should receive a meaningful boost
- anchor influence does not have to imply literal duplication only
- v1 implementation can remain simple by boosting exact or near-anchor positions

### 15.4 `EndingScore(s)`

This term gives the builder early phrase-ending awareness.

Recommended conceptual form:

`EndingScore(s) = EndingInfluenceWeight * EndingAffinity(s, EndingMode)`

#### Required behaviour

- apply only within a defined ending region
- remain a soft structural bias
- do not attempt to replace EndingAdjuster responsibilities

### 15.5 `LightPhraseBalancePrior(s)`

This is a light pre-selection bias only.

Its role is to mildly discourage structurally lopsided candidate landscapes, while leaving most phrase-balance control to later selection updates.

### 15.6 `LightDensityShaping(s)`

This term softly adapts the score landscape based on intended density.

#### Intended effect

- low-density plans should sharpen preference for stronger positions
- higher-density plans should allow more medium-strength or interstitial steps to compete

This is a permissiveness adjustment, not the main density-control mechanism.

### 15.7 `Jitter(s)`

This is the bounded stochasticity term.

Recommended interpretation:

- very small random perturbation
- used to break near ties and avoid total repetition
- should never override large structural score differences

A useful conceptual form is:

`Jitter(s) in [-SelectionJitter, +SelectionJitter]`

---

## 16. Relative priority ordering of score terms

The agreed implementation priority ordering is:

1. **MetricScore** — dominant structural prior
2. **SourceRelationScore** and **AnchorScore** — main response-shaping terms
3. **EndingScore** and **LightPhraseBalancePrior** — local structural refinement
4. **LightDensityShaping** — permissiveness adjustment
5. **Jitter** — close-call perturbation only

### Why this ordering matters

Without this ordering, source-reactivity could become too strong and collapse structural coherence.

With this ordering, the builder remains musically legible first and responsive second.

---

## 17. Selection strategy

The builder now has a locked conceptual selection strategy:

> **Greedy, constraint-aware selection with dynamic score suppression**

This means the builder should:

1. compute raw scores for all steps
2. derive a target number of active steps
3. iteratively choose the best currently valid step
4. update the remaining score landscape after each accepted selection
5. stop when target or tolerance conditions are satisfied, or no acceptable candidates remain

Heavier search strategies such as beam search or global combinational optimisation are explicitly out of scope for v1 because they would reduce inspectability and responsiveness for little gain in this short-turn setting.

---

## 18. Selection flow

### Step 0 — derive target count

Convert `TargetDensity` into an intended active-step count.

Conceptually:

- `targetCount = round(TargetDensity * TurnLengthSteps)`
- derive lower and upper tolerance bounds from `DensityTolerance`

### Step 1 — compute raw scores

Compute `RawScore(s)` and store candidate metadata.

### Step 2 — initialise selection state

Set up:

- empty selected-step set
- boolean active-step map
- per-segment counters if rebalance is enabled
- any helper structures needed for spacing and cluster checks

### Step 3 — repeatedly select best valid candidate

At each iteration, choose the highest-scoring remaining candidate that satisfies the current validity rules.

### Step 4 — update the score landscape

After each accepted step, modify neighbouring or competing candidates so the scaffold evolves adaptively rather than following a frozen initial ranking.

### Step 5 — stop condition

Stop when:

- the target count has been reached within tolerance, or
- no good valid candidates remain, or
- the upper tolerance bound has been reached

### Step 6 — build output object

Construct `SkeletonPattern` and populate metadata and summary fields.

---

## 19. Selection-time constraints and validity checks

A crucial clarified distinction is that some controls are **selection-time legality checks or adaptive controls**, not raw-score terms.

These include:

- minimum spacing
- maximum cluster size
- target-count satisfaction
- optional segment rebalance
- optional protected-step handling

### Why this matters

The implementation should not try to fold every structural rule into one monolithic score.

Instead:

- use raw score to express desirability
- use selection logic to enforce structural legality and adaptivity

This separation keeps the system more interpretable and tunable.

---

## 20. Dynamic score suppression

Dynamic suppression is a locked part of the design.

After a step is selected, the scores of certain remaining candidates should be reduced or adjusted.

This prevents:

- over-clustering
- excessively dense local bursts
- repeated selection from the same structural area
- messy or unstable scaffolds

### 20.1 Spacing suppression

If a candidate lies too close to an already selected step, it should either:

- be invalidated, or
- receive a very strong suppression penalty

A falloff-style suppression is acceptable if softer behaviour near the spacing boundary is preferred.

### 20.2 Cluster control

If selecting a candidate would cause local cluster size to exceed `MaximumClusterSize`, it should be blocked or heavily penalised.

### 20.3 Segment balancing

If `RebalanceAcrossSegments` is enabled:

- overfilled segments should receive penalties on remaining candidates
- underfilled segments may receive small boosts

### 20.4 Anchor protection hook

If anchor preservation requires it, selected anchor-related steps may be marked as protected so later suppression or downstream handling does not erase structurally important scaffold points.

---

## 21. Early stopping and tolerance handling

The selection stage should not blindly force exact target-count satisfaction if only poor candidates remain.

Recommended behaviour:

- allow stopping once the lower tolerance bound is satisfied and no sufficiently good valid candidates remain
- hard-stop if the upper tolerance bound has been reached

This prevents low-quality late additions that would damage the scaffold just to satisfy an exact count.

---

## 22. Stochasticity policy

The current agreed stochasticity policy is deliberately lightweight.

### 22.1 Purpose of stochasticity

Stochasticity exists to:

- break near ties
- avoid identical outputs on structurally similar inputs
- add slight variety without undermining the legibility of the scaffold

### 22.2 Placement in the pipeline

Jitter belongs in the **raw scoring stage** as a bounded perturbation, but only as a small term.

It should not replace the selection logic, and it should not be used to override large structural score differences.

### 22.3 Determinism expectations

When `SelectionJitter` is zero, the builder should behave deterministically and be straightforward to test.

When non-zero jitter is enabled, its effect should remain small enough to preserve the broader score landscape.

---

## 23. Internal processing model

The current intended algorithmic shape is:

1. validate request
2. derive helper maps from source turn and analysis
3. compute per-step feature values
4. compute `RawScore(s)` for each step
5. derive target active-step count
6. iteratively select steps under spacing, cluster, density, and rebalance rules
7. dynamically suppress or rebalance remaining candidates after each acceptance
8. construct `SkeletonPattern`
9. expose summary and metadata for debug visibility

This is now the locked conceptual processing model.

---

## 24. Helper data likely needed internally

These do not necessarily need to appear in the public request object, but the implementation will probably derive them internally:

- `bool[] sourceOccupiedSteps`
- `bool[] sourceAnchorSteps`
- `bool[] endingRegionSteps`
- metrical-strength map per step
- step-to-segment mapping if segment rebalance is enabled

Current preference:

- keep the public request object lean
- derive helper maps internally

---

## 25. Debug and metadata expectations

The locked design strengthens the case for rich debug metadata.

Because the builder now distinguishes raw scoring from selection-time suppression, the debug system becomes more valuable if it exposes at least:

- metric contribution
- source-relation contribution
- anchor contribution
- ending contribution
- spacing suppression or penalty effect
- jitter contribution
- final selected or not-selected status

This makes `SkeletonStepMeta` not just a convenience structure but a core interpretability aid.

---

## 26. Testing implications

The clarified design implies the following test families are especially important:

- deterministic zero-jitter cases
- density-conformity cases
- mirror vs complement relation cases
- anchor-preservation cases
- ending-bias cases
- spacing enforcement cases
- maximum-cluster cases
- rebalance-across-segments cases
- early-stop / lower-tolerance cases
- metadata traceability cases

---

## 27. Constraints on implementation style

Current implementation direction should preserve the following properties:

- pure logic module
- no Unity UI or audio dependencies
- easy to unit test
- deterministic when jitter is zero
- configurable without hardcoding project-specific values into algorithm logic
- inspectable outputs for debug UI
- limited scope: do not collapse downstream responsibilities back into this class

---

## 28. Provisional summary for implementation

At this stage, Skeleton Builder should be implemented as a controllable, inspectable, score-and-select structural generator that:

- consumes planner intent plus source context
- scores all step positions using a metrically dominant raw-score model
- selects a sparse scaffold using greedy, constraint-aware iteration
- dynamically suppresses competing candidates after each accepted step
- emits rich metadata for debugging and evaluation
- leaves local variation and final polish to downstream modules

In short, the module is now specified as a **metrical-first, response-aware, greedy structural scaffold generator** whose key strengths are responsiveness, explainability, and tunable musical legibility.
