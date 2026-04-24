# ResponsePlanner Evaluation (Expanded Draft)

## 1. Overview

This section evaluates the behaviour of the ResponsePlanner using a synthetic dataset of generated rhythmic patterns. Each pattern is passed through a full pipeline:

PatternTurn → TurnAnalysis → ResponsePlanner → ResponsePlan

The purpose of this evaluation is twofold:

1. To validate that the planner behaves in a musically coherent and internally consistent manner  
2. To establish a data-driven foundation for parameter tuning

---

## 2. Motivation

Designing a rule-based or hybrid AI system for musical interaction presents a key challenge:

How can decision-making be both musically meaningful and systematically tunable?

To address this, a synthetic dataset was constructed to probe the planner across a wide range of rhythmic conditions and expose decision boundaries.

---

## 3. Dataset Construction

The dataset consists of synthetically generated rhythmic turns with controlled variation in:

- Density
- Energy
- Structural profile
- Anchor distribution
- Ending activity

Each base pattern includes variants to simulate realistic variation.

---

## 4. Evaluation Methodology

The evaluation is structured around:

- Behavioural alignment (musical correctness)
- Decision consistency (score vs selection)
- Sensitivity to variation (base vs variant)
- Explainability (score transparency)

---

## 5. Key Findings

- The planner is mostly deterministic but exhibits indifference zones (ties)
- ~29% of decisions are low-confidence (borderline)
- Response stability varies significantly by type
- Anchor features strongly influence decision changes
- Behavioural regions overlap (e.g. Fill vs Intensify)

---

## 6. Implications

The planner operates as a multi-objective system with overlapping feature spaces.

This supports expressive behaviour but introduces ambiguity, motivating structured tuning.

---

## 7. Role in Tuning

This dataset enables:

- Identification of decision boundaries
- Detection of unstable regions
- Data-driven adjustment of thresholds and weights

---

## 8. Next Steps

- Build parameter suggestion engine
- Refine scoring contributions
- Introduce tie-breaking strategies

The ResponsePlanner was designed to provide interpretable high-level intent rather than perfect categorical classification. Evaluation showed that some overlap between response types was musically acceptable, and even desirable, provided that downstream response generation rendered those intents in clearly differentiated ways.