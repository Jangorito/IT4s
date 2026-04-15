# StructureBuilder Specification
## Intelli-Trading 4s – Implementation Guide

---

## 1. Objective

Generate a PatternTurn by selecting optimal step positions using a scoring-based method.

---

## 2. Inputs

StructureInput:
- sourcePattern (bool[])
- anchors (int[])
- ResponsePlan plan

---

## 3. Output

PatternTurn (bool[] steps)

---

## 4. Pipeline

build():
    compute targetHits
    score all steps
    select top steps
    enforce spacing
    apply response tweaks
    return pattern

---

## 5. Hit Count

targetHits = round(TargetDensity × TurnLengthSteps)

Clamp within valid bounds.

---

## 6. Step Scoring

For each step i:

score = 0

# complementarity
if plan.ComplementarityBias > 0.5:
    if source[i] == false:
        score += 1
else:
    if source[i] == true:
        score += 1

# anchors
if plan.PreserveAnchors and i in anchors:
    score += 0.5

# metrical bias
if isStrongBeat(i):
    score += 0.3

# randomness
score += random(-ε, +ε)

---

## 7. Selection

sortedSteps = sort descending by score  
selected = take top targetHits  

---

## 8. Spacing Enforcement

Ensure minimum gap between steps.

If violation:
- remove lower-scored step
- replace with next candidate

---

## 9. ResponseType Adjustments

Mirror:
- favour source alignment

Complement:
- favour gaps

Simplify:
- reduce hits
- favour strong beats

Intensify:
- increase hits
- allow closer spacing

Contrast:
- avoid source-heavy areas

Fill:
- bias final segment

---

## 10. Performance Requirements

- O(n log n) or better
- no candidate loops
- no evaluation passes

---

## 11. Extensibility

Future additions:
- weighted scoring tuning
- candidate generation layer
- evaluation model

---

## 12. Class Structure

StructureBuilder
    build()

StepScorer
    score()

StepSelector
    selectTopK()

SpacingEnforcer
    enforce()

---

## 13. Principle

Rank → Select → Constrain

---

## 14. Next Step

Integrate into Response Generator
