# MotifTransformer Design
## Intelli-Trading 4s – Report-Facing Documentation

---

## 1. Overview

The MotifTransformer is a lightweight post-processing component that introduces recognisable rhythmic reuse into generated responses.

It operates after structural generation and modifies the pattern minimally to create a sense of musical continuity and interaction.

---

## 2. Role in the System

Human Turn  
→ Turn Analysis  
→ ResponsePlanner  
→ ResponsePlan  
→ StructureBuilder  
→ MotifTransformer  
→ PatternTurn  

---

## 3. Design Rationale

Complex motif systems were explored but rejected due to:
- short turn lengths (1–2 bars)
- real-time constraints
- limited input complexity

This led to a minimal, rule-based design.

---

## 4. Core Concept

Extract a small rhythmic fragment and reuse it with a simple transformation.

---

## 5. Motif Definition

- 2–6 steps
- contains 1–2 hits
- may include anchors

---

## 6. Extraction Strategy

Select motif based on:
- anchors
- local density
- end-of-turn bias

---

## 7. Transformations

- Direct reuse
- Shift
- Thin
- Gap-fill
- Ending echo

---

## 8. ResponseType Mapping

Mirror → reuse  
Complement → gap-fill  
Simplify → thin  
Intensify → reuse/shift  
Contrast → skip  
Fill → ending echo  

---

## 9. Constraints

- small local edits only
- preserve spacing
- limit density drift

---

## 10. Advantages

- fast
- simple
- improves interaction feel

---

## 11. Future Work

- multi-motif systems
- probabilistic transforms
- learned motif models

---

## 12. Conclusion

Provides lightweight musical continuity without complexity.
