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