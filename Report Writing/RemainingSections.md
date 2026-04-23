# Dissertation Structure — Remaining Chapters (Topline Plan)

This document outlines the high-level structure and purpose of the remaining dissertation chapters following Chapter 3 (System Design). It is intended as a planning scaffold to guide writing and ensure clear separation of concerns across chapters.

---

# Chapter 4 — Implementation

## Purpose
To describe how the system design (Chapter 3) was realised in practice. This chapter focuses on **technical execution**, not design justification.

---

## 4.1 Implementation Overview
- Brief mapping from design (Chapter 3) to implementation
- Overview of system components:
  - Unity (orchestration)
  - Bela (input sensing)
  - OSC communication
  - ChucK (audio playback)

---

## 4.2 Unity System
- Structure of Unity project
- Key scripts and modules:
  - TurnLoopController
  - Data handling (PatternTurn, buffers)
  - Flow of execution within Unity
- Interaction between components

---

## 4.3 Bela Integration
- Piezo sensor setup
- Signal processing pipeline:
  - thresholding
  - peak detection
  - refractory handling
- Hit event generation

---

## 4.4 OSC Communication Layer
- Message format (`/it4/hit`)
- Timestamp handling (split 64-bit → reconstruction)
- Transmission pipeline (Bela → Unity)
- Reliability considerations

---

## 4.5 ChucK Audio Integration
- Role of ChucK in playback
- Scheduling events using sample timing
- Mapping PatternTurn → playback events

---

## 4.6 Debugging and Tooling
- Debug UI components:
  - PatternTurn visualisation
  - planner/skeleton debug panels
- Runtime observability
- Development workflow and testing approach

---

## Key Rule for Chapter 4
- DO: explain *how it is built*
- DO NOT: re-justify design decisions (already done in Chapter 3)

---

# Chapter 5 — Evaluation

## Purpose
To critically assess the system in terms of **functionality, interaction quality, and limitations**.

---

## 5.1 Evaluation Overview
- What is being evaluated
- Criteria:
  - responsiveness
  - correctness
  - musicality
  - interaction quality

---

## 5.2 Functional Evaluation
- Does the system work as intended?
- End-to-end pipeline:
  - input → capture → response → playback
- Stability and reliability

---

## 5.3 Interaction and Musical Behaviour
- Responsiveness to performer input
- Variation in responses
- Structural coherence of output
- Does the system behave like an interaction partner?

---

## 5.4 Response Quality Analysis
- Behaviour of different response types
- Strengths and weaknesses of generated outputs
- Observations from testing

---

## 5.5 Limitations
- Flat or predictable responses
- Lack of tempo adaptation
- No long-term memory across turns
- Constraints of rule-based generation
- Symbolic representation limitations

---

## 5.6 Reflection on Design Decisions
- What worked well
- What did not work as expected
- Trade-offs observed in practice

---

## Key Rule for Chapter 5
- Be **critical and analytical**
- Avoid purely descriptive writing

---

# Chapter 6 — Conclusion and Future Work

## Purpose
To summarise contributions, reflect on outcomes, and define future directions.

---

## 6.1 Summary of Work
- What was built
- Key system components
- Research problem revisited

---

## 6.2 Contributions
- Closed-loop interaction architecture
- Symbolic rhythmic representation
- Response planning and generation pipeline

---

## 6.3 Limitations (Recap)
- Key system constraints
- Gaps identified in evaluation

---

## 6.4 Future Work
Potential extensions:
- Adaptive BPM / tempo inference
- Improved response generation (stochastic / ML-based)
- Multi-turn memory and context awareness
- Better use of microtiming
- Expressive performance modelling

---

## Key Rule for Chapter 6
- Be concise and reflective
- Emphasise what has been learned and achieved

---

# Overall Flow Reminder

- Chapter 3 → Design (WHY and HOW it should work)
- Chapter 4 → Implementation (HOW it was built)
- Chapter 5 → Evaluation (HOW WELL it works)
- Chapter 6 → Conclusion (WHAT it means and WHAT’S NEXT)

---

# Writing Principle

Each chapter must answer a different question:

- Chapter 4: *How did I build it?*
- Chapter 5: *How well does it work?*
- Chapter 6: *What did I achieve and what next?*

Avoid overlapping these responsibilities.