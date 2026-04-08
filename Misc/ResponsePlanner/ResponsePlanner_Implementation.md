# Response Planner – Implementation Specification (Codex-Facing)

## Purpose
Convert TurnAnalysisResult into a ResponsePlan.

## Input
TurnAnalysisResult:
- Density
- Energy
- Anchors
- EndActivity
- SegmentActivityProfile

## Output
ResponsePlan:
- ResponseType Type
- float TargetDensity
- float TargetEnergy
- bool PreserveAnchors
- bool MirrorEnding
- float VariationAmount
- float SyncopationBias
- int TurnLengthSteps

## Core API
ResponsePlan Plan(TurnAnalysisResult analysis)

## Processing Steps
1. DecideType(analysis)
2. BuildPlan(type, analysis)
3. ApplyStochasticVariation(plan)

## Response Type Rules (initial baseline)
- If EndActivity.IsActive → FILL
- If Density < 0.25 → INTENSIFY
- If Density > 0.75 → SIMPLIFY
- If Anchors strong → MIRROR
- Else → COMPLEMENT

## Parameter Derivation
Each ResponseType defines:
- Density adjustment
- Energy adjustment
- Anchor handling
- Variation level
- Syncopation bias

## Stochastic Control
- Small bounded randomness applied to parameters
- All outputs clamped to valid ranges [0,1]

## Constraints
- Deterministic baseline must exist
- No direct pattern generation here
- No duplication of analysis logic

## Extensibility
- Replace DecideType with weighted model
- Integrate ML-based planners
- Add context/history awareness

## Notes
This system must remain lightweight, testable, and modular.
