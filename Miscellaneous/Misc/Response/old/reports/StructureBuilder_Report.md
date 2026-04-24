# StructureBuilder Design
## Intelli-Trading 4s – Report-Facing Documentation

---

## 1. Overview

The StructureBuilder is the core generative component of the response system. It is responsible for determining the **temporal placement of hits** within a response turn.

Unlike traditional generative systems that build patterns through layered transformations, the StructureBuilder adopts a **ranking-based placement strategy**, selecting the most musically appropriate steps based on planner-driven features.

---

## 2. Role in the System

Human Turn  
→ Turn Analysis  
→ ResponsePlanner  
→ ResponsePlan  
→ **StructureBuilder**  
→ PatternTurn  

The StructureBuilder is the **first and most critical stage** of response generation.

---

## 3. Design Rationale

The system operates under strict constraints:

- short temporal scope (1–2 bars)
- single drum pad input
- requirement for near-instant response

Given these constraints, a complex multi-stage generator was deemed unnecessary.

Instead, the StructureBuilder uses a **priority-based selection model**, which:

- reduces computational cost
- maintains musical coherence
- aligns closely with planner output

---

## 4. Core Concept

The StructureBuilder does not construct patterns procedurally.

Instead, it:

> ranks all possible step positions based on musical relevance, then selects the top candidates.

---

## 5. Inputs

### From ResponsePlan
- ResponseType
- TargetDensity
- ComplementarityBias
- PreserveAnchors
- TurnLengthSteps

### From TurnAnalysis
- source pattern (binary step sequence)
- anchor positions

---

## 6. Hit Count Determination

The number of hits is derived from density:

targetHits = round(TargetDensity × TurnLengthSteps)

This defines the structural constraint of the output.

---

## 7. Step Scoring Model

Each step is assigned a score based on:

### 7.1 Complementarity

Determines interaction with the source pattern:

- high bias → prefer gaps (call-response)
- low bias → align with existing hits (mirroring)

---

### 7.2 Anchor Influence

If anchors are preserved:

- boost anchor step scores

Otherwise:

- ignore or slightly penalise anchors

---

### 7.3 Metrical Bias

To introduce musical structure:

- strong beats receive higher scores
- weak positions receive lower scores

---

### 7.4 Random Variation

Small bounded noise is added to prevent deterministic repetition.

---

## 8. Selection Process

1. Score all steps
2. Sort by score
3. Select top K steps

This produces the structural skeleton of the pattern.

---

## 9. Spacing Constraints

A post-processing step ensures:

- minimum spacing between hits
- removal of excessive clustering

This maintains playability and clarity.

---

## 10. ResponseType Influence

ResponseType modifies scoring behaviour:

- Mirror → align with source
- Complement → fill gaps
- Simplify → reduce density, favour strong beats
- Intensify → increase density and spread
- Contrast → avoid source-heavy regions
- Fill → bias final segment

---

## 11. Advantages

- extremely fast (single-pass)
- musically interpretable
- tightly coupled to planner output
- extensible without redesign

---

## 12. Relation to Explored Designs

The StructureBuilder replaces earlier concepts such as:

- SkeletonBuilder (multi-stage)
- Candidate-based generation
- Pattern scoring systems

These were excluded due to latency and limited benefit in short-form interaction.

---

## 13. Dissertation Framing

The StructureBuilder represents a **constraint-guided, feature-driven generation model**, prioritising:

- responsiveness
- interpretability
- alignment with interaction design

---

## 14. Future Extensions

Potential upgrades include:

- candidate generation and evaluation
- richer metrical models
- adaptive weighting
- multi-instrument support

---

## 15. Conclusion

The StructureBuilder provides a **minimal yet musically grounded solution** for real-time rhythmic generation in short-form AI-human interaction.
