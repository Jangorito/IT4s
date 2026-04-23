# Chapter 5 Findings Log

## F1 — Planner response distribution
**Task**: Phase 4, Task 1  
**Evidence IDs**: E10, E23  
**Sections**: 5.3  
**Figures/Tables**: Figure 5.1, Figure 5.2  

### Raw result
- Intensify: 118 (39.33%)
- Fill: 62 (20.67%)
- Mirror: 62 (20.67%)
- Complement: 36 (12.00%)
- Contrast: 22 (7.33%)

### Interpretation
- All response types are used, so planner behaviour is non-degenerate.
- Intensify is dominant, indicating a structured bias toward energy-increasing responses.
- Mirror and Fill remain substantial, suggesting meaningful behavioural variety.
- Complement and Contrast are less frequent, implying either narrower triggering conditions or under-representation in the dataset.

### Margin findings
- Margins range from 0 to 1.5 in the sample reviewed.
- High-margin cases indicate strong preference.
- Low-margin and zero-margin cases indicate ambiguity and visible decision boundaries.

### Safe claims
- The planner exhibits structured, feature-driven variation rather than collapsing to a single response mode.
- The planner exposes uncertainty through measurable score margins.
- Ambiguous cases are inspectable rather than hidden.

### Risks / limitations
- Intensify may be overrepresented.
- Need histogram/summary of full margin distribution, not just sampled rows.
- Distribution may partly reflect dataset construction, not only planner behaviour.

### Write-up use
- Supports C3 strongly.
- Supports interpretability and controlled planner bias discussion in 5.3.
- Also feeds limitations discussion in 5.6 if needed.

## F2 — Structural Response Distinctiveness

**Task**: Phase 4, Task 2  
**Evidence IDs**: E14, E24, E25, E36  
**Sections**: 5.4  
**Figures/Tables**: Figure 5.4, Figure 5.5, Table 5.4  

---

### Aggregate results
- Response types exhibit distinct overlap/gap-fill profiles:
  - Mirror / Simplify: higher overlap
  - Complement / Contrast / Fill: higher gap-fill
- Anchor preservation varies:
  - High for Mirror, Simplify, Intensify
  - Lower for Complement, Contrast
- Mean divergence values are high across all types (~0.53–0.70), indicating strong structural separation.

---

### Pairwise divergence findings
- Simplify is the most structurally distinct response type:
  - Highest divergence against Fill (0.77), Intensify (0.73), and Contrast (0.72)
- Fill and Intensify form the closest pair (0.38), indicating similar expansion behaviour
- Complement and Contrast also cluster together, reflecting shared gap-oriented behaviour
- Mirror is closest to Intensify among source-related responses

---

### Case-level insights
- Structural behaviour varies significantly depending on input pattern type
- Some cases preserve anchors across all response types despite differing overlap/gap profiles
- Simplify can be maximally source-preserving yet highly divergent due to reduced step count
- Identical aggregate metrics do not imply identical step structures
- No cases exhibit complete identity or complete disjointness (no Jaccard 0 or 1 globally)

---

### Outliers / edge cases
- High-divergence cases consistently involve Simplify vs gap-oriented responses
- Low-divergence cases occur in dense/even patterns where step sets converge
- Complement and Contrast frequently collapse to identical aggregate metrics
- Intensify and Fill occasionally produce near-identical summaries but still differ structurally

---

### Safe claims
- Response types produce structurally distinct outputs rather than collapsing to a single mode
- Structural differences align with intended response semantics
- Distinctiveness is consistent but context-sensitive
- Structural behaviour can be analysed both globally and at the case level

---

### Risks / limitations
- Complement and Contrast may not be sufficiently distinct in some cases
- Intensify and Fill may overlap structurally in dense regions
- Aggregate metrics can obscure underlying structural differences
- Behaviour depends partly on synthetic dataset construction

---

### Write-up use
- Core evidence for Section 5.4 (Structural Response Distinctiveness)
- Supports Figure 5.4 (case comparisons)
- Supports Figure 5.5 (pairwise divergence)
- Feeds limitations discussion in Section 5.6

4. Marker-winning insights (DO NOT MISS THESE)

These are the things examiners LOVE:

🔥 Insight 1 — “Identical metrics ≠ identical structure”

You already saw:

Complement and Contrast can have identical overlap/gap/anchor values

BUT:

Jaccard ≠ 0
→ structures are still different

👉 This is a top-tier observation

🔥 Insight 2 — “Simplify is structurally extreme”
High overlap
Low density
High divergence

👉 It behaves like a structural compression operator, not just a reduced Mirror

🔥 Insight 3 — “Clusters of behaviour exist”

You have natural groupings:

{Mirror, Simplify} → source-aligned
{Complement, Contrast} → gap-oriented
{Intensify, Fill} → expansion

👉 That’s emergent structure, not hard-coded categories

🔥 Insight 4 — “No degenerate outputs”
No Jaccard = 0
No Jaccard = 1

👉 Every response:

shares something
differs something

This is exactly what you want in an interactive system


## F3 — Variant Sensitivity and Reactivity

**Task**: Phase 4, Task 3  
**Evidence IDs**: E21, E29  
**Sections**: 5.3  

---

### Aggregate results
- Total paired cases: 150
- Changed response type: 35
- Percentage changed: 23.33%

---

### Interpretation
- The planner exhibits controlled sensitivity to input variation.
- Approximately one in four cases results in a different response type under small input changes.
- This indicates that the system is neither rigid nor unstable.

---

### Feature-dependent sensitivity
- AnchorLed structures: 40% change
- Even structures: 13.33% change
- Increasing energy profiles: 30% change
- Accented endings: 16% change

This suggests that responsiveness depends on structural and energetic characteristics of the input.

---

### Response transition behaviour
- Certain transitions occur consistently:
  - Fill → Intensify (100%)
  - Mirror → Intensify
  - Complement → Mirror
- These transitions indicate structured decision boundaries rather than random switching.

---

### Margin behaviour
- Many changed cases involve low-margin base decisions that become higher-margin in variants.
- This suggests that input variation resolves decision ambiguity.

---

### Safe claims
- The planner is responsive to input variation.
- Responsiveness is feature-dependent and structured.
- The system exhibits interpretable decision transitions rather than random behaviour.

---

### Risks / limitations
- Majority of cases (76.67%) remain unchanged.
- Some response types (e.g. Intensify) show strong stability.
- Sensitivity may depend on synthetic dataset design.

---

### Write-up use
- Core evidence for reactivity in Section 5.3
- Supports interpretability and decision-boundary arguments
- Connects directly to planner margin analysis

## F4 — Plan vs Output Fidelity

**Task**: Phase 4, Task 5  
**Sections**: 5.6  

---

### Aggregate results
- Mean density error: 0.048
- Mean gap mismatch: 0.149
- Mean anchor mismatch: 0.578

---

### Interpretation
- Density is reproduced accurately across response types.
- Gap/complementarity behaviour is moderately aligned.
- Anchor preservation behaviour is poorly aligned.

---

### Response-type behaviour
- Mirror and Complement show strong alignment between plan and output.
- Intensify and Fill show severe anchor mismatch (~0.87).
- This indicates that anchor intent is not enforced in generation.

---

### Key insight
- The system demonstrates correct high-level planning but inconsistent low-level realisation.
- Planner intent is not reliably translated into output structure.

---

### Safe claims
- The planner is expressive and interpretable.
- The generation layer is only partially faithful to planner intent.
- Some structural constraints (e.g. density) are easier to realise than others (e.g. anchors).

---

### Risks / limitations
- Anchor behaviour is not enforced in the generator.
- Complementarity is only loosely controlled.
- Evaluation is limited to structural metrics, not perceptual quality.

---

### Write-up use
- Core evidence for Section 5.6 (Limitations)
- Supports critique of FeatureTransformer / generation layer
- Provides justification for future ML-based realiser