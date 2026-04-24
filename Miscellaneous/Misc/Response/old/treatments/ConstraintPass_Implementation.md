# ConstraintPass Specification
## Intelli-Trading 4s – Implementation Guide

---

## 1. Objective

Validate and correct a PatternTurn by enforcing constraints.

---

## 2. Inputs

ConstraintInput:
- pattern (bool[])
- targetDensity
- spacingRules

---

## 3. Output

Corrected PatternTurn

---

## 4. Pipeline

apply():
    enforceSpacing()
    clampDensity()
    reduceClusters()
    return pattern

---

## 5. Spacing Enforcement

For each hit:
    ensure minimum gap to next hit

If violated:
    remove lower-priority hit

---

## 6. Density Clamp

currentHits = count(pattern)

if currentHits > max:
    remove weakest hits

if currentHits < min:
    optionally add hits (rare)

---

## 7. Cluster Reduction

Detect regions with excessive hits:
    thin cluster by removing some hits

---

## 8. Constraints

Hard:
- spacing
- valid indices

Soft:
- density alignment
- distribution

---

## 9. Performance

- linear time
- no iteration loops
- deterministic

---

## 10. Structure

ConstraintPass
    apply()

---

## 11. Principle

Detect → Fix → Return

---

## 12. Next Step

Pipeline Integration
