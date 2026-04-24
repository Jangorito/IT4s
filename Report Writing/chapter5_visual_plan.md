# Chapter 5 Visual Plan

---

## Figure 5.1 — Planner Response Distribution

**Section**  
5.3

**Purpose**  
Show how often each ResponseType is selected across dataset

**Data Source**  
E10 (Planner CSV), E23 (Planner Metrics)

**Format**  
Bar chart:
- X-axis: ResponseType
- Y-axis: Count

**Claim Supported**  
C3 (planner behaviour)

---

## Figure 5.2 — Planner Decision Confidence (Margin)

**Section**  
5.3

**Purpose**  
Show how confident planner decisions are

**Data Source**  
E30 (Planner Margin Analysis)

**Format**  
Histogram or box plot:
- X-axis: margin value
- Y-axis: frequency

**Claim Supported**  
P2 (planner ambiguity / limitation)

---

## Figure 5.3 — Planner Traceability Example

**Section**  
5.3

**Purpose**  
Show full feature → decision mapping

**Data Source**  
E11 (Planner Debug Snapshot), E25 (Cases)

**Format**  
Annotated screenshot or diagram:
- input summary
- features
- score breakdown
- chosen ResponseType

**Claim Supported**  
C3 (interpretability)

---

## Figure 5.4 — Skeleton Response Comparison

**Section**  
5.4

**Purpose**  
Show structural difference between response types

**Data Source**  
E25 (Cases), E16 (Skeleton Debug)

**Format**  
Grid:
- rows = ResponseTypes
- columns = time steps
- marks = hits

**Claim Supported**  
C4 (distinctiveness)

---

## Figure 5.5 — Structural Divergence Illustration

**Section**  
5.4

**Purpose**  
Show difference between two response outputs

**Data Source**  
E36 (Jaccard), E14 (Batch Outputs)

**Format**  
Side-by-side comparison + similarity score

**Claim Supported**  
C4

---

## Figure 5.6 — System Runtime Debug View

**Section**  
5.5

**Purpose**  
Show system in operation

**Data Source**  
E31 (Screenshots), E17

**Format**  
Screenshot with labels:
- phase
- input
- output

**Claim Supported**  
C1, C2, C5

---

## Figure 5.7 — PatternTurn Representation

**Section**  
5.5

**Purpose**  
Show how rhythm is represented internally

**Data Source**  
E5

**Format**  
Grid diagram

**Claim Supported**  
C5

---

## Figure 5.8 — Plan vs Output Comparison ⭐

**Section**  
5.6

**Purpose**  
Show mismatch between intended and actual output

**Data Source**  
E37 (Plan vs Output), E25

**Format**  
4 stacked rows:
1. Input
2. Plan (abstract)
3. Skeleton
4. Final Output

**Claim Supported**  
P2 (critical limitation)

---

## Figure 5.9 — Timing / Latency Breakdown

**Section**  
5.5 or 5.6

**Purpose**  
Show real-time performance

**Data Source**  
E38 (Timing Logs)

**Format**  
Simple table or bar chart:
- capture → plan
- plan → playback

**Claim Supported**  
P1


---

## Table 5.1 — Evaluation Mapping

Section: 5.1  
Already designed

---

## Table 5.2 — Subsystem Summary

Section: 5.2

Columns:
- subsystem
- evidence
- verified behaviour
- claim

---

## Table 5.3 — Planner Case Studies

Section: 5.3

Columns:
- case ID
- input profile
- key features
- response type
- notes

---

## Table 5.4 — Skeleton Metrics Summary

Section: 5.4

Columns:
- response type
- overlap
- gap-fill
- anchor preservation
- divergence

---

## Table 5.5 — Claim Evaluation Summary

Section: 5.6

Columns:
- claim
- evidence strength (strong/partial/deferred)
- reason