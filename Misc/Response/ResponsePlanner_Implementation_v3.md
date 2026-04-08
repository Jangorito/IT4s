# Response Planner – Implementation Specification (Codex-Facing)

## Purpose
Convert TurnAnalysisResult into a ResponsePlan.

## Input
- Density
- Energy
- Anchors
- EndActivity
- SegmentActivityProfile

## Output
ResponsePlan:
- ResponseType Type
- float TargetDensity
- float TargetEnergy
- bool PreserveAnchors
- bool MirrorEnding
- float VariationAmount
- float SyncopationBias
- int TurnLengthSteps

## Core API
ResponsePlan Plan(TurnAnalysisResult analysis)

## Processing Steps
1. DecideType(analysis)
2. BuildPlan(type, analysis)
3. ApplyStochasticVariation(plan)
4. Clamp(plan)

---

## Decision Logic (Locked)

Priority order:
1. if EndActivity strong → FILL
2. else if Anchors strong AND activity sufficient → MIRROR
3. else if Anchors present → COMPLEMENT
4. else if Density low OR Energy low → INTENSIFY
5. else if Density high AND Energy high → SIMPLIFY
6. else if SAP strongly directional → CONTRAST
7. else if SAP rising (no strong ending) → COMPLEMENT
8. else → MIRROR

## Feature Interpretation
- EndActivity: terminal spike / closing gesture
- Anchors: strong rhythmic identity points
- Density: proportion of active steps
- Energy: velocity / intensity
- SAP: distribution of activity across segments

## Edge Cases
- Sparse + strong ending → FILL
- Dense + anchors → MIRROR unless overcrowding dominates → SIMPLIFY
- Rising SAP alone ≠ FILL
- Weak input → INTENSIFY
- Ambiguous → MIRROR

---

## Parameter Mapping (Locked Baseline)

### Global constraints
Clamp after derivation and after stochastic offsets:
- TargetDensity in [0,1]
- TargetEnergy in [0,1]
- VariationAmount in [0,1]
- SyncopationBias in [0,1]

### MIRROR
Intent: retain identity with light variation.
- TargetDensity = analysis.Density.StepDensity
- TargetEnergy = analysis.Energy.AverageVelocity
- PreserveAnchors = true
- MirrorEnding = true
- VariationAmount in [0.15, 0.30]
- SyncopationBias in [0.25, 0.40]

### COMPLEMENT
Intent: support around the input instead of duplicating it.
- TargetDensity = analysis.Density.StepDensity * 0.9
- TargetEnergy = analysis.Energy.AverageVelocity * 0.9
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount in [0.30, 0.50]
- SyncopationBias in [0.40, 0.65]

### SIMPLIFY
Intent: reduce clutter while preserving some structure.
- TargetDensity = analysis.Density.StepDensity * 0.6
- TargetEnergy = analysis.Energy.AverageVelocity * 0.7
- PreserveAnchors = true
- MirrorEnding = false
- VariationAmount in [0.10, 0.25]
- SyncopationBias in [0.10, 0.30]

### INTENSIFY
Intent: increase momentum and activity.
- TargetDensity = analysis.Density.StepDensity + 0.20
- TargetEnergy = analysis.Energy.AverageVelocity + 0.15
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount in [0.35, 0.60]
- SyncopationBias in [0.50, 0.75]

### CONTRAST
Intent: oppose the character of the input.
- TargetDensity = 1.0 - analysis.Density.StepDensity
- TargetEnergy = 1.0 - analysis.Energy.AverageVelocity
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount in [0.50, 0.80]
- SyncopationBias in [0.60, 0.85]

### FILL
Intent: produce a cadential / ending response.
- TargetDensity = analysis.Density.StepDensity + 0.25
- TargetEnergy = analysis.Energy.AverageVelocity + 0.20
- PreserveAnchors = false
- MirrorEnding = true
- VariationAmount in [0.60, 0.90]
- SyncopationBias in [0.70, 0.95]

---

## Stochastic Control (Locked Baseline)

Apply small bounded perturbations after base parameter derivation:
- TargetDensity += random in [-0.05, +0.05]
- TargetEnergy += random in [-0.05, +0.05]
- VariationAmount += random in [-0.10, +0.10]
- SyncopationBias += random in [-0.10, +0.10]

Rules:
- randomness must diversify outputs without changing response identity
- MIRROR should remain close and coherent
- CONTRAST should remain oppositional
- FILL should remain expressive and cadential

---

## Constraints
- deterministic baseline decision logic
- no direct pattern generation in planner
- no feature recomputation
- parameter values are derived from analysed input plus bounded variation

## Extensibility
- replace hard rules with weighted scoring
- add stochastic selection between top candidates
- add history/context awareness
- later swap in learned planner while preserving ResponsePlan contract
