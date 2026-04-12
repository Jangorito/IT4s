# ConstraintPass Design
## Intelli-Trading 4s – Report-Facing Documentation

---

## 1. Overview

The ConstraintPass is the final stage in the response generation pipeline. It ensures that the generated rhythmic pattern is **valid, playable, and musically coherent**.

Rather than generating or transforming content, it acts as a **safety and correction layer**, enforcing rules that prevent undesirable rhythmic artefacts.

---

## 2. Role in the System

Human Turn  
→ Turn Analysis  
→ ResponsePlanner  
→ ResponsePlan  
→ StructureBuilder  
→ MotifTransformer  
→ EndingAdjuster  
→ **ConstraintPass**  
→ PatternTurn  

---

## 3. Design Rationale

Given the lightweight nature of earlier stages, small inconsistencies may arise:

- overly dense clusters
- invalid spacing
- unintended overlaps from transformations

The ConstraintPass resolves these issues in a **deterministic, low-cost manner**, avoiding the need for expensive evaluation or regeneration.

---

## 4. Core Concept

The ConstraintPass:

> enforces a set of hard and soft constraints to ensure the output pattern is valid and musically sensible.

---

## 5. Constraint Types

### 5.1 Hard Constraints
Must always be satisfied:

- minimum spacing between hits
- no overlapping invalid states
- valid pattern length

---

### 5.2 Soft Constraints
Preferably satisfied:

- density close to target
- avoidance of excessive clustering
- balanced distribution

---

## 6. Key Rules

### Spacing Rule
Ensure minimum gap between hits (e.g. 1–2 steps).

### Density Clamp
Ensure total hits remain within acceptable range.

### Cluster Reduction
Remove or thin overly dense local regions.

### Boundary Safety
Ensure no illegal modifications near edges.

---

## 7. Correction Strategy

- detect violations
- remove or adjust lowest-priority hits
- optionally replace with better candidates

No regeneration loops are used.

---

## 8. Advantages

- extremely fast
- deterministic
- improves robustness
- decouples validation from generation

---

## 9. Relation to Explored Designs

Replaces:
- full evaluation systems
- scoring-based candidate selection
- iterative optimisation loops

These are retained as future extensions.

---

## 10. Dissertation Framing

The ConstraintPass demonstrates:

- separation of generation and validation
- practical constraint-based modelling
- efficient enforcement of musical rules

---

## 11. Future Work

- learned constraint weighting
- adaptive spacing rules
- integration with evaluation scoring

---

## 12. Conclusion

The ConstraintPass ensures that generated rhythms are **clean, valid, and performance-ready**, completing the response pipeline.
