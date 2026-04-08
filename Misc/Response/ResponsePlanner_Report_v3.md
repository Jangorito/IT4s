# Response Planner – Design Rationale (Report-Facing)

## Overview
The ResponsePlanner transforms analysed rhythmic input into a structured musical intention. It determines how the system should respond before any notes are generated.

## Purpose
This layer separates analysis from generation, enabling more human-like musical interaction. It interprets input, selects an interaction strategy, and parameterises that strategy before realisation.

## Core Concept
The planner outputs a ResponsePlan: an intent-level specification rather than raw note data.

## ResponsePlan Structure
- ResponseType
- TargetDensity
- TargetEnergy
- PreserveAnchors
- MirrorEnding
- VariationAmount
- SyncopationBias
- TurnLengthSteps

## Response Types
- MIRROR
- COMPLEMENT
- SIMPLIFY
- INTENSIFY
- CONTRAST
- FILL

## Behavioural Flow
1. Analyse input (TurnAnalysisResult)
2. Decide ResponseType (decision table)
3. Derive parameters from analysed features
4. Apply bounded stochastic variation
5. Output ResponsePlan

---

## Response Type Decision System

### Priority Order
1. End Activity
2. Anchors
3. Density + Energy
4. Segment Activity Profile (SAP)
5. Fallback

### Locked Decision Rules
1. If strong EndActivity → FILL
2. Else if strong Anchors and sufficient activity → MIRROR
3. Else if Anchors present but phrase open → COMPLEMENT
4. Else if low Density or Energy → INTENSIFY
5. Else if high Density and Energy → SIMPLIFY
6. Else if strong directional SAP → CONTRAST
7. Else if rising SAP without strong ending → COMPLEMENT
8. Else → MIRROR

### Musical Justification
- FILL handles phrase closure
- MIRROR reflects identifiable rhythmic material
- COMPLEMENT supports without copying
- INTENSIFY adds momentum to sparse input
- SIMPLIFY introduces contrast via reduction
- CONTRAST introduces opposition and conversational tension

### Edge Case Handling
- Sparse + strong ending → FILL
- Dense + strong anchors → MIRROR unless overcrowding dominates → SIMPLIFY
- Rising shape is not automatically a fill
- Weak or flat input → INTENSIFY
- Ambiguous cases → MIRROR

---

## Parameter Mapping

### Design Principle
Parameters are not arbitrary presets. Each response type transforms the analysed input into a target behaviour. The planner therefore produces a continuous control description rather than selecting from fixed output patterns.

### Global Constraints
All numeric control values are clamped into valid ranges after derivation and stochastic adjustment:
- TargetDensity ∈ [0,1]
- TargetEnergy ∈ [0,1]
- VariationAmount ∈ [0,1]
- SyncopationBias ∈ [0,1]

### MIRROR
Intent: repeat the user idea with light rephrasing.
- TargetDensity = input density
- TargetEnergy = input energy
- PreserveAnchors = true
- MirrorEnding = true
- VariationAmount = low to low-moderate (0.15–0.30)
- SyncopationBias = low-moderate (0.25–0.40)

This keeps rhythmic identity highly legible while avoiding exact repetition.

### COMPLEMENT
Intent: respond around the input instead of copying it.
- TargetDensity = slightly reduced relative to input (approximately ×0.9)
- TargetEnergy = slightly reduced relative to input (approximately ×0.9)
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount = moderate (0.30–0.50)
- SyncopationBias = moderate to moderately high (0.40–0.65)

This supports the phrase indirectly and leaves more rhythmic space.

### SIMPLIFY
Intent: calm or clarify an already busy phrase.
- TargetDensity = reduced strongly relative to input (approximately ×0.6)
- TargetEnergy = reduced moderately (approximately ×0.7)
- PreserveAnchors = true
- MirrorEnding = false
- VariationAmount = low (0.10–0.25)
- SyncopationBias = low (0.10–0.30)

This maintains enough structural continuity to sound related while reducing crowding.

### INTENSIFY
Intent: push sparse material forward.
- TargetDensity = input density + approximately 0.20
- TargetEnergy = input energy + approximately 0.15
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount = moderate-high (0.35–0.60)
- SyncopationBias = highish (0.50–0.75)

This increases motion and expressive pressure when the input is too weak to sustain dialogue alone.

### CONTRAST
Intent: answer differently rather than support directly.
- TargetDensity = inverse tendency relative to input (1 - input density)
- TargetEnergy = inverse tendency relative to input (1 - input energy)
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount = high (0.50–0.80)
- SyncopationBias = high (0.60–0.85)

This creates the strongest sense of opposition and agent personality.

### FILL
Intent: provide phrase-ending response or resolution.
- TargetDensity = input density + approximately 0.25
- TargetEnergy = input energy + approximately 0.20
- PreserveAnchors = false
- MirrorEnding = true
- VariationAmount = high to very high (0.60–0.90)
- SyncopationBias = very high (0.70–0.95)

This creates a more active and expressive terminal response with cadential force.

---

## Bounded Stochasticity

After parameter derivation, the planner applies small random offsets:
- Density and energy perturbation: around ±0.05
- Variation and syncopation perturbation: around ±0.10

This stochastic layer is bounded and subordinate to intent. Randomness should diversify outputs without erasing the behavioural identity of each response type.

---

## Key Contribution
The planner introduces a feature-driven decision and parameterisation layer that enables structured musical interaction rather than direct reactive generation. It turns analysed human input into a controllable response space suitable for later rhythmic realisation.
