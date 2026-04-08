# Response Planner – Implementation Specification (v4)

## Purpose

Convert TurnAnalysisResult into a ResponsePlan.

---

## Input

- Density
- Energy
- Anchors
- AnchorSupport
- EndActivity
- SegmentActivityProfile

---

## Output

ResponsePlan:
- ResponseType Type
- float TargetDensity
- float TargetEnergy
- bool PreserveAnchors
- bool MirrorEnding
- float VariationAmount
- float SyncopationBias
- float ComplementarityBias
- int TurnLengthSteps

---

## Core API

ResponsePlan Plan(TurnAnalysisResult analysis)

---

## Processing Steps

1. DecideType(analysis)
2. BuildPlan(type, analysis)
3. ApplyStochasticVariation(plan)
4. Clamp(plan)

---

## AnchorSupport (New Feature)

Fields expected:

- AverageSupport
- StrongHitRatio
- SupportedWeakHitRatio
- UnsupportedWeakHitRatio

Interpretation:

- High support → structurally grounded
- Low support → unstable or weak rhythm

---

## Decision Logic

Priority order:

1. EndActivity strong → FILL

2. Anchors strong AND AverageSupport > 0.6 → MIRROR

3. Anchors present AND AverageSupport < 0.4 → INTENSIFY or COMPLEMENT

4. Anchors present → COMPLEMENT

5. Density low OR Energy low → INTENSIFY

6. Density high AND Energy high → SIMPLIFY

7. SAP strongly directional → CONTRAST

8. SAP rising → COMPLEMENT

9. Else → MIRROR

---

## Thresholds

- High support: > 0.6
- Low support: < 0.4

Constants (non-learned in v1)

---

## ComplementarityBias

Range: [0,1]

Meaning:

- 0 → align with input
- 1 → interlock / fill gaps

---

## Parameter Mapping

### MIRROR
- ComplementarityBias = random(0.1, 0.3)

### COMPLEMENT
- ComplementarityBias = random(0.7, 0.9)

### SIMPLIFY
- ComplementarityBias = random(0.2, 0.4)

### INTENSIFY
- ComplementarityBias = random(0.5, 0.7)

### CONTRAST
- ComplementarityBias = random(0.4, 0.6)

### FILL
- ComplementarityBias = random(0.6, 0.8)

---

## BuildPlan Updates

- Assign ComplementarityBias based on ResponseType
- Modulate using AnchorSupport:
    - low support → increase complementarity slightly
    - high support → decrease complementarity slightly

---

## Stochastic Variation

Apply:

- Density/Energy ±0.05
- Variation/Syncopation ±0.10
- Complementarity ±0.05

Then clamp all values to [0,1]

---

## Constraints

- deterministic decision logic
- no pattern generation here
- no candidate evaluation
- planner defines intent only

---

## Realiser Contract

Planner outputs intent.

Realiser must:

- interpret ComplementarityBias
- generate patterns accordingly
- enforce relational behaviour

---

## Extensibility

Future upgrades may include:

- weighted decision systems
- learned planner
- history-aware complementarity
- adaptive thresholds

---

## Non-goals

Do NOT:

- compute complementarity scores here
- generate rhythms here
- modify analyser outputs

---

## Expected Behaviour

Planner should:

- distinguish strong vs weak input
- choose musically appropriate response types
- encode relational intent explicitly

This enables a more expressive and controllable realisation stage.