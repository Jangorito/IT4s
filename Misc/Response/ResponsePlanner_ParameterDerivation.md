# ResponsePlanner Parameter Derivation
## Intelli-Trading 4s – ResponsePlan Mapping Specification

---

## 1. Overview

This document defines how the ResponsePlanner converts a selected `ResponseType` into concrete `ResponsePlan` values.

It follows the response-type decision layer and completes the planner’s core behaviour.

Pipeline position:

Human Turn  
→ Turn Analysis  
→ ResponsePlanner Decision Logic  
→ **Parameter Derivation**  
→ ResponsePlan  
→ StructureBuilder  
→ MotifTransformer  
→ EndingAdjuster  
→ ConstraintPass

---

## 2. Purpose

Once the planner has selected a `ResponseType`, it must translate that choice into a small set of implementation-ready control values.

These values should be:

- compact
- musically interpretable
- easy to debug
- suitable for downstream realization

This stage therefore answers:

- how dense should the response be?
- should it align with source hits or gaps?
- should anchors be preserved?
- should the ending mirror the source?

---

## 3. Core ResponsePlan Fields

The v1 planner output is defined by six core fields:

- `ResponseType`
- `TargetDensity`
- `ComplementarityBias`
- `PreserveAnchors`
- `MirrorEnding`
- `TurnLengthSteps`

This document focuses on how the middle four are derived from the chosen response type and the analysed source turn.

---

## 4. Field Definitions

### 4.1 TargetDensity

`TargetDensity` is a normalized value in the range `[0, 1]`.

It represents the intended density of the generated response, not an exact promise of final output.

Downstream systems use this value to determine an approximate hit count.

---

### 4.2 ComplementarityBias

`ComplementarityBias` is a normalized value in the range `[0, 1]`.

Interpretation:

- `0.0` = strongly source-aligned
- `0.5` = neutral / mixed
- `1.0` = strongly gap-seeking

This value tells the StructureBuilder whether to favour source hit positions or the spaces around them.

---

### 4.3 PreserveAnchors

`PreserveAnchors` is a boolean flag.

Interpretation:

- `true` = downstream stages should preserve or respect meaningful anchor structure
- `false` = anchor preservation is not part of the intended response relationship

This does not necessarily imply exact copying of anchor positions. It means anchor structure should remain musically influential.

---

### 4.4 MirrorEnding

`MirrorEnding` is a boolean flag.

Interpretation:

- `true` = downstream stages should aim for an ending relationship similar to the source
- `false` = the ending may diverge and be shaped differently

This provides a lightweight v1 control for ending behaviour without requiring a full ending-mode enum.

---

## 5. Parameter Derivation Strategy

Parameter derivation should be:

- response-type-led
- context-refined
- bounded
- simple enough to tune

The recommended method is:

1. set baseline values from `ResponseType`
2. refine using source analysis context
3. clamp normalized outputs
4. emit a compact `ResponsePlan`

This keeps the system both interpretable and flexible.

---

## 6. Baseline Mapping Table

The following table defines the default mapping from each `ResponseType` to planner outputs.

| ResponseType | TargetDensity | ComplementarityBias | PreserveAnchors | MirrorEnding | Rationale |
|---|---:|---:|---|---|---|
| Mirror | near source | low-mid | usually true | usually true | Preserve source relationship |
| Complement | near/slightly below source | high | often true | usually false | Call-response in gaps |
| Simplify | below source | low-mid | sometimes true | usually false | Reduce congestion |
| Intensify | above source | mid | optional | usually false | Increase activity |
| Contrast | near or moderately shifted | mid-high | usually false | false | Deliberate divergence |
| Fill | above source | mid-high | optional | usually false | Add activity, especially for closure |

---

## 7. Recommended Numeric Baselines

The table below gives concrete v1 baseline values before context adjustment.

| ResponseType | Density Delta | Baseline ComplementarityBias | Baseline PreserveAnchors | Baseline MirrorEnding |
|---|---:|---:|---|---|
| Mirror | `+0.00` | `0.30` | `true` | `true` |
| Complement | `-0.05` | `0.75` | `true` | `false` |
| Simplify | `-0.15` | `0.40` | `false` | `false` |
| Intensify | `+0.12` | `0.55` | `false` | `false` |
| Contrast | `+0.00` | `0.65` | `false` | `false` |
| Fill | `+0.15` | `0.70` | `false` | `false` |

Recommended interpretation:

- `Density Delta` is applied to source density first
- all final densities should be clamped to safe min/max bounds
- boolean defaults may be overridden by strong context

---

## 8. TargetDensity Derivation

### 8.1 General Formula

Recommended formula:

`TargetDensity = clamp(SourceDensity + ResponseTypeDelta + ContextModifier, MinDensity, MaxDensity)`

Where:

- `SourceDensity` comes from analysis
- `ResponseTypeDelta` comes from the baseline table
- `ContextModifier` is a small bounded adjustment
- `MinDensity` and `MaxDensity` come from planner config

---

### 8.2 Recommended Density Principles

The planner should avoid extreme jumps.

Good v1 rules:

- prefer small-to-moderate adjustments
- do not double perceived activity in one step
- keep output density within musically realistic bounds
- let StructureBuilder realize density approximately rather than forcing exactness

Suggested default clamp range:

- `MinDensity = 0.10`
- `MaxDensity = 0.80`

These can be tuned later.

---

### 8.3 Density Context Modifiers

Recommended context refinements:

#### Mirror
- little or no modifier
- may slightly increase if source energy is high but density is still moderate

#### Complement
- slightly reduce if source already occupies many strong positions
- keep near source if source is balanced and conversational

#### Simplify
- stronger negative modifier if source is both busy and high-energy
- weaker reduction if source is only moderately dense

#### Intensify
- increase slightly more when source is very sparse
- reduce the increase if source ending is already strong

#### Contrast
- allow small positive or negative modifier depending on profile
- avoid extreme density divergence unless a strong reason exists

#### Fill
- increase more when ending is weak or back-loaded
- reduce if source is sparse overall but ending is already strong

---

## 9. ComplementarityBias Derivation

### 9.1 Baseline Logic

This value should come primarily from `ResponseType`.

Recommended baseline interpretation:

- Mirror: low
- Complement: high
- Simplify: low-mid
- Intensify: mid
- Contrast: mid-high
- Fill: mid-high

---

### 9.2 Context Refinements

Increase `ComplementarityBias` when:

- source occupancy is high
- meaningful gaps exist
- a conversational response is desirable
- preserving anchors does not require close alignment

Decrease `ComplementarityBias` when:

- source identity should be reflected more directly
- anchors are especially important
- ending relationship should remain close to source
- response type is Mirror

Suggested refinement bounds:

- add or subtract at most `0.10` to `0.15`

Clamp final value to `[0, 1]`.

---

### 9.3 Interpretation by Response Type

#### Mirror
Bias toward source-aligned positions.

Typical final range:
- `0.20 – 0.40`

#### Complement
Bias strongly toward source gaps.

Typical final range:
- `0.65 – 0.85`

#### Simplify
Remain fairly neutral, but do not strongly chase gaps.

Typical final range:
- `0.30 – 0.50`

#### Intensify
Use a mixed relationship.

Typical final range:
- `0.45 – 0.65`

#### Contrast
Encourage differentiated placement without becoming random.

Typical final range:
- `0.55 – 0.75`

#### Fill
Bias toward adding activity where the source leaves space, especially late in the turn.

Typical final range:
- `0.60 – 0.80`

---

## 10. PreserveAnchors Derivation

### 10.1 Default Rule

Baseline value depends on `ResponseType`, but should only remain true if anchors are actually meaningful.

Recommended principle:

> never preserve anchors simply because the response type says so; preserve them only when anchor structure contributes musically useful identity.

---

### 10.2 Set PreserveAnchors = true when

- anchor count passes a meaningful threshold
- anchor salience is strong enough
- response type is Mirror or Complement
- preserving anchors supports recognisability
- source is not so congested that anchor preservation would make spacing worse

---

### 10.3 Set PreserveAnchors = false when

- anchors are weak or unstable
- response type is Contrast
- source is too dense for anchor preservation to remain useful
- Simplify would benefit from stripping back to core metrical positions
- the source has no clear structural identity to preserve

---

### 10.4 ResponseType Tendencies

| ResponseType | PreserveAnchors tendency |
|---|---|
| Mirror | true if anchors meaningful |
| Complement | often true |
| Simplify | conditional |
| Intensify | usually false |
| Contrast | usually false |
| Fill | usually false |

This should remain a conservative flag.

---

## 11. MirrorEnding Derivation

### 11.1 Default Rule

`MirrorEnding` should be true only when the source ending is musically clear enough to be worth reflecting.

It should not be enabled by default for every response.

---

### 11.2 Set MirrorEnding = true when

- source ending is strong or structurally clear
- response type is Mirror
- preserving phrase identity is desirable
- ending shape is a defining part of the turn

---

### 11.3 Set MirrorEnding = false when

- source ending is weak or open
- the response should complete rather than imitate
- response type is Complement, Contrast, Fill, or most Intensify cases
- later stages should have freedom to taper, open, or punch the ending differently

---

### 11.4 ResponseType Tendencies

| ResponseType | MirrorEnding tendency |
|---|---|
| Mirror | often true |
| Complement | usually false |
| Simplify | usually false |
| Intensify | usually false |
| Contrast | false |
| Fill | false |

This field should remain a small, phrase-level control rather than a full ending policy.

---

## 12. Full Parameter Table

This is the recommended v1 mapping table.

| ResponseType | TargetDensity rule | ComplementarityBias rule | PreserveAnchors rule | MirrorEnding rule |
|---|---|---|---|---|
| Mirror | `SourceDensity + 0.00`, small context refinement | low, typically `0.20–0.40` | true if anchors meaningful | true if ending strong |
| Complement | `SourceDensity - 0.05`, clamp | high, typically `0.65–0.85` | often true if anchors meaningful | false by default |
| Simplify | `SourceDensity - 0.15`, possibly more if very busy | low-mid, `0.30–0.50` | true only if anchors clearly help identity | false |
| Intensify | `SourceDensity + 0.12`, slightly more if very sparse | mid, `0.45–0.65` | usually false | false |
| Contrast | near source or small shift, avoid extremes | mid-high, `0.55–0.75` | usually false | false |
| Fill | `SourceDensity + 0.15`, especially if ending weak | mid-high, `0.60–0.80` | usually false | false |

---

## 13. Suggested Implementation Shape

Recommended helper flow:

1. `ChooseResponseType(...)`
2. `DeriveTargetDensity(...)`
3. `DeriveComplementarityBias(...)`
4. `DerivePreserveAnchors(...)`
5. `DeriveMirrorEnding(...)`
6. construct `ResponsePlan`

This is preferable to one large monolithic planner method.

---

## 14. Example Worked Mappings

### Example A – Balanced, anchored, strong ending
Input summary:

- density: balanced
- energy: medium
- anchors: meaningful
- ending: strong
- profile: balanced
- chosen response type: Mirror

Derived response plan:

- `TargetDensity ≈ SourceDensity`
- `ComplementarityBias ≈ 0.30`
- `PreserveAnchors = true`
- `MirrorEnding = true`

Interpretation:
Preserve the source’s musical identity and phrase shape.

---

### Example B – Sparse, weak ending, low energy
Input summary:

- density: sparse
- energy: low
- anchors: weak
- ending: open
- profile: back-loaded
- chosen response type: Fill

Derived response plan:

- `TargetDensity = SourceDensity + 0.15` (clamped)
- `ComplementarityBias ≈ 0.70`
- `PreserveAnchors = false`
- `MirrorEnding = false`

Interpretation:
Add activity and help complete the phrase rather than imitate it.

---

### Example C – Busy, high-energy, clustered
Input summary:

- density: busy
- energy: high
- anchors: some, but source congested
- ending: strong
- profile: front-loaded
- chosen response type: Simplify

Derived response plan:

- `TargetDensity = SourceDensity - 0.15` (or slightly lower)
- `ComplementarityBias ≈ 0.40`
- `PreserveAnchors = false` or conditional
- `MirrorEnding = false`

Interpretation:
Answer clearly by reducing complexity and avoiding congestion.

---

## 15. Design Rationale

This parameter-derivation model is strong for the project because it:

- preserves modularity
- keeps planner behaviour interpretable
- allows controlled tuning
- maps cleanly onto StructureBuilder and later stages
- avoids premature complexity

The planner therefore remains a true decision layer rather than collapsing into full generation logic.

---

## 16. Conclusion

The ResponsePlanner should derive `ResponsePlan` values by combining:

- a baseline mapping from `ResponseType`
- small context-sensitive refinements from analysis
- bounded numeric clamps
- conservative boolean rules for anchors and endings

This produces a compact, musically meaningful control object that downstream stages can realize efficiently in real time.
