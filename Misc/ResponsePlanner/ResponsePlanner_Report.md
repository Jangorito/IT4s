# Response Planner – Design Rationale (Report-Facing)

## Overview
The ResponsePlanner is responsible for transforming analysed rhythmic input into a structured musical intention. Rather than directly generating notes, it defines how the system should respond.

## Purpose
This layer introduces a separation between analysis and generation, enabling more human-like musical behaviour. It allows the system to interpret input before producing output.

## Core Concept
The planner outputs a ResponsePlan, which encodes intent rather than raw musical data.

## ResponsePlan Structure
- ResponseType (interaction style)
- TargetDensity
- TargetEnergy
- PreserveAnchors
- MirrorEnding
- VariationAmount
- SyncopationBias
- TurnLengthSteps

## Response Types
- MIRROR: Repeats input with variation
- COMPLEMENT: Fills gaps or contrasts
- SIMPLIFY: Reduces complexity
- INTENSIFY: Increases density/energy
- CONTRAST: Opposes structure
- FILL: Phrase-ending response

## Design Justification
- Separates musical reasoning from generation
- Enables expressive variability
- Supports future ML integration
- Improves interpretability for evaluation

## Behavioural Flow
1. Receive TurnAnalysisResult
2. Decide ResponseType
3. Derive parameters
4. Apply controlled randomness
5. Output ResponsePlan

## Key Contribution
This component enables the system to behave as an interactive musical agent rather than a reactive pattern generator.
