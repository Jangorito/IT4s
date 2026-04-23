# Chapter 5 Production Workflow & Evidence Plan

## Intelli-Trading Fours Dissertation (Evaluation Chapter)

---

# 0. Purpose of This Document

This document defines a **strict, executable workflow** for producing Chapter 5 (Evaluation).

It is designed to:

* Eliminate vague writing and “hand-waving”
* Force **evidence-first reasoning**
* Ensure **complete coverage of claims**
* Make the chapter reproducible by any agent or collaborator

---

# 1. Core Evaluation Philosophy

## 1.1 What Chapter 5 IS

> A structured, evidence-driven technical investigation of whether the system behaves as designed.

The system is evaluated as:

* an **interaction architecture**
* a **closed-loop musical system**
* a **real-time responsive pipeline**

---

## 1.2 What Chapter 5 is NOT

* Not a narrative reflection
* Not a feature description
* Not a list of tests
* Not a claim of musical “quality” without evidence

---

## 1.3 Evaluation Lens (CRITICAL)

All evaluation must answer:

> Does the system demonstrate **meaningful, interpretable, responsive interaction**, rather than just output generation?

This aligns with co-creative system research, where:

* systems must **relate to user input to avoid appearing random** 
* interaction quality depends on **understandability and responsiveness**, not just output

---

# 2. Chapter 5 Global Workflow

## DO NOT SKIP OR REORDER THESE PHASES

---

# Phase 0 — Claim Ledger (MANDATORY START)

## Goal

Define EXACTLY what the chapter proves.

---

## Output Format

### Strongly Evidenced Claims

* Fully supported by existing system + data

### Partially Evidenced Claims

* Some evidence exists, but incomplete

### Explicitly Deferred Claims

* Not evaluated (must be clearly stated)

---

## Example Categories (pre-filled baseline)

### Strong

* Closed-loop interaction exists
* Turn-taking architecture works
* Planner produces interpretable outputs
* Structural response differences exist

### Partial

* Real-time responsiveness (needs timing evidence)
* Plan → output alignment
* Musical plausibility

### Deferred

* User studies
* Long-term adaptation
* Full expressive generation

---

## GATE

No writing begins until ALL claims are categorised.

---

# Phase 1 — Evidence Register

## Goal

Track every usable asset.

---

## Required Table

| ID | Asset | Type | Claim | Status | Section | Notes |
| -- | ----- | ---- | ----- | ------ | ------- | ----- |

---

## Asset Types

### Tests

* unit tests (analysis, planner, skeleton)
* integration tests (loop)

### Batch Data

* planner CSV
* skeleton outputs
* tuning logs

### Debug Tools

* PatternTurn visualiser
* Planner debug UI
* Skeleton debug UI
* TurnLoop state UI

### Historical Evidence

* pre/post tuning comparisons

### Report Evidence

* interim report claims (scope + limitations)

---

## GATE

If it’s not in the register → it cannot be used.

---

# Phase 2 — Section Planning Grid

Each section must be planned BEFORE writing.

---

## Required Template

| Section | Question | Claims | Evidence IDs | Figures | Tables | Missing |
| ------- | -------- | ------ | ------------ | ------- | ------ | ------- |

---

## GATE

No section is drafted without:

* at least 1 evidence source
* at least 1 figure/table (unless justified)

---

# Phase 3 — Visual-First Design

## Principle

> Figures and tables define the chapter. Text explains them.

---

## Required Artefacts

### Tables

1. Evaluation Questions Mapping
2. Subsystem Summary
3. Planner Case Table
4. Skeleton Metrics Summary
5. Claim Evidence Table

### Figures

1. Planner distribution
2. Planner margin/confidence
3. Planner traceability example
4. Skeleton comparisons
5. Runtime debug screenshot
6. PatternTurn visualisation
7. Planner debug panel

---

## GATE

Every section must reference at least one artefact.

---

# Phase 4 — Addition Decisions

Identify missing evidence BEFORE writing.

---

## Required Decisions

### A. Plan → Output Alignment

Compare:

* input
* plan
* skeleton
* final output

### B. Timing Evidence

Measure:

* capture → plan → playback latency

### C. Case Studies

Select 3–5 representative examples

---

## For each:

* DO
* DON’T
* DEFER

---

## GATE

No “implied evaluation” allowed — decisions must be explicit.

---

# Phase 5 — Asset Pack Assembly

## Goal

Create a single source of truth.

---

## Must Contain

* selected datasets
* selected figures
* selected cases
* selected screenshots
* selected limitations evidence

---

## GATE

No drafting from memory — only from Asset Pack.

---

# Phase 6 — Drafting Order (STRICT)

1. 5.1 Framework
2. 5.2 Subsystems
3. 5.3 Planner (deep)
4. 5.4 Skeleton (deep)
5. 5.5 Runtime
6. 5.6 Limitations
7. 5.7 Synthesis

---

## Rule

Core sections (5.3, 5.4) must be strongest.

---

# Phase 7 — Red-Team Pass

## Checklist

For every paragraph:

* Is the claim classified (strong/partial/deferred)?
* Is evidence explicitly referenced?
* Is this describing design (should be Chapter 3)?
* Is this overclaiming musical quality?
* Should this be a figure instead?

---

## GATE

Unjustified claims are removed or downgraded.

---

# 3. Chapter Structure (Final)

## 5.1 Evaluation Framework

Define scope, claims, and evaluation approach.

## 5.2 Subsystem Correctness

Verify pipeline components function correctly.

## 5.3 Planner Behaviour

Evaluate interpretability and decision logic.

## 5.4 Structural Response Distinctiveness

Evaluate generated structures (not audio quality).

## 5.5 Closed-Loop Execution

Evaluate real-time loop and observability.

## 5.6 Limitations

Explicit gaps between architecture and output.

## 5.7 Synthesis

What is proven vs not proven.

---

# 4. Critical Writing Rules

## Rule 1 — Evidence First

No claim without explicit evidence.

---

## Rule 2 — No README Writing

Descriptions must support arguments.

---

## Rule 3 — No Output Overclaiming

System is NOT a finished musical agent.

---

## Rule 4 — Always Separate Layers

* Plan ≠ Skeleton ≠ Output

---

## Rule 5 — Interaction > Generation

Focus on responsiveness and structure.

---

# 5. Theoretical Anchoring (for justification)

Use literature to justify evaluation framing:

* Co-creative systems must maintain **relation to user input** 
* Call-and-response is fundamental to musical interaction structure 
* Interactive systems require **real-time adaptation and coordination** 

---

# 6. Final Execution Summary

## Workflow

1. Build Claim Ledger
2. Build Evidence Register
3. Plan Sections
4. Design Figures/Tables
5. Decide Additions
6. Assemble Asset Pack
7. Draft in Order
8. Red-team pass

---

## Core Principle

> This chapter does not “argue quality” —
> it **demonstrates system behaviour under controlled evidence**.

---

END OF DOCUMENT
