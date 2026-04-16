# Skeleton Builder Design Notes (Unified Report-Facing)

## 1. Purpose of this document

This unified document merges the original Skeleton Builder report-facing design notes with the newly agreed addendum material. It presents a coherent, dissertation-ready narrative that integrates both the initial rationale and the refined design decisions.

---

## 2. Pipeline position

ResponsePlan -> SkeletonBuilder -> MotifTransformer -> EndingAdjuster -> ConstraintPass -> PatternTurn

Skeleton Builder is the first generative stage responsible for structural time placement.

---

## 3. Core definition

Skeleton Builder converts planner intent into a sparse structural step pattern by selecting active time positions using metrical weighting, density, source relationship, anchors, and lightweight constraints.

---

## 4. Stronger emphasis on metrical weighting

A key refinement is that metrical hierarchy is now the dominant structural prior.

- Strong beats are inherently more attractive
- Weak beats require justification
- Response behaviour operates on top of metrical structure

This supports rhythmic intelligibility in short turn-based interaction.

---

## 5. Updated musical framing

Skeleton Builder is best described as a:

**metrically grounded structural generator**

This aligns with rhythm perception theory and strengthens dissertation justification.

---

## 6. Locked scoring model

Each step receives a structural suitability score:

RawScore(step) = MetricScore + SourceRelationScore + AnchorScore + EndingScore + PhraseBalance + DensityShaping + Jitter

This separates desirability from constraint enforcement.

---

## 7. Scoring dimensions

### Metric Score (dominant)
Encodes metrical hierarchy and strong-beat emphasis.

### Source Relation Score
Encodes mirroring, complementarity, contrast, etc.

### Anchor Score
Preserves salient structural events.

### Ending Score
Adds early phrase-ending awareness.

### Phrase Balance
Encourages structural spread.

### Density Shaping
Adjusts permissiveness based on density.

### Jitter
Small stochastic variation for tie-breaking.

---

## 8. Priority ordering

1. Metric score
2. Source relation + anchors
3. Ending + phrase cues
4. Density shaping
5. Jitter

This ensures musical legibility is preserved.

---

## 9. Selection algorithm (locked)

Greedy, constraint-aware selection with dynamic suppression.

Steps:
1. Compute scores
2. Derive target density
3. Iteratively select best valid step
4. Suppress competing candidates
5. Stop within tolerance

---

## 10. Constraint interpretation

Constraints are applied during selection, not scoring:

- spacing
- clustering
- density satisfaction
- segment balancing

---

## 11. Dynamic suppression

After selecting a step:
- nearby candidates are penalised
- clusters are limited
- segments are balanced

This enables adaptive generation.

---

## 12. Why this approach fits

- real-time suitable
- explainable
- musically grounded
- lightweight vs heavy search

---

## 13. Architectural role

Separates:
- intent (planner)
- structure (builder)
- variation (transformer)
- ending (adjuster)
- validation (constraint pass)

---

## 14. Literature grounding

Supports:
- metrical hierarchy
- rhythmic structure
- perceptual salience
- co-creative control

---

## 15. Dissertation-ready summary

Skeleton Builder evaluates all steps using weighted musical evidence, then selects a sparse scaffold under structural constraints.

---

## 16. Final summary

The design is now:
- metrically grounded
- structurally explicit
- algorithmically clear
- dissertation-justifiable
