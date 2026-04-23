# Chapter 5 Evidence Register

| ID | Asset Name | Type | Description | Claim Supported | Section | Status | Needs Cleanup | Notes |
|----|-----------|------|-------------|-----------------|---------|--------|---------------|------|
| E1 | TurnLoopController | System | Orchestrates full interaction loop (capture → plan → play → rearm) | C1, C2 | 5.5 | Implemented | No | Core interaction architecture |
| E2 | TurnLoop Tests | Test | Verifies loop closure and phase transitions | C1, C6 | 5.2, 5.5 | Implemented | No | Confirms closed-loop execution |
| E3 | HitBuffer + Capture Flow | System | Captures and segments input into turns | C1, C6 | 5.2 | Implemented | No | Input pipeline |
| E4 | PatternCompiler | System | Quantises hits into PatternTurn representation | C6 | 5.2 | Implemented | Maybe | No explicit tests |
| E5 | PatternTurnDebugRenderer | Debug | Visualises rhythmic structure (grid + hits + offsets) | C5 | 5.5 | Implemented | No | Strong figure candidate |
| E6 | TurnAnalyser | System | Extracts density, energy, anchors, SAP features | C3, C6 | 5.2, 5.3 | Implemented | No | Core analysis |
| E7 | Analysis Unit Tests | Test | Validates feature extraction correctness | C6 | 5.2 | Implemented | No | High test coverage |
| E8 | ResponsePlanner | System | Generates ResponsePlan from analysis | C3 | 5.3 | Implemented | No | Core reasoning component |
| E9 | ResponsePlanner Tests | Test | Verifies planner determinism and logic | C3, C6 | 5.2 | Implemented | No | Strong evidence |
| E10 | Planner CSV (300 cases) | Data | Batch planner outputs across synthetic inputs | C3, C4 | 5.3 | Available | Yes | Needs summarisation |
| E11 | Planner Debug Snapshot | Debug | Feature → score → decision mapping | C3 | 5.3 | Implemented | No | Interpretability evidence |
| E12 | SkeletonBuilder | System | Generates structural rhythmic skeletons | C4 | 5.4 | Implemented | No | Core generation layer |
| E13 | Skeleton Tests | Test | Validates structural constraints and behaviour | C4, C6 | 5.2, 5.4 | Implemented | No | Strong correctness |
| E14 | Skeleton Batch Outputs | Data | Large-scale structural outputs (1000+ cases) | C4 | 5.4 | Available | Yes | Used for metrics |
| E15 | Skeleton Metrics | Data | Overlap, gap-fill, anchor preservation metrics | C4 | 5.4 | Available | Yes | Quantitative evidence |
| E16 | Skeleton Debug Renderer | Debug | Visualises structural outputs | C5 | 5.5 | Implemented | No | Strong comparison figure |
| E17 | TurnLoopDebugPresenter | Debug | Displays system state and interaction phases | C5 | 5.5 | Implemented | No | Best system-level figure |
| E18 | FeatureTransformer | System | Converts skeleton into final output | P2 | 5.6 | Implemented | No | Known weak layer |
| E19 | AiResponsePreparationFlow | System | Connects planning → generation → playback | P2 | 5.6 | Implemented | No | CRITICAL limitation evidence |
| E20 | Tuning Report | Data | Planner behaviour summary (distribution, margins) | C3 | 5.3 | Available | Yes | Convert to tables |
| E21 | Variant Dataset | Data | Tests planner sensitivity to input variation | C3 | 5.3 | Available | Yes | Supports reactivity |
| E23 | Planner Metrics Suite | Metrics | Response distribution, margins, sensitivity | C3, P2 | 5.3 | Available | Yes | From tuning_report |
| E24 | Skeleton Metrics Suite | Metrics | Overlap, gap-fill, anchor preservation, structure | C4 | 5.4 | Available | Yes | Core distinctiveness |
| E25 | Curated Case Studies | Data | 3–5 representative cases | C3, C4, P2 | 5.3, 5.4 | To Be Created | Yes | See below |
| E26 | Pre-Phase Skeleton Evaluation | Data | Baseline weaknesses before tuning | C4, P2 | 5.4, 5.6 | Available | Yes | Mark as historical |
| E27 | Post-Phase Skeleton Evaluation | Data | Improved diversity + remaining issues | C4, P2 | 5.4, 5.6 | Available | Yes | Shows iteration |
| E28 | Skeleton Genericity Diagnostic | Report | Developer note identifying structural issues | P2 | 5.6 | Available | No | GOLD for limitations |
| E29 | Variant Sensitivity Analysis | Data | Measures planner response changes across variants | C3 | 5.3 | Available | Yes | Reactivity evidence |
| E30 | Planner Margin Analysis | Metrics | Measures decision confidence / ambiguity | P2 | 5.3, 5.6 | Available | Yes | Limitation evidence |
| E31 | Debug Panel Screenshots | Visual | Stored runtime debug screenshots | C5 | 5.5 | Available | Yes | Ready-to-use figures |
| E32 | Synthetic Dataset Generator | System | Controlled input generation (structure + energy) | C3 | 5.3 | Implemented | No | Justifies experiment design |
| E33 | Missing PatternCompiler Tests | Gap | No validation of quantisation logic | P2 | 5.6 | Missing | No | Must mention |
| E34 | Missing FeatureTransformer Tests | Gap | No validation of output layer | P2 | 5.6 | Missing | No | Major limitation |
| E35 | No Latency Measurement | Gap | No timing / responsiveness evaluation | P1 | 5.6 | Missing | No | Weakens real-time claim |
| E36 | Skeleton Divergence (Jaccard) | Metrics | Measures difference between response types | C4 | 5.4 | Available | Yes | STRONGEST distinctiveness metric |
| E37 | Plan vs Output Dataset | Data | Comparison of input, plan, skeleton, and final output | P2 | 5.6 | To Be Created | Yes | Core limitation evidence |
| E38 | Timing Logs | Data | Measures latency between capture, planning, and playback | P1 | 5.5, 5.6 | To Be Created | Yes | Supports real-time claim |

E25 details:
- Includes sparse/open-ending case
- Includes balanced/anchored case
- Includes dense/high-energy case
- Includes borderline planner case
- May include one limitation-focused case
- Each case should show input, analysis, ResponsePlan, skeleton, and optional final output