# Turn Analysis Implementation Plan (Codex Workflow)

This document defines a structured, chunked approach for implementing the Turn Analysis system using Codex. Each section represents a self-contained implementation phase with clear scope and deliverables.

---

## Section 0 — Foundations / Contracts

### Goal
Establish all shared data structures, enums, and utilities required by the Turn Analysis system.

### Scope
- TurnAnalysisResult
- Feature data classes:
  - DensityFeatures
  - EnergyFeatures
  - AnchorFeatures
  - EndActivityFeatures
- Shared enum:
  - ActivityShape
- Shared utilities:
  - Segment partitioning helper (fixed 4 segments)
- Optional:
  - Analyzer interface or pipeline structure

### Deliverable
- Compilable codebase with all analysis contracts defined
- No actual analysis logic implemented yet

---

## Section 1 — Density

### Goal
Implement occupancy-based analysis of the step grid.

### Scope
- DensityFeatures
- DensityAnalyzer
- Global density (StepDensity)
- Segment densities (4 segments)
- Edge case handling

### Key Rules
- Active step = velocity > 0
- Multiple hits per step count as one active step
- Segment count fixed at 4
- Deterministic output

### Deliverable
- PatternTurn → DensityFeatures working
- Unit tests covering:
  - empty turn
  - sparse patterns
  - dense patterns
  - uneven segment sizes

---

## Section 2 — Energy

### Goal
Implement velocity-based intensity analysis.

### Scope
- EnergyFeatures
- EnergyThresholds
- EnergyAnalyzer
- Global metrics:
  - MeanVelocity
  - PeakVelocity
  - VelocityVariance
- Segment mean velocities (4 segments)
- Derived traits:
  - HighEnergy / LowEnergy
  - FlatEnergy
  - Accented
  - Crescendo / Decrescendo

### Key Rules
- Only active steps contribute to calculations
- Segment logic must reuse shared partition helper
- Traits derived after raw metrics

### Deliverable
- PatternTurn → EnergyFeatures working
- Unit tests for:
  - no hits
  - single hit
  - dynamic variation
  - crescendo/decrescendo cases

---

## Section 3 — Anchor Detection

### Goal
Identify structurally salient steps using a weighted salience model.

### Scope
- AnchorFeatures
- AnchorAnalyzer
- Salience components:
  - VelocityScore
  - LocalAccentScore
  - IsolationScore
  - PositionalScore
- Fixed weights and threshold
- Per-step outputs:
  - salience scores
  - anchor flags
- Aggregate outputs:
  - AnchorCount
  - AnchorIndices
  - StrongestAnchor
  - Opening/Closing anchors
  - AnchorCountsPerSegment

### Key Rules
- Deterministic
- O(N)
- Inactive steps always score 0
- Earliest index wins on ties

### Deliverable
- PatternTurn → AnchorFeatures working
- Unit tests for:
  - isolated hits
  - dense patterns
  - edge boundaries
  - tie-breaking behaviour

---

## Section 4 — End Activity

### Goal
Capture phrase-ending behaviour using the final portion of the turn.

### Scope
- EndActivityFeatures
- EndActivityAnalyzer
- End window = final 25% of steps
- Metrics:
  - EndDensity
  - EndEnergy
  - EndAccent

### Key Rules
- Window defined as floor(N * 0.75)
- Only scan end window
- No active steps → zero outputs

### Deliverable
- PatternTurn → EndActivityFeatures working
- Unit tests for:
  - empty turn
  - no end activity
  - strong final accent
  - sustained endings

---

## Section 5 — Segment Activity Profile (SAP)

### Goal
Derive high-level shape classifications from segment-based density and energy.

### Scope
- Shared enum:
  - ActivityShape
    - Flat
    - Increasing
    - Decreasing
    - FrontLoaded
    - BackLoaded
    - MidPeak
    - MidDip
- Classification logic for:
  - DensityShape
  - EnergyShape
- Epsilon-tolerant comparisons
- Ordered heuristic rules

### Key Rules
- Derived feature only (no raw pattern scanning)
- Uses segment outputs from Density and Energy
- Deterministic classification
- No combined density+energy shape in v1

### Deliverable
- DensityFeatures + EnergyFeatures → SAP output
- Unit tests for:
  - flat profiles
  - increasing/decreasing
  - mid-peak/mid-dip
  - noisy edge cases

---

## Section 6 — Turn Analysis Orchestrator + Debug

### Goal
Assemble the full Turn Analysis pipeline and enable inspection.

### Scope
- TurnAnalyzer / TurnAnalysisPipeline
- Orchestration:
  - Density
  - Energy
  - Anchors
  - End Activity
  - SAP
- Construct TurnAnalysisResult
- Optional:
  - Debug formatter / presenter
  - Integration with Unity debug UI

### Key Rules
- No hidden logic outside analyzers
- Pure orchestration layer
- Maintain immutability of results

### Deliverable
- PatternTurn → TurnAnalysisResult end-to-end
- Integration test with real PatternTurn input
- Debug output showing:
  - density values
  - energy values
  - SAP shape
  - anchors
  - end activity

---

## Summary

The implementation should proceed sequentially:

1. Foundations / Contracts  
2. Density  
3. Energy  
4. Anchor Detection  
5. End Activity  
6. Segment Activity Profile  
7. Orchestrator + Debug  

Each section must be:
- fully implemented
- tested
- reviewed

before moving to the next.

The Turn Analysis system should only be considered complete once the full pipeline is working end-to-end and its outputs are inspectable.