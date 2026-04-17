# Skeleton Builder Baseline Evaluation (Pre-Phase 1)

## 1. Purpose

This evaluation assesses the current Skeleton Builder’s behaviour using the extended batch harness. The goal is to determine:

- whether planner intent is realised in the generated skeleton
- whether response types produce distinct rhythmic identities
- whether the system exhibits the shortcomings identified in the prior audit

This report establishes a **baseline** prior to any Phase 1 tuning.

---

## 2. Key Findings Summary

| Area | Verdict |
|------|--------|
| Response-type differentiation | Present but inconsistent |
| Weak-step usage | Suppressed across most types |
| Metric dominance | Strong and often decisive |
| Response identity | Partially expressed, often blurred |
| Ending behaviour | Largely uniform across types |
| Anchor preservation | Strong and reliable |

---

## 3. Response-Type Differentiation

### 3.1 Aggregate Divergence

Average pairwise divergence (Jaccard distance):

- Mirror: 0.346  
- Complement: 0.374  
- Simplify: 0.487  
- Intensify: 0.454  
- Contrast: 0.345  
- Fill: 0.490  

### Interpretation

- Response types are **not globally collapsed**
- There is measurable structural variation across outputs
- However, divergence is **not consistently aligned with musical intent**

---

### 3.2 Collapse Cases

Repeated observation across sources:

> Closest pair: Mirror vs Simplify = 0.0

### Interpretation

- Mirror and Simplify frequently produce **identical skeletons**
- This indicates insufficient differentiation between:
  - *“echo the call”* (Mirror)
  - *“reduce to essentials”* (Simplify)

---

## 4. Weak-Step Usage

| Response Type | Weak-Step Ratio |
|--------------|----------------|
| Mirror       | 0.079 |
| Complement   | 0.051 |
| Simplify     | 0.027 |
| Contrast     | 0.061 |
| Intensify    | 0.184 |
| Fill         | 0.241 |

### Interpretation

- Weak steps are **heavily underused** across most response types
- Only:
  - **Fill** (0.241)
  - **Intensify** (0.184)  
  show meaningful usage

### Conclusion

This confirms:

> Weak-step deferral is suppressing expressive placement.

The system is capable of using weak steps, but:
- access to them is too restricted
- especially for Complement and Contrast

---

## 5. Metric Dominance

| Response Type | Strongest-Step Ratio |
|--------------|---------------------|
| Simplify     | 0.601 |
| Complement   | 0.447 |
| Mirror       | 0.371 |
| Contrast     | 0.371 |
| Intensify    | 0.218 |
| Fill         | 0.198 |

### Interpretation

- Strong beats remain the primary driver of selection
- Even non-conservative types (Complement, Contrast) remain heavily metric-biased

### Conclusion

> Metric salience still dominates selection across most response types.

Other scoring terms rarely override metric hierarchy.

---

## 6. Source Relationship Behaviour

| Response Type | Overlap Ratio |
|--------------|--------------|
| Mirror       | 0.564 |
| Simplify     | 0.513 |
| Complement   | 0.259 |
| Contrast     | 0.248 |
| Intensify    | 0.321 |
| Fill         | 0.186 |

### Interpretation

- Mirror and Simplify behave as expected (high overlap)
- Fill uses more gaps (lower overlap)

However:

- Complement, Contrast, and Fill are **closer than expected**
- Differences are **quantitative, not qualitative**

### Conclusion

> Response types express intent weakly through source relation.

They differ in degree (more vs less overlap), but not in **distinct structural strategies**.

---

## 7. Ending Behaviour

| Response Type | Ending Occupancy |
|--------------|-----------------|
| Mirror       | 0.179 |
| Complement   | 0.178 |
| Simplify     | 0.188 |
| Intensify    | 0.140 |
| Contrast     | 0.163 |
| Fill         | 0.158 |

### Interpretation

- Ending usage is **nearly uniform across all response types**

### Conclusion

> Ending behaviour is not response-specific.

Endings act as a generic late-placement preference rather than:
- closure
- continuation
- interruption
- or conversational reply

---

## 8. Anchor Preservation

| Response Type | Anchor Preservation |
|--------------|--------------------|
| Mirror       | 0.883 |
| Simplify     | 0.872 |
| Complement   | 0.824 |
| Intensify    | 0.836 |
| Contrast     | 0.769 |
| Fill         | 0.772 |

### Interpretation

- Anchor preservation is **consistently high across all types**

### Conclusion

> Anchor handling is functioning correctly and should remain unchanged.

---

## 9. Overall Diagnosis

### Strengths

- Deterministic and stable selection
- Clear metrical grounding
- Reliable anchor preservation
- Measurable (non-zero) response-type divergence
- Functional density scaling

### Weaknesses

1. **Weak-step suppression**
   - Prevents expressive and syncopated responses
   - Particularly limits Complement and Contrast

2. **Mirror vs Simplify collapse**
   - Indicates insufficient behavioural differentiation

3. **Shallow response identity**
   - Complement, Contrast, and Fill behave similarly
   - Differences are incremental, not structural

4. **Metric dominance**
   - Overpowers source relation and response intent

5. **Generic ending behaviour**
   - Lacks conversational meaning

---

## 10. Phase 1 Focus Areas

Based on this evaluation, Phase 1 tuning should prioritise:

### 1. Weak-step deferral relaxation
- Enable expressive use of interstitial positions
- Especially for Fill, Contrast, and Complement

### 2. Increased source-relation influence
- Strengthen overlap vs gap decisions
- Improve differentiation between response types

### 3. Slight reduction in metric dominance
- Allow non-metric factors to occasionally win
- Preserve overall rhythmic stability

---

## 11. Deferred Areas (Later Phases)

The following should not be addressed in Phase 1:

- Ending behaviour redesign
- Phrase contour modelling
- Segment-based shaping
- Post-selection optimisation
- Anchor-based phrase logic

---

## 12. Final Verdict

The Skeleton Builder is:

> **Structurally sound but expressively constrained.**

It successfully produces stable, metrical skeletons, but:

- response intent is under-realised
- expressive placements are suppressed
- multiple response types converge toward similar outputs

Phase 1 should focus on **unlocking controlled expressiveness** without sacrificing determinism or clarity.