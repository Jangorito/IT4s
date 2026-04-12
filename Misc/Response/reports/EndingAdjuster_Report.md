# EndingAdjuster Design
## Intelli-Trading 4s – Report-Facing Documentation

---

## 1. Overview

The EndingAdjuster is a lightweight post-processing component responsible for shaping the **final segment** of a generated rhythmic response.

In short-form musical interaction (1–2 bars), the ending plays a critical role in:

- signalling closure or continuation
- reinforcing response intent
- shaping perceived musicality

The EndingAdjuster ensures that endings are **intentional rather than incidental**.

---

## 2. Role in the System

Human Turn  
→ Turn Analysis  
→ ResponsePlanner  
→ ResponsePlan  
→ StructureBuilder  
→ MotifTransformer  
→ **EndingAdjuster**  
→ ConstraintPass  
→ PatternTurn  

---

## 3. Design Rationale

Earlier designs relied on emergent endings from generation pipelines. However, due to:

- short temporal scope
- conversational interaction requirements
- perceptual importance of endings

an explicit ending-shaping stage was introduced.

Unlike full phrase-modelling systems, this component:

- operates only on a small region
- uses simple rule-based logic
- avoids iterative generation or evaluation

---

## 4. Core Concept

The EndingAdjuster:

> inspects the final portion of the pattern and applies a small, targeted transformation to align with the intended response behaviour.

---

## 5. Ending Window

The system defines a fixed **ending window**, typically:

- last 2–4 steps (short patterns)
- last 4–8 steps (longer patterns)

All adjustments are strictly limited to this region.

---

## 6. Inputs

### From ResponsePlan
- ResponseType
- MirrorEnding
- TargetDensity

### From Analysis
- source ending pattern
- optional end activity features

### From Current Pattern
- generated pattern from previous stages

---

## 7. Ending Modes

The EndingAdjuster supports a small set of modes:

### Mirror
Align response ending with source ending.

### Open
Avoid strong closure; allow flow into next turn.

### Fill
Increase activity in the ending region.

### Taper
Reduce activity toward the end.

### Punch
Emphasise a strong final hit.

---

## 8. Mode Selection

Mode is selected based on ResponsePlan:

- MirrorEnding → Mirror
- Fill → Fill
- Simplify → Taper
- Intensify → Punch
- Complement / Contrast → Open
- Default → no change

---

## 9. Behavioural Effects

### Mirror
- copy or approximate source ending pattern

### Open
- reduce density
- avoid final clustering

### Fill
- increase local density
- allow clustering

### Taper
- remove late hits
- increase spacing

### Punch
- ensure one strong late hit

---

## 10. Constraints

- only modify ending window
- preserve global density where possible
- maintain spacing constraints (except controlled fill)
- skip adjustment if already suitable

---

## 11. Advantages

- extremely low computational cost
- improves phrase clarity
- enhances conversational feel
- integrates cleanly with planner decisions

---

## 12. Relation to Explored Designs

Replaces more complex concepts such as:

- full phrase modelling
- candidate ending generation
- ending scoring systems

These remain as potential future work.

---

## 13. Dissertation Framing

The EndingAdjuster demonstrates:

- explicit modelling of phrase termination
- alignment of generation with interaction design
- practical simplification of musical structure handling

---

## 14. Future Work

- dynamic window sizing
- learned ending preferences
- stylistic ending models
- integration with evaluation layer

---

## 15. Conclusion

The EndingAdjuster provides a **targeted, efficient solution** for shaping rhythmic endings, ensuring that responses feel complete, intentional, and musically coherent.
