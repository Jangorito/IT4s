# ResponsePlanner Decision Logic
## Intelli-Trading 4s – Response Type Selection Specification

---

## 1. Overview

This document defines the **ResponseType decision logic** for the ResponsePlanner.

It specifies how the system selects a high-level response strategy based on features extracted from Turn Analysis.

This is the **core choice layer** of the planner.

---

## 2. Purpose

The goal of this logic is to:

- convert analysed musical features into a **clear response strategy**
- remain **interpretable and debuggable**
- support **real-time performance**
- provide **strong justification** for dissertation reporting

---

## 3. Input Descriptors

The planner reduces Turn Analysis into coarse descriptors.

### Density
- Sparse
- Balanced
- Busy

### Energy
- Low
- Medium
- High

### Anchors
- Weak
- Meaningful

### Ending
- Open / Weak
- Neutral
- Strong

### Activity Profile
- Front-loaded
- Balanced
- Back-loaded

---

## 4. Response Types

The planner selects one of:

- Mirror
- Complement
- Simplify
- Intensify
- Contrast
- Fill

---

## 5. Decision Table

| Condition | ResponseType | Rationale |
|----------|-------------|----------|
| Sparse + low energy + weak ending | Fill | Completes phrase |
| Sparse + low/medium energy | Intensify | Adds activity |
| Sparse + meaningful anchors | Complement | Uses structure without overlap |
| Balanced + meaningful anchors | Mirror | Preserves identity |
| Balanced + gaps present | Complement | Enables call-response |
| Busy + high energy | Simplify | Reduces congestion |
| Busy + clustered | Simplify | Improves clarity |
| Busy + saturated | Contrast | Avoids overcrowding |
| Strong ending + anchors | Mirror | Maintains phrase |
| Weak ending + back-loaded | Fill | Improves closure |

---

## 6. Decision Buckets

### Mirror
- balanced density
- medium energy
- meaningful anchors
- strong or stable ending

### Complement
- meaningful gaps
- moderate density
- useful anchors

### Simplify
- busy density
- high energy
- clustering present

### Intensify
- sparse density
- low/medium energy

### Contrast
- overly patterned input
- mirroring feels too literal

### Fill
- sparse input
- weak/open ending

---

## 7. Priority Rules

1. Simplify overrides all when input is too dense
2. Fill overrides Intensify when issue is ending
3. Complement preferred over Mirror when gaps exist
4. Mirror preferred over Contrast when identity is strong

---

## 8. Tie-Break Rules

| Conflict | Resolution |
|--------|-----------|
| Mirror vs Complement | Prefer Complement if gaps exist |
| Fill vs Intensify | Prefer Fill if ending is weak |
| Simplify vs Contrast | Prefer Simplify if dense |
| Mirror vs Contrast | Prefer Mirror if anchors strong |

---

## 9. Weighted Scoring Model

Each response type receives a score.

### Example Weights

| Feature | Mirror | Complement | Simplify | Intensify | Contrast | Fill |
|--------|-------|------------|----------|-----------|----------|------|
| Sparse | -1 | +1 | -2 | +2 | 0 | +2 |
| Balanced | +2 | +1 | 0 | 0 | 0 | 0 |
| Busy | -2 | -1 | +3 | -1 | +2 | -2 |
| Low Energy | 0 | 0 | -1 | +2 | 0 | +1 |
| Medium Energy | +1 | +1 | 0 | 0 | 0 | 0 |
| High Energy | 0 | 0 | +2 | 0 | +1 | -1 |
| Anchors | +2 | +2 | +1 | 0 | -1 | 0 |
| Weak Anchors | 0 | 0 | 0 | +1 | +1 | +1 |
| Strong Ending | +2 | 0 | 0 | 0 | 0 | -1 |
| Open Ending | -1 | 0 | 0 | +1 | 0 | +3 |

---

## 10. Final Selection Algorithm

1. Derive descriptors
2. Compute scores for each ResponseType
3. Select highest score
4. Apply tie-break rules if needed

---

## 11. Design Rationale

This approach:

- avoids brittle rule trees
- remains interpretable
- allows tuning via weights
- supports real-time constraints

---

## 12. Conclusion

The ResponsePlanner decision logic is a:

> weighted, rule-guided system that maps musical features to one of six response strategies.

It forms the foundation for all downstream response generation.
