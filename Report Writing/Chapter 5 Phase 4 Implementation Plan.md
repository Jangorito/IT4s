# Chapter 5 — Phase 4 Implementation Plan

## Intelli-Trading Fours Dissertation

---

# 0. Purpose

This document defines the **exact execution plan** for building all evaluation artefacts in Chapter 5.

It translates:

* the **Visual Plan (Phase 3)** 
* the **Evidence Register (Phase 1)**
* the **Section Plan (Phase 2)**

into a **step-by-step build pipeline**.

---

# 1. Core Principle

> Build in **dependency order**, not section order.

This prevents:

* duplicated work
* missing data
* broken figures

---

# 2. Build Pipeline Overview

## Stage Order

1. Data Extraction
2. Curated Case Construction
3. Missing Evidence (Additions)
4. Figure Generation
5. Table Generation

---

# 3. Stage 1 — Data Extraction (Foundation)

## Task 1 — Planner CSV Processing

**Inputs**

* E10 (Planner CSV)
* E23 (Planner Metrics)

**Extract**

* ResponseType counts
* Margin values

**Outputs**

* planner_distribution.csv
* planner_margins.csv

**Used by**

* Figure 5.1
* Figure 5.2

---

## Task 2 — Skeleton Metrics Extraction

**Inputs**

* E14 (Skeleton Outputs)
* E24 (Skeleton Metrics Suite)
* E36 (Jaccard Divergence)

**Extract**

* overlap
* gap-fill
* anchor preservation
* divergence

**Outputs**

* skeleton_metrics_summary.csv

**Used by**

* Table 5.4
* Figure 5.5

---

## Task 3 — Variant Sensitivity Analysis

**Inputs**

* E29 (Variant Dataset)

**Extract**

* % of cases where ResponseType changes

**Outputs**

* planner_variant_analysis.csv

**Used by**

* Section 5.3

---

# 4. Stage 2 — Curated Case Construction

## Task 4 — Build E25 (Curated Case Set)

Select **3–5 representative cases**:

1. Sparse / open ending
2. Balanced / anchored
3. Dense / high-energy
4. Borderline planner decision
5. (optional) limitation case

---

### For each case, store:

* input PatternTurn
* extracted features
* ResponsePlan
* skeleton output
* final output (optional)

---

### Outputs

* case_01.json
* case_02.json
* case_03.json

---

### Used by

* Figure 5.3
* Figure 5.4
* Figure 5.8
* Table 5.3

---

# 5. Stage 3 — Missing Evidence (Additions)

## Task 5 — Plan → Output Dataset (E37)

**For each curated case:**

Compute:

* density (input vs output)
* overlap
* structural differences

---

### Output

* plan_vs_output.csv

---

### Used by

* Figure 5.8
* Section 5.6

---

## Task 6 — Timing Logs (E38)

**Capture timestamps for:**

* capture end
* analysis complete
* planning complete
* playback triggered

---

### Output

* timing_log.csv

---

### Used by

* Figure 5.9

---

# 6. Stage 4 — Figure Generation

## Task 7 — Figure 5.1 (Planner Distribution)

Input: planner_distribution.csv
Output: bar chart

---

## Task 8 — Figure 5.2 (Planner Margin)

Input: planner_margins.csv
Output: histogram / box plot

---

## Task 9 — Figure 5.3 (Planner Traceability)

Input: curated case
Output: annotated diagram

---

## Task 10 — Figure 5.4 (Skeleton Comparison)

Input: curated cases
Output: grid visual

---

## Task 11 — Figure 5.5 (Divergence)

Input: skeleton metrics
Output: comparison diagram

---

## Task 12 — Figure 5.6 (Runtime Debug)

Input: debug screenshots
Output: labelled system view

---

## Task 13 — Figure 5.7 (PatternTurn Representation)

Input: debug renderer
Output: grid diagram

---

## Task 14 — Figure 5.8 (Plan vs Output)

Input: plan_vs_output.csv + cases
Output: stacked diagram

---

## Task 15 — Figure 5.9 (Timing)

Input: timing_log.csv
Output: simple chart/table

---

# 7. Stage 5 — Table Generation

## Task 16 — Table 5.3 (Planner Cases)

Columns:

* case ID
* input profile
* key features
* response type
* notes

---

## Task 17 — Table 5.4 (Skeleton Metrics)

Columns:

* response type
* overlap
* gap-fill
* anchor preservation
* divergence

---

## Task 18 — Table 5.5 (Claim Summary)

Columns:

* claim
* evidence strength
* justification

---

# 8. Execution Rules

## Rule 1 — No Coding Before Design

Only implement after confirming data purpose.

---

## Rule 2 — No Orphan Data

Every dataset must feed at least one figure or table.

---

## Rule 3 — Minimal Scope

Do not expand beyond defined tasks.

---

## Rule 4 — Evidence First

All outputs must directly support claims.

---

# 9. Final Workflow Summary

1. Extract data
2. Build curated cases
3. Fill missing evidence
4. Generate figures
5. Generate tables
6. Write Chapter 5

---

# 10. Key Insight

> This phase is not “implementation work” —
> it is the execution of a **controlled evaluation study**.

---

END OF DOCUMENT
