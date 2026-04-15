# ResponsePlanner Specification
## Intelli-Trading 4s – Codex / Implementation-Facing Documentation

---

## 1. Purpose

The ResponsePlanner is responsible for converting a completed `TurnAnalysisResult` into a compact `ResponsePlan` that downstream response-generation components can realise.

It is **not** a generator.  
It is a **decision layer**.

Its job is to answer:

1. What kind of response should the AI produce?
2. How dense should that response be?
3. Should it align with source hits or occupy gaps?
4. Should salient anchors be preserved?
5. What ending relationship should be encouraged?

---

## 2. Position in Pipeline

Human Turn  
→ Turn Analysis  
→ **ResponsePlanner**  
→ ResponsePlan  
→ StructureBuilder  
→ MotifTransformer  
→ EndingAdjuster  
→ ConstraintPass  
→ PatternTurn

The ResponsePlanner should have no knowledge of later implementation details beyond the fields exposed in `ResponsePlan`.

---

## 3. Design Goals

The implementation should satisfy the following goals:

- fast enough for near-instant use in a trading-fours interaction
- deterministic enough to be debuggable
- flexible enough to allow bounded variation
- musically interpretable
- easy to justify in dissertation writing
- small enough to avoid becoming a rule jungle

---

## 4. Recommended Class Responsibility

### `ResponsePlanner`

Primary orchestration class.

Responsibilities:

- accept a `TurnAnalysisResult`
- optionally accept planning config / thresholds
- derive a small internal planning context
- choose `ResponseType`
- derive response parameters
- output `ResponsePlan`

The class should not:

- generate candidate patterns
- mutate `PatternTurn`
- score step positions directly
- contain downstream realization logic

---

## 5. Recommended Supporting Types

### 5.1 `ResponseType`

Enum defining the top-level relationship of the response to the source.

Recommended values:

- `Mirror`
- `Complement`
- `Simplify`
- `Intensify`
- `Contrast`
- `Fill`

---

### 5.2 `ResponsePlan`

Compact immutable or effectively immutable data object describing planner output.

Recommended v1 fields:

- `ResponseType ResponseType`
- `float TargetDensity`
- `float ComplementarityBias`
- `bool PreserveAnchors`
- `bool MirrorEnding`
- `int TurnLengthSteps`

Optional later fields:

- `float MetricalBiasStrength`
- `float RandomnessAmount`
- `float MotifReuseStrength`
- `EndingMode EndingModeOverride`

Do not add these extras unless they become genuinely necessary.

---

### 5.3 `ResponsePlannerConfig`

Config object containing thresholds and tunable weights.

Recommended responsibilities:

- feature thresholds
- density adjustment values
- energy thresholds
- anchor influence thresholds
- mapping weights used in response-type selection
- clamp values for output ranges

This should keep magic numbers out of planner logic.

---

### 5.4 `PlanningContext`

Optional internal helper object.

Purpose:

- flatten important values from `TurnAnalysisResult`
- store derived descriptors used repeatedly during planning

Example contents:

- `float SourceDensity`
- `float SourceEnergy`
- `int AnchorCount`
- `bool HasStrongEnding`
- `bool EndingIsOpen`
- `bool ActivityIsBackLoaded`
- `bool ActivityIsFrontLoaded`
- `int TurnLengthSteps`

This is not required, but strongly recommended if the planner starts becoming cluttered.

---

## 6. Planner Input Model

The planner consumes a `TurnAnalysisResult`.

It should not rely on every low-level feature equally. For v1, it should extract only the signals genuinely needed for response selection.

Recommended input usage:

### Density
Use for:
- sparse / balanced / busy categorisation
- target density adjustment
- simplify vs intensify decisions

### Energy
Use for:
- restrained / medium / assertive categorisation
- modifier on response intensity
- refine simplify / intensify / contrast decisions

### Anchors
Use for:
- deciding whether structural salience is worth preserving
- mirror/complement decisions
- possible motif-related downstream intent

### End Activity
Use for:
- deciding `MirrorEnding`
- identifying open vs emphatic endings
- supporting fill / taper / punch style intent indirectly

### Segment Activity Profile
Use for:
- front-loaded vs back-loaded profile
- whether complement or contrast may be musically useful
- local contour-aware planning decisions

---

## 7. Planner Output Model

### 7.1 `ResponseType`

Primary strategy label.

Meanings:

- `Mirror`: broadly align with source behaviour
- `Complement`: answer in gaps / call-response style
- `Simplify`: reduce complexity relative to source
- `Intensify`: increase activity or emphasis
- `Contrast`: respond differently in contour or occupancy
- `Fill`: answer sparse or weakly resolved source with added activity

---

### 7.2 `TargetDensity`

Float in normalized range `[0, 1]`.

Represents desired output density, not guaranteed final density.

Used by downstream generator to derive target hit count.

Should typically be derived from source density plus bounded adjustment.

Recommended rule:
- clamp after all adjustments
- avoid drastic jumps in v1

---

### 7.3 `ComplementarityBias`

Float in normalized range `[0, 1]`.

Represents whether the response should prefer source gaps or source-aligned positions.

Interpretation:
- `0.0` = strongly source-aligned
- `0.5` = neutral / mixed
- `1.0` = strongly gap-seeking

This should be one of the key knobs consumed by `StructureBuilder`.

---

### 7.4 `PreserveAnchors`

Boolean.

Meaning:
- `true`: downstream stages should favour source anchor positions or derived anchor influence
- `false`: anchor preservation is not part of the intended response behaviour

Use this only when anchor structure appears musically meaningful.

---

### 7.5 `MirrorEnding`

Boolean.

Meaning:
- `true`: downstream stages should bias toward an ending relationship similar to the source
- `false`: ending may diverge, remain open, taper, or be otherwise shaped by response type

This is intentionally minimal for v1. Richer ending modes can be layered later.

---

### 7.6 `TurnLengthSteps`

Integer.

Usually copied from the analysed turn or current runtime config.

This is included so downstream stages do not need to re-query upstream state.

---

## 8. Recommended Planning Procedure

Use a **two-stage planner**.

### Stage A: choose `ResponseType`
### Stage B: derive numeric / boolean control parameters

Do not attempt a monolithic all-at-once ruleset.

---

## 9. Stage A – Response Type Selection

Use a rule-guided weighted approach.

Recommended approach:

1. derive categorical descriptors from analysis
2. compute weighted scores for each `ResponseType`
3. choose highest score
4. apply small tie-break rules if required

This preserves interpretability while avoiding brittle if-else trees.

---

## 10. Recommended Derived Descriptors

Before scoring, derive a small set of booleans / coarse categories.

Examples:

- `isSparse`
- `isBusy`
- `isHighEnergy`
- `isLowEnergy`
- `hasMeaningfulAnchors`
- `hasStrongEnding`
- `endingIsOpen`
- `isBackLoaded`
- `isFrontLoaded`
- `isBalancedProfile`

These can be computed from config thresholds.

This makes planner logic easier to debug and report on.

---

## 11. Suggested Response-Type Heuristics

These are not rigid hard rules. They are the intended v1 planning logic.

### 11.1 Mirror
Prefer when:
- density is moderate / balanced
- energy is moderate
- source identity feels coherent
- anchors are meaningful
- no strong reason exists to simplify or intensify

### 11.2 Complement
Prefer when:
- source has meaningful gaps
- source anchors provide structure but do not saturate space
- call-response feel is desirable
- moderate density and balanced activity profile are present

### 11.3 Simplify
Prefer when:
- source is dense or busy
- energy is high
- local clustering is strong
- a clearer reply would be musically beneficial

### 11.4 Intensify
Prefer when:
- source is sparse or underactive
- source energy is low or moderate
- escalation feels appropriate
- response can add emphasis without congestion

### 11.5 Contrast
Prefer when:
- source is highly patterned or predictable
- mirroring would feel too literal
- complementing would still be too similar
- a more differentiated contour is desirable

### 11.6 Fill
Prefer when:
- source leaves obvious unfilled space
- ending is weak, open, or under-articulated
- a more active or completed answer is desirable
- local late-turn enrichment would help

---

## 12. Suggested Scoring Structure

Implement weighted additive scoring for each `ResponseType`.

Example pattern:

`score[Mirror] += ...`
`score[Complement] += ...`
`score[Simplify] += ...`

Recommended inputs into scores:

- density category
- energy category
- anchor presence
- ending type
- segment profile
- optional history / anti-repetition logic later

Example design principle:
- density and energy should be the strongest drivers
- anchors and ending should refine, not dominate
- segment profile should act as supporting context

Keep weight count small in v1.

---

## 13. Optional Tie-Break Rules

If top scores are close, use clear tie-breaks rather than arbitrary selection.

Recommended tie-break order:

1. prefer `Complement` over `Mirror` when conversational gap-play seems useful
2. prefer `Simplify` over `Contrast` when source density is very high
3. prefer `Fill` over `Intensify` when the issue is mainly a weak ending / sparse closure
4. prefer `Mirror` when anchors are strong and source is balanced

If you later add bounded randomness, apply it only after these rules and only within a narrow margin.

---

## 14. Stage B – Parameter Derivation

After selecting `ResponseType`, derive the concrete `ResponsePlan` fields.

This stage should be straightforward and mostly table-driven.

---

## 15. Recommended Parameter Rules by Response Type

### 15.1 Mirror
- `TargetDensity`: near source density
- `ComplementarityBias`: low-to-mid
- `PreserveAnchors`: often true when anchors are meaningful
- `MirrorEnding`: often true

### 15.2 Complement
- `TargetDensity`: near source density or slightly reduced
- `ComplementarityBias`: high
- `PreserveAnchors`: often true, but interpreted as structural reference rather than exact reuse
- `MirrorEnding`: usually false unless source ending is especially strong

### 15.3 Simplify
- `TargetDensity`: reduced from source
- `ComplementarityBias`: neutral to moderately low
- `PreserveAnchors`: true only if anchors are strong and help maintain identity
- `MirrorEnding`: false by default

### 15.4 Intensify
- `TargetDensity`: increased from source
- `ComplementarityBias`: neutral to moderately high
- `PreserveAnchors`: optional, depending on source coherence
- `MirrorEnding`: false unless strong ending imitation is desired

### 15.5 Contrast
- `TargetDensity`: may stay near source or shift moderately
- `ComplementarityBias`: depends on contour choice, often mid-to-high
- `PreserveAnchors`: usually false
- `MirrorEnding`: false

### 15.6 Fill
- `TargetDensity`: increased, especially if source is sparse
- `ComplementarityBias`: mid-to-high
- `PreserveAnchors`: optional
- `MirrorEnding`: usually false

---

## 16. Target Density Derivation

Recommended formula style:

`targetDensity = sourceDensity + responseTypeAdjustment + optionalModifier`

Where:
- `responseTypeAdjustment` comes from config
- `optionalModifier` may depend on energy / ending / segment profile
- final value is clamped to `[minDensity, maxDensity]`

Suggested v1 principle:
- keep adjustments modest
- avoid doubling perceived activity in one step
- let StructureBuilder realize the density rather than forcing exact outcomes

---

## 17. Complementarity Bias Derivation

This should be derived from response type plus contextual refinement.

Suggested baseline tendencies:

- `Mirror`: low
- `Complement`: high
- `Simplify`: low-to-mid
- `Intensify`: mid
- `Contrast`: mid-to-high
- `Fill`: mid-to-high

Context refinements:
- increase if source occupancy is high and there are meaningful gaps
- decrease if anchor mirroring is more important than gap-play
- clamp final value to `[0, 1]`

---

## 18. Preserve Anchors Derivation

Suggested boolean rule:

Set true when:
- anchor count or anchor salience passes threshold
- response type is not inherently anchor-rejecting
- preserving anchors would help retain source identity

Set false when:
- anchors are weak or unstable
- response type is `Contrast`
- source is too congested for anchor preservation to help

Avoid making this overly sensitive.

---

## 19. Mirror Ending Derivation

Suggested boolean rule:

Set true when:
- source ending is strong or structurally clear
- response type is `Mirror`
- preserving phrase relationship is desirable

Set false when:
- ending is open or weak
- response type encourages divergence
- later stages should have freedom to taper, fill, or punch

---

## 20. Configuration Recommendations

Create a dedicated config class rather than scattering numeric values.

Recommended config groups:

### Density Thresholds
- sparse threshold
- busy threshold
- min output density
- max output density

### Energy Thresholds
- low energy threshold
- high energy threshold

### Anchor Thresholds
- meaningful anchor count threshold
- anchor salience threshold if required

### Ending Thresholds
- strong ending threshold
- open ending threshold

### ResponseType Weights
- weights or bonuses used during scoring

### ResponseType Density Adjustments
- mirror delta
- complement delta
- simplify delta
- intensify delta
- contrast delta
- fill delta

---

## 21. Recommended Implementation Style

Use readable, debug-friendly code.

Good v1 shape:

- `Plan(TurnAnalysisResult analysis) -> ResponsePlan`
- `BuildContext(analysis, config) -> PlanningContext`
- `ChooseResponseType(context, config) -> ResponseType`
- `DerivePlan(context, responseType, config) -> ResponsePlan`

Optional helpers:
- `ScoreMirror(...)`
- `ScoreComplement(...)`
- etc.

This is preferable to one huge method.

---

## 22. Debugging / Inspection Recommendations

The planner should be easy to inspect at runtime.

Recommended debug outputs:

- source descriptor summary
- per-response-type scores
- selected response type
- final ResponsePlan fields

Example useful debug block:

- Density: 0.33 (Sparse)
- Energy: 0.24 (Low)
- Anchors: 2 (Meaningful)
- Ending: Open
- Profile: BackLoaded
- Scores: Mirror=1.8, Complement=2.4, Simplify=0.6, Intensify=2.1, Contrast=0.9, Fill=2.7
- Selected: Fill
- Plan: Density=0.46, CompBias=0.68, PreserveAnchors=true, MirrorEnding=false

This will greatly help integration with your existing debug-heavy workflow.

---

## 23. What the Planner Should Not Do

Do not let `ResponsePlanner`:

- place hits
- adjust spacing
- transform motifs
- rewrite endings directly
- validate final patterns
- loop through candidate patterns
- become a hidden evaluator

All of these belong elsewhere in the pipeline.

---

## 24. Future Extensions

Possible future upgrades:

- stochastic bounded selection among near-equal response types
- short-term response history to avoid repetitive strategy choice
- style profiles
- explicit ending mode output
- motif reuse strength output
- learned parameter tuning

These should be layered onto the v1 planner, not baked in prematurely.

---

## 25. Summary

Implementation target:

Build a lightweight, rule-guided `ResponsePlanner` that:

1. reads `TurnAnalysisResult`
2. derives a compact planning context
3. chooses one of six response types
4. converts that choice into a lean `ResponsePlan`
5. hands off cleanly to StructureBuilder and later stages

The planner should be small, musically interpretable, and strongly aligned with the real-time design goals of Intelli-Trading 4s.
