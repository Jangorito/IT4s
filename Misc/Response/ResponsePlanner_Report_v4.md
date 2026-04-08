# Response Planner – Design Rationale (Report-Facing, v4)

## Overview
The ResponsePlanner transforms analysed rhythmic input into a structured musical intention. It determines how the system should respond before any notes are generated.

This layer separates analysis from generation, enabling more human-like musical interaction. It interprets input, selects an interaction strategy, and parameterises that strategy before realisation.

The upgraded formulation extends this by incorporating:

- Anchor Support (structural grounding)
- Complementarity (relational behaviour between input and response)

---

## Purpose
The planner exists to bridge the gap between:

- low-level rhythmic analysis
- high-level musical interaction

Rather than generating notes directly, it produces a **ResponsePlan**, an intent-level specification that defines how the system should behave musically.

---

## Core Concept
The planner outputs a **ResponsePlan**: a structured representation of intent rather than explicit note sequences.

This enables:

- modular system design
- controllable behaviour
- clearer musical justification in the dissertation

---

## ResponsePlan Structure (Updated)

- ResponseType
- TargetDensity
- TargetEnergy
- PreserveAnchors
- MirrorEnding
- VariationAmount
- SyncopationBias
- ComplementarityBias
- TurnLengthSteps

---

## ComplementarityBias (New)

ComplementarityBias ∈ [0,1] defines how the response should relate to the input:

- 0.0 → reinforce / align with input
- 0.5 → mixed behaviour
- 1.0 → strongly interlocking / gap-filling

This introduces an explicit representation of **call-and-response behaviour**, rather than relying on implicit generation rules.

---

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
2. Anchors + Anchor Support
3. Density + Energy
4. Segment Activity Profile (SAP)
5. Fallback

---

## Anchor Interpretation (Updated)

The planner now considers:

- anchor presence (where important hits occur)
- anchor support (how metrically grounded they are)

This allows the system to distinguish between:

- strong, stable rhythmic ideas
- weak or structurally unsupported input

---

## Locked Decision Rules (Updated)

1. If strong EndActivity → FILL

2. Else if strong Anchors AND high support → MIRROR

3. Else if Anchors present AND low support → INTENSIFY or STABILISING COMPLEMENT

4. Else if Anchors present → COMPLEMENT

5. Else if low Density OR low Energy → INTENSIFY

6. Else if high Density AND high Energy → SIMPLIFY

7. Else if strong directional SAP → CONTRAST

8. Else if rising SAP without strong ending → COMPLEMENT

9. Else → MIRROR

---

## Musical Justification

- **FILL** handles phrase closure
- **MIRROR** reflects clear rhythmic identity
- **COMPLEMENT** supports without copying
- **INTENSIFY** reinforces weak or sparse input
- **SIMPLIFY** reduces overcrowding
- **CONTRAST** introduces conversational tension

Anchor support enables the system to distinguish between:

- ideas worth preserving
- ideas that require reinforcement or reinterpretation

---

## Parameter Mapping

### Design Principle

Parameters are derived from analysed features, not selected from fixed presets.

All values are clamped:

- TargetDensity ∈ [0,1]
- TargetEnergy ∈ [0,1]
- VariationAmount ∈ [0,1]
- SyncopationBias ∈ [0,1]
- ComplementarityBias ∈ [0,1]

---

### MIRROR
- TargetDensity = input density
- TargetEnergy = input energy
- PreserveAnchors = true
- MirrorEnding = true
- VariationAmount = 0.15–0.30
- SyncopationBias = 0.25–0.40
- ComplementarityBias = 0.10–0.30

---

### COMPLEMENT
- TargetDensity ≈ input × 0.9
- TargetEnergy ≈ input × 0.9
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount = 0.30–0.50
- SyncopationBias = 0.40–0.65
- ComplementarityBias = 0.70–0.90

---

### SIMPLIFY
- TargetDensity ≈ input × 0.6
- TargetEnergy ≈ input × 0.7
- PreserveAnchors = true
- MirrorEnding = false
- VariationAmount = 0.10–0.25
- SyncopationBias = 0.10–0.30
- ComplementarityBias = 0.20–0.40

---

### INTENSIFY
- TargetDensity = input + 0.20
- TargetEnergy = input + 0.15
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount = 0.35–0.60
- SyncopationBias = 0.50–0.75
- ComplementarityBias = 0.50–0.70

---

### CONTRAST
- TargetDensity = 1 - input density
- TargetEnergy = 1 - input energy
- PreserveAnchors = false
- MirrorEnding = false
- VariationAmount = 0.50–0.80
- SyncopationBias = 0.60–0.85
- ComplementarityBias = 0.40–0.60

---

### FILL
- TargetDensity = input + 0.25
- TargetEnergy = input + 0.20
- PreserveAnchors = false
- MirrorEnding = true
- VariationAmount = 0.60–0.90
- SyncopationBias = 0.70–0.95
- ComplementarityBias = 0.60–0.80

---

## Bounded Stochasticity

After parameter derivation:

- Density/Energy ±0.05
- Variation/Syncopation ±0.10
- Complementarity ±0.05

Randomness is bounded and subordinate to intent.

---

## Key Contribution

The planner introduces:

- feature-driven decision making
- support-aware structural interpretation
- explicit relational modelling via complementarity

This enables structured musical interaction rather than direct reactive generation.

---

## Design Insight

The system cleanly separates:

- Analysis → what happened
- Planning → what should happen
- Realisation → how it is executed

This separation is critical for both:

- extensibility
- dissertation justification