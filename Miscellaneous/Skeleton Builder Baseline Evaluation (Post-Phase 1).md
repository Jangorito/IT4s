# Post–Phase 1 Evaluation
## ResponsePlanner + SkeletonBuilder Behaviour

---

## 1. Purpose

This document evaluates the behaviour of the ResponsePlanner and SkeletonBuilder following Phase 1 tuning.

Phase 1 aimed to:
- Increase ResponseType differentiation
- Expand behavioural diversity in generated skeletons
- Improve gap-based and complementary responses
- Reduce structural collapse between response types

---

## 2. Summary of Changes (Phase 1)

Phase 1 introduced:

- Relaxation of weak-step suppression
- Increased weighting of non-metric contributors:
  - source relation (mirror vs complement)
  - gap-filling behaviour
  - density shaping
- Improved scoring separation between response types

---

## 3. Key Results

---

## 3.1 Increased Response Diversity

### Observation

All response types exhibit increased divergence scores.

### Interpretation

Phase 1 successfully:
- Increased structural variation between outputs
- Reduced uniformity across different planner intents

### Outcome

The system now produces:
> more distinct rhythmic structures across response types

---

## 3.2 Improved Complement / Contrast / Fill Behaviour

### Observation

- Overlap with source decreased
- Gap-filling behaviour increased
- Weak-step usage increased

### Interpretation

These response types now better reflect their intended roles:

- Complement → fills conversational space
- Contrast → opposes source structure
- Fill → densifies and occupies gaps

### Outcome

Clear improvement in:
- musical conversationality
- inter-response differentiation

---

## 3.3 Increased Weak-Step Participation

### Observation

Weak-step selection increased across expressive response types.

### Interpretation

Relaxing metric dominance allowed:
- more off-beat activity
- more syncopation
- greater rhythmic variation

### Outcome

Outputs are:
- less rigid
- more musically expressive

---

## 3.4 Mirror Behaviour Shift

### Observation

Mirror shows:
- increased overlap with source (expected)
- significantly increased weak-step usage (unexpected)

### Interpretation

While mirror alignment improved, the increase in weak-step activity suggests:

- metric relaxation affected mirror more than intended
- mirror may now deviate from strict structural imitation

### Outcome

Mirror is:
- more flexible
- but potentially less “pure” in identity

---

## 3.5 Simplify Stability

### Observation

Simplify behaviour remained largely unchanged:
- low weak-step usage
- strong-beat dominance
- high anchor preservation

### Interpretation

This stability indicates:
- Phase 1 changes did not disrupt conservative response modes

### Outcome

Simplify remains:
> a reliable baseline reduction strategy

---

## 3.6 Intensify Under-Responsiveness

### Observation

Intensify shows minimal change post Phase 1.

### Interpretation

Current scoring:
- does not sufficiently amplify density or energy differences
- lacks strong behavioural push

### Outcome

Intensify remains:
> insufficiently expressive compared to other response types

---

## 3.7 Partial Resolution of Mirror–Simplify Collapse

### Observation

Some separation between Mirror and Simplify is achieved, but:

- zero-distance cases still occur
- outputs can still converge structurally

### Interpretation

Planner + builder mapping still allows:
- similar structural outcomes for distinct intents

### Outcome

This issue is:
> improved but not resolved

---

## 4. System-Level Diagnosis (Post–Phase 1)

The pipeline now behaves as:


Turn → Analysis → Planner → Skeleton
↓
stronger intent signals
↓
partially expressive mapping
↓
improved but still constrained output


---

## 5. What Phase 1 Successfully Achieved

- Increased response-type divergence
- Improved gap-based behaviours (Complement / Fill / Contrast)
- Introduced controlled weak-step participation
- Maintained stability in conservative modes (Simplify)

---

## 6. Remaining Limitations

### 6.1 Mirror Over-Relaxation
- Weak-step usage may be too permissive
- Identity drift from strict mirroring

### 6.2 Intensify Weak Expression
- Insufficient density/energy amplification

### 6.3 Incomplete Response-Type Separation
- Mirror and Simplify still overlap in some cases

### 6.4 Limited Ending Behaviour Variation
- Ending shaping remains underdeveloped

---

## 7. Key Insight

> Phase 1 improved structural diversity, but expressive intent is still not fully realised in generation.

The system now distinguishes responses better,
but does not yet **fully embody their musical roles**.

---

## 8. Direction for Next Phase

Next improvements should focus on:

### Planner
- Strengthen behavioural extremes (especially Intensify)

### Builder
- Differentiate structural rules per ResponseType
- Re-balance metric vs intent influence (especially for Mirror)

### Coupling
- Tighten mapping:
  ResponseType → structural behaviour

---

## 9. Status

Phase 1 is considered:

> **successful in increasing diversity, but incomplete in achieving expressive clarity**

Further refinement is required to fully realise musically distinct responses.

---