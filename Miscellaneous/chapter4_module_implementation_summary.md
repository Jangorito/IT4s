# Chapter 4 Module Implementation Summary

Scope: `TurnAnalysis`, `ResponsePlanner`, `SkeletonBuilder`, `FeatureTransformer`, and their live integration path.

## 1. Module Summary Table

| Module | Main files/classes | Runtime status | Input data structure(s) | Output data structure(s) | Core responsibility | Algorithmic / heuristic approach | Affects audible output? | Evidence references |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| TurnAnalysis | `TurnAnalyser`, `TurnAnalysisResult`, `DensityAnalyser`, `EnergyAnalyser`, `AnchorAnalyser`, `EndActivityAnalyser`, `SegmentActivityProfileAnalyser` | LIVE | `PatternTurn` | `TurnAnalysisResult` | Extract descriptor set from the captured human turn for downstream reasoning | Rule-based feature extraction over step occupancy, velocities, segment partitions, anchors, and end-region activity | Indirect | [TurnAnalyser.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/TurnAnalysis/TurnAnalyser.cs:30), [DensityAnalyser.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/TurnAnalysis/DensityAnalyser.cs:10), [EnergyAnalyser.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/TurnAnalysis/EnergyAnalyser.cs:17), [AnchorAnalyser.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/TurnAnalysis/AnchorAnalyser.cs:21), [EndActivityAnalyser.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/TurnAnalysis/EndActivityAnalyser.cs:9), [SegmentActivityProfileAnalyser.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/TurnAnalysis/SegmentActivityProfileAnalyser.cs:17) |
| ResponsePlanner | `ResponsePlanner`, `PlanningContext`, `ResponsePlan`, `ResponsePlannerConfig`, `ResponsePlannerDebugSnapshot` | LIVE | `TurnAnalysisResult` | `ResponsePlan` | Convert analysed source-turn features into a response strategy | Thresholded context derivation, weighted scoring of response types, deterministic tie resolution, plan parameter derivation with clamping | Indirect | [ResponsePlanner.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/Response/ResponsePlanner.cs:27), [ResponsePlan.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/Response/ResponsePlan.cs:7), [ResponsePlannerConfig.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/Response/ResponsePlannerConfig.cs:7), [ResponsePlannerTests.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/ResponsePlannerTests.cs:13) |
| SkeletonBuilder | `SkeletonBuilder`, `SkeletonBuildRequest`, `SkeletonPattern`, `SkeletonStepMeta`, `SkeletonPatternSummary`, `SkeletonDebugSnapshot`, `SkeletonBuilderConfig` | LIVE-PROTOTYPE | `SkeletonBuildRequest` containing `ResponsePlan`, `PatternTurn`, `TurnAnalysisResult`, timing/config data | `SkeletonPattern` | Produce a scored structural step-selection pattern from the planned response strategy | Per-step score-map construction, candidate ranking, spacing/ending/profile constraints, density-targeted selection, debug snapshot generation | No direct effect | [SkeletonBuilder.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/Response/SkeletonBuilder.cs:28), [SkeletonBuildRequest.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/Response/SkeletonBuildRequest.cs:7), [SkeletonPattern.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/Response/SkeletonPattern.cs:7), [SkeletonBuilderTests.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/SkeletonBuilderTests.cs:83) |
| FeatureTransformer | `FeatureTransformer` | LIVE | `PatternTurn` | `PatternTurn` | Generate the current audible AI response turn by transforming the source turn directly | Simple hand-coded transformations selected by gap/occupancy heuristics or explicit mode | Yes | [FeatureTransformer.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Rhythm/Transformations/FeatureTransformer.cs:46), [AiResponsePreparationFlow.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/AiResponsePreparationFlow.cs:110), [IT4ChuckTurnPlayer.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/ChunityAudio/IT4ChuckTurnPlayer.cs:80), [AiResponsePreparationFlowTests.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/AiResponsePreparationFlowTests.cs:19) |

## 2. Data Flow Summary

### Intended reasoning path

`PatternTurn`  
`-> TurnAnalyser`  
`-> TurnAnalysisResult`  
`-> ResponsePlanner`  
`-> ResponsePlan`  
`-> SkeletonBuilder`  
`-> SkeletonPattern`  
`-> future / deferred realiser`

### Current audible path

`PatternTurn`  
`-> FeatureTransformer`  
`-> PatternTurn`  
`-> IT4ChuckTurnPlayer`  
`-> ChucK playback`

### Split point

Split occurs in [`AiResponsePreparationFlow.Prepare(...)`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/AiResponsePreparationFlow.cs:48).

- Analysis, planning, and optional skeleton generation run first.
- Audible generation then bypasses `ResponsePlan` and `SkeletonPattern`.
- Playback receives the `PatternTurn` returned by `FeatureTransformer.Transform(...)`, not the structural output.

## 3. TurnAnalysis Technical Summary

### Purpose

- Compute compact, rule-based descriptors from the captured source `PatternTurn`.
- Supply planner-facing evidence about density, energy, anchors, ending behaviour, and segment profile.

### Main classes/files

- `TurnAnalyser`
- `TurnAnalysisResult`
- `DensityAnalyser`
- `EnergyAnalyser`
- `AnchorAnalyser`
- `EndActivityAnalyser`
- `SegmentActivityProfileAnalyser`
- `SegmentHelper`
- `EnergyThresholds`
- `SegmentActivityProfileThresholds`

### Inputs

- `PatternTurn`
- Fields actually used across analysers:
- `velocity[]`
- `offsetSamples[]` is not used by the analysers inspected here
- step count from array length

### Outputs

- `TurnAnalysisResult`
- Contains:
- `Density`
- `Energy`
- `Anchor`
- `AnchorSupport`
- `EndActivity`
- `SegmentActivityProfile`

### Execution point in runtime loop

- Called inside [`AiResponsePreparationFlow.Prepare(...)`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/AiResponsePreparationFlow.cs:67).
- Output stored through the `onAnalysed` callback in `TurnLoopController`.

### Implemented analysers

#### DensityAnalyser

- Computes:
- total step count
- active step count where `velocity[i] > 0`
- inactive step count
- overall step density
- per-segment densities over 4 contiguous segments
- Writes:
- `DensityFeatures.StepCount`
- `ActiveStepCount`
- `InactiveStepCount`
- `StepDensity`
- `SegmentDensities`
- Configuration / thresholds:
- fixed 4-segment partitioning via `SegmentHelper.SegmentCount = 4`
- Uses:
- step positions
- segment partitions
- does not use velocities beyond non-zero occupancy

#### EnergyAnalyser

- Computes:
- mean velocity over active hits only
- peak velocity
- variance of active-hit velocities
- segment mean velocities
- derived flags for high/low/flat/accented/crescendo/decrescendo
- Writes:
- `EnergyFeatures.MeanVelocity`
- `PeakVelocity`
- `VelocityVariance`
- `SegmentMeanVelocities`
- `IsHighEnergy`
- `IsLowEnergy`
- `IsFlatEnergy`
- `IsAccented`
- `IsCrescendo`
- `IsDecrescendo`
- Thresholds / configuration from `EnergyThresholds`:
- high-energy mean `>= 90`
- low-energy mean `<= 10`
- flat variance `<= 1`
- accent if `peak - mean >= 45`
- crescendo if last minus first segment mean `>= 50`
- decrescendo if first minus last segment mean `>= 50`
- Uses:
- velocities
- segment partitions
- does not use anchors or end-region logic directly

#### AnchorAnalyser

- Computes per-step salience and classifies anchor steps.
- Salience is weighted sum of:
- normalized velocity score
- local accent score
- local isolation score
- metrical weight score
- phrase-role score
- Writes:
- `AnchorFeatures.StepCount`
- `StepSalienceScores`
- `StepIsAnchor`
- `AnchorCount`
- `AnchorIndices`
- `StrongestAnchorIndex`
- `StrongestAnchorScore`
- `HasOpeningAnchor`
- `HasClosingAnchor`
- `AnchorCountsPerSegment`
- Key thresholds / configuration:
- velocity weight `0.30`
- accent weight `0.20`
- isolation weight `0.15`
- metrical weight `0.25`
- phrase role weight `0.10`
- anchor threshold `0.55`
- local radius `2`
- Metrical handling:
- special case for `96` steps: assumes `48` steps per bar
- otherwise treats the turn as a single bar-length frame for weighting
- Uses:
- velocities
- step positions
- segments
- anchors
- phrase-opening / phrase-closing logic

#### EndActivityAnalyser

- Computes summary over final quarter of the turn.
- Writes:
- `EndActivityFeatures.EndDensity`
- `EndEnergy`
- `EndAccent`
- End-region logic:
- end region starts at `floor(stepCount * 0.75)`
- `EndDensity` = active density in that region
- `EndEnergy` = mean active-hit velocity in that region
- `EndAccent` = max end-region velocity / global max velocity
- Uses:
- velocities
- end-region logic

#### SegmentActivityProfileAnalyser

- Classifies coarse shape of density and energy evolution across 4 segments.
- Input is derived features, not raw `PatternTurn`.
- Writes:
- `SegmentActivityProfileFeatures.DensityShape`
- `EnergyShape`
- Implemented shapes:
- `Flat`
- `Increasing`
- `Decreasing`
- `FrontLoaded`
- `BackLoaded`
- `MidPeak`
- `MidDip`
- Threshold / configuration:
- `ShapeEpsilon` from `SegmentActivityProfileThresholds`
- Uses:
- segment densities
- segment mean velocities

#### TurnAnalyser orchestration

- `TurnAnalyser.Analyze(...)` runs analysers in fixed order:
- density
- energy
- anchor
- end activity
- segment activity profile
- Aggregates results into one `TurnAnalysisResult`.
- `SegmentActivityProfileAnalyser` depends on earlier density/energy outputs.

### Tests/debug evidence

- [`AiResponsePreparationFlowTests.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/AiResponsePreparationFlowTests.cs:19) verifies analysis occurs before planning/generation.
- [`TemporaryTurnAnalysisPlannerBatchRunner.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Editor/TemporaryDiagnostics/TemporaryTurnAnalysisPlannerBatchRunner.cs:16) provides editor-only diagnostic generation of analysis/planner/skeleton outputs.
- Diagnostic artefacts are written under `DiagnosticsOutput/TurnAnalysisResponsePlanner/...`.

### Limitations

- Purely rule-based; no learned feature extraction.
- Uses coarse 4-segment summaries.
- Ignores microtiming offsets for analysis.
- Anchor metrical weighting is partially hard-coded around a `96`-step case.
- `AnchorSupport` is present in `TurnAnalysisResult` but not materially populated by the inspected orchestration path.
- One analyser file, `SapAnalyser.cs`, is a stub and not part of the live path.

## 4. ResponsePlanner Technical Summary

### Purpose

- Convert `TurnAnalysisResult` into a compact response strategy for the next module.
- Produce interpretable planning state rather than direct sound events.

### Main classes/files

- `ResponsePlanner`
- `PlanningContext`
- `ResponsePlan`
- `ResponseType`
- `ResponsePlannerConfig`
- `ResponsePlannerDebugSnapshot`

### Inputs

- `TurnAnalysisResult`
- Planner reads:
- density features
- energy features
- anchor features
- end activity features
- segment activity profile features

### Outputs

- `ResponsePlan`
- `LastSnapshot : ResponsePlannerDebugSnapshot`

### Execution point in runtime loop

- Called in [`AiResponsePreparationFlow.Prepare(...)`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/AiResponsePreparationFlow.cs:80).
- Output stored through `onPlanned` in `TurnLoopController`.

### Response types

- `Mirror`
- `Complement`
- `Simplify`
- `Intensify`
- `Contrast`
- `Fill`

### Planning process

#### 1. context construction

- `BuildContext(...)` normalizes and derives planner-facing booleans from analysis.
- Concrete behaviour:
- `SourceDensity` from `Density.StepDensity`
- `SourceEnergy` from `MeanVelocity / 127`
- `AnchorCount` from `Anchor.AnchorCount`
- `HasMeaningfulAnchors` requires count and strongest-anchor score thresholds
- `HasStrongEnding` can be triggered by closing anchor or end density/energy/accent thresholds
- `EndingIsOpen` uses open-ending thresholds
- `ActivityIsBackLoaded`, `ActivityIsFrontLoaded`, `ActivityIsBalanced` come from segment profile classifications
- `IsSparse`, `IsBusy`, `IsLowEnergy`, `IsHighEnergy`, `HasConversationalSpace`, `IsCongested`, `IsPredictableProfile` come from config thresholds
- `TurnLengthSteps` is inferred from density or anchor step count

#### 2. score calculation

- `ScoreResponseTypes(...)` assigns weighted scores to every `ResponseType`.
- Inputs are the context booleans and normalized source density/energy.
- Planner config provides the primary weights:
- density
- energy
- anchor
- ending
- profile
- conversational-space
- congestion
- predictable-profile
- Additional density/complementarity adjustments are applied per candidate.

#### 3. response type selection

- Highest score wins.
- If scores are within `ScoreTieMargin`, candidates are tie-resolved explicitly.
- Concrete tie rules:
- prefer `Complement` over `Mirror` when conversational gap play is available
- prefer `Simplify` over `Contrast` under congestion
- prefer `Fill` over `Intensify` when closure is weak
- prefer `Mirror` when anchored/balanced identity evidence is present
- otherwise use stable preference order:
- `Mirror`
- `Complement`
- `Simplify`
- `Intensify`
- `Fill`
- `Contrast`

#### 4. plan derivation

- `DerivePlan(...)` computes final plan parameters for the chosen type.
- `TargetDensity`:
- starts from source density plus a response-type baseline delta
- adds context-dependent density modifiers
- clamps to planner min/max and max-delta constraints
- `ComplementarityBias`:
- starts from per-type baseline
- adjusts from context
- clamps to `[0,1]`
- `PreserveAnchors`:
- enabled mainly for anchor-salient / mirroring conditions
- `MirrorEnding`:
- enabled from ending-related logic
- `TurnLengthSteps`:
- at least `1`

### ResponsePlan fields

- `ResponseType`
- selects the high-level response behaviour category
- `TargetDensity`
- target activity density for downstream structural generation
- `ComplementarityBias`
- controls bias toward complementary versus overlapping step choices downstream
- `PreserveAnchors`
- requests retention/protection of salient source anchors
- `MirrorEnding`
- requests ending-structure reinforcement
- `TurnLengthSteps`
- expected output length for downstream generation

### Debug snapshot / interpretability support

- `ResponsePlanner.LastSnapshot` is populated on each `Plan(...)` call.
- Snapshot stores:
- descriptor summary booleans
- source numeric summary
- per-response-type scores
- selected response type
- final plan
- This is diagnostic/interpretability support, not playback data.

### Tests/debug evidence

- [`ResponsePlannerTests.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/ResponsePlannerTests.cs:13) covers response selection, clamping, determinism, and snapshot creation.
- [`AiResponsePreparationFlowTests.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/AiResponsePreparationFlowTests.cs:19) confirms planner runs in the live preparation path.
- [`TemporaryTurnAnalysisPlannerBatchRunner.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Editor/TemporaryDiagnostics/TemporaryTurnAnalysisPlannerBatchRunner.cs:48) emits planner diagnostics outside the live loop.

### Limitations

- Planner is rule-based and threshold-heavy.
- No long-term adaptation or learning.
- No explicit memory of prior turns beyond current input analysis.
- `ResponsePlan` is live, but not currently the direct driver of audible playback.
- Interpretability support is strong relative to realised sound generation.

## 5. SkeletonBuilder Technical Summary

### Purpose

- Convert the `ResponsePlan` plus source-turn context into a structural step-selection pattern.
- Represent intended response structure as a scored skeleton, not as final audible audio events.

### Main classes/files

- `SkeletonBuilder`
- `SkeletonBuildRequest`
- `SkeletonBuilderConfig`
- `SkeletonPattern`
- `SkeletonStepMeta`
- `SkeletonPatternSummary`
- `SkeletonDebugSnapshot`

### Inputs

- `SkeletonBuildRequest`
- Contains:
- `Plan : ResponsePlan`
- `SourceTurn : PatternTurn`
- `SourceAnalysis : TurnAnalysisResult`
- `TurnLengthSteps`
- `StepsPerQuarter`
- `Config : SkeletonBuilderConfig`

### Outputs

- `SkeletonPattern`
- Optional debug representation via `SkeletonDebugSnapshot.FromPattern(...)`

### Execution point in runtime loop

- Called in [`AiResponsePreparationFlow.Prepare(...)`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/AiResponsePreparationFlow.cs:87) when a builder instance is present.
- Result stored in `TurnLoopController` through `StoreSkeletonDebugSnapshot(...)`.

### Build process

#### 1. request construction

- `AiResponsePreparationFlow` builds `SkeletonBuildRequest` from:
- live `ResponsePlan`
- compiled source `PatternTurn`
- current `TurnAnalysisResult`
- current turn length
- `stepsPerQuarter`
- `SkeletonBuilderConfig`

#### 2. contextual map construction

- `SkeletonBuilder.BuildSkeleton(...)` derives maps via `SkeletonDerivedMaps.Build(...)`.
- `BuildSelectionContext(...)` assembles per-step context arrays:
- explicit/fallback anchor flags
- ending steps
- weak metrical steps
- source occupancy and adjacency
- interstitial gaps
- nearest-source distances
- local source density
- metric strength / metrical weights
- source relation scores
- segment target weights
- segment indices
- `ResponseBehaviourProfileFactory.Build(...)` derives behaviour profile from the plan.

#### 3. step scoring

- `BuildStepMeta(...)` computes per-step scores and flags.
- Important score components written into each `SkeletonStepMeta`:
- `MetricScore`
- `SourceRelationScore`
- `AnchorScore`
- `EndingScore`
- `PhraseBalanceScore`
- `DensityShapingScore`
- `SpacingPenalty`
- `JitterOffset`
- `RawScore`
- `FinalScore`
- Scoring also records reason/boost flags such as:
- strong beat
- protected anchor
- complement boosted
- mirror boosted
- interstitial boosted
- segment target boosted
- late drive boosted
- anchor boosted
- ending boosted

#### 4. candidate selection

- Builder ranks candidates and derives a target active count from `Plan.TargetDensity`.
- `BuildSkeleton(scoredSteps, selectionContext)` performs the selection pass.
- If target count is missed, spacing constraints are progressively relaxed.

#### 5. constraint application

- Selection includes soft structural constraints:
- minimum spacing
- cluster-size handling
- ending enforcement
- behaviour-profile enforcement
- optional segment rebalance
- `EnsureEndingSelection(...)` and `EnsureBehaviourProfileSelection(...)` explicitly patch the chosen set when needed.

#### 6. debug snapshot construction

- `SkeletonPattern` stores the full structural/debug representation.
- `TurnLoopController.StoreSkeletonDebugSnapshot(...)` converts it to `SkeletonDebugSnapshot`.
- Snapshot records selected indices, annotations, achieved density, anchor alignment, overlap, and density-target status.

### SkeletonPattern structure

- `TurnLengthSteps`
- `ActiveSteps`
- `SelectionScores`
- `StepMeta`
- `SelectedStepIndices`
- `Summary`

### SkeletonStepMeta / debug metadata

- `StepIndex`
- `SegmentIndex`
- `IsStrongBeat`
- `StepsFromEnd`
- `MetricScore`
- `SourceRelationScore`
- `AnchorScore`
- `EndingScore`
- `PhraseBalanceScore`
- `DensityShapingScore`
- `SpacingPenalty`
- `JitterOffset`
- `RawScore`
- `FinalScore`
- `Selected`
- `SourceOccupied`
- `SourceGap`
- `SourceAnchor`
- `IsExplicitAnchor`
- `IsFallbackAnchor`
- `InEndingRegion`
- `AdjacentToSource`
- `NearSource`
- `InterstitialSourceGap`
- `DistanceToNearestSourceHit`
- `PreviousSourceHitDistance`
- `NextSourceHitDistance`
- `LocalSourceDensity`
- `MetricStrengthLevel`
- `SegmentSourceWeight`
- `Protected`
- `ReasonFlags`

### Runtime status

- Runs live: yes.
- Affects audible output directly: no.
- Where output is stored:
- returned from `AiResponsePreparationFlow`
- converted to `SkeletonDebugSnapshot`
- stored in controller/debug state
- not sent to `IT4ChuckTurnPlayer`

### Tests/debug evidence

- [`SkeletonBuilderTests.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/SkeletonBuilderTests.cs:83) checks structure sizes, selected count, density summary, and source/anchor mapping behaviour.
- [`AiResponsePreparationFlowTests.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/AiResponsePreparationFlowTests.cs:62) confirms skeleton generation occurs before feature transformation when a builder is supplied.
- [`TemporaryTurnAnalysisPlannerBatchRunner.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Editor/TemporaryDiagnostics/TemporaryTurnAnalysisPlannerBatchRunner.cs:63) emits structural diagnostics for non-live inspection.

### Limitations

- Live but still prototype in system terms.
- Output is not yet realised into the audible `PatternTurn`.
- Structural richness exceeds current downstream use.
- Constraint system is still heuristic, not learned or search-based.
- No integrated final realiser stage after skeleton generation in the live playback path.

## 6. FeatureTransformer Technical Summary

### Purpose

- Produce the current audible AI response turn.
- Directly transform the human `PatternTurn` into another `PatternTurn` for playback.

### Main classes/files

- `FeatureTransformer`
- downstream consumer: `IT4ChuckTurnPlayer`

### Inputs

- `PatternTurn`
- Optional `FeatureTransformer.Mode`

### Outputs

- Transformed `PatternTurn`

### Execution point in runtime loop

- Called in [`AiResponsePreparationFlow.Prepare(...)`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/AiResponsePreparationFlow.cs:110).
- Output stored through `onGenerated`.
- Same generated pattern is later passed to `IT4ChuckTurnPlayer.PlayTurn(...)`.

### Transformation modes

#### Auto

- Trigger / selection logic:
- default mode
- runs `Analyse(...)` first
- if `activeSteps == 0`, returns a clone
- if `maxGap < 6`, selects `SparseOrnament`
- else if `meanGap < 3f`, selects `EchoAccent`
- else selects `EndFill`
- Transformation behaviour:
- delegates to one of the concrete modes above
- Velocity handling:
- no independent handling beyond selected mode
- Step-position handling:
- no independent handling beyond selected mode

#### EchoAccent

- Trigger / selection logic:
- explicit mode, or selected by `Auto` when `meanGap < 3f`
- Transformation behaviour:
- clones source
- finds hits whose velocity is at least `round(maxVelocity * 0.8)`
- for each such hit, tries to place an echo at `i + 3`
- only inserts if target step is currently empty
- Velocity handling:
- inserted velocity is `round(sourceVelocity * 0.7)`
- Step-position handling:
- adds fixed offset of `+3` steps
- inserted microtiming offset is `0`

#### EndFill

- Trigger / selection logic:
- explicit mode, or selected by `Auto` fallback branch
- Transformation behaviour:
- inspects final quarter beginning at `floor(stepCount * 0.75)`
- if end-region active count is `<= 2`, attempts insertions at:
- `stepCount - 6`
- `stepCount - 3`
- `stepCount - 1`
- inserts only into empty steps
- Velocity handling:
- inserted velocity is `round(max(60, meanVelocity))`
- Step-position handling:
- uses fixed late-turn positions
- inserted microtiming offset is `0`

#### SparseOrnament

- Trigger / selection logic:
- explicit mode, or selected by `Auto` when `maxGap < 6`
- Transformation behaviour:
- clones source
- for each onset with gap-after value `>= 2`, tries to place an ornament one step later
- only inserts if target step is empty
- Velocity handling:
- inserted velocity is `round(sourceVelocity * 0.6)`
- Step-position handling:
- adds fixed offset of `+1` step
- inserted microtiming offset is `0`

#### Other implemented modes

- No additional public modes beyond:
- `Auto`
- `EchoAccent`
- `EndFill`
- `SparseOrnament`

### Relationship to planner/skeleton

- Consumes `TurnAnalysisResult`: no
- Consumes `ResponsePlan`: no
- Consumes `SkeletonPattern`: no
- Current relationship:
- runs after those modules in the live loop
- ignores their outputs
- transforms the original compiled source turn directly

### Tests/debug evidence

- [`AiResponsePreparationFlowTests.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/AiResponsePreparationFlowTests.cs:19) asserts the generated result equals `new FeatureTransformer().Transform(compiledPattern)`.
- [`AiResponsePreparationFlowTests.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Tests/EditMode/Editor/AiResponsePreparationFlowTests.cs:62) further shows that even when skeleton generation is enabled, playback-bound output still comes from `FeatureTransformer`.
- [`IT4ChuckTurnPlayer.cs`](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/ChunityAudio/IT4ChuckTurnPlayer.cs:80) accepts only `PatternTurn`.

### Limitations

- This is the current audible generator, but it is musically/simple rule transformation rather than planner-driven realisation.
- No use of planner or skeleton outputs.
- Limited transformation vocabulary.
- Inserts use fixed step offsets and zero microtiming.
- Velocity shaping is scalar rescaling only.

## 7. Live Runtime Integration

| Step | Method/class | Input | Output | Stored? | Sent to playback? | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `TurnLoopController.TickGeneratingAiResponse()` | `CapturedTurnData` / compiled turn context | call into preparation flow | n/a | n/a | Live orchestration entry for AI turn generation. See [TurnLoopController.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/TurnLoopController.cs:885). |
| 2 | `AiResponsePreparationFlow.Prepare(...)` | `CapturedTurnData`, timing/config, callbacks | compiled source `PatternTurn` | not separately callback-stored here | no | Preparation flow owns the analysis/planning/generation chain. See [AiResponsePreparationFlow.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/Orchestration/AiResponsePreparationFlow.cs:48). |
| 3 | `PatternCompiler.Compile(...)` | captured turn + timing metadata | source `PatternTurn` | implicit local variable | no | Source turn for both reasoning and direct transformation. |
| 4 | `TurnAnalyser.Analyze(...)` | source `PatternTurn` | `TurnAnalysisResult` | yes, via `StoreAnalysisResult(...)` | no | First live reasoning stage. |
| 5 | `ResponsePlanner.Plan(...)` | `TurnAnalysisResult` | `ResponsePlan` | yes, via `StoreResponsePlan(...)` | no | Live planning stage; snapshot retained in planner instance. |
| 6 | `SkeletonBuilder.BuildSkeleton(...)` when builder present | `SkeletonBuildRequest` | `SkeletonPattern` | yes, converted by `StoreSkeletonDebugSnapshot(...)` | no | Live structural generation, but debug/prototype in system effect terms. |
| 7 | `FeatureTransformer.Transform(...)` | source `PatternTurn` | generated `PatternTurn` | yes, via `StoreGeneratedAiPattern(...)` | yes | Split occurs here: reasoning output exists, but playback-bound output is still direct transformation of the source turn. |
| 8 | `AiResponsePreparationFlow` return object | analysis + plan + generated pattern + optional skeleton | `AiResponsePreparationResult` | controller uses component parts | generated pattern only | Container object exposes all products of the preparation stage. |
| 9 | `IT4ChuckTurnPlayer.PlayTurn(...)` | generated `PatternTurn` | ChucK global arrays / playback trigger | n/a | yes | Playback consumes only `PatternTurn`, not plan or skeleton. See [IT4ChuckTurnPlayer.cs](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/Scripts/ChunityAudio/IT4ChuckTurnPlayer.cs:80). |
| 10 | `IT4_TurnPlayer.ck` | velocity/offset arrays + timing globals | audible scheduled drum playback | n/a | yes | ChucK sorts events and schedules them sample-accurately. See [IT4_TurnPlayer.ck](/c:/Users/Student/OneDrive%20-%20The%20University%20of%20Nottingham/Disso%2025-26/IT4s/GameEngine/Unity/My%20project/Assets/StreamingAssets/ChucK/IT4_TurnPlayer.ck:100). |

## 8. Chapter 4 Writing Guidance

### TurnAnalysis

- Main-text explanation length:
- short-to-medium
- Include a table:
- yes; one compact feature table is useful
- Include pseudocode:
- no
- Move details to appendix:
- per-field detail for all feature classes
- exact anchor weighting formula
- exact threshold values
- Do not over-explain:
- every analyser class separately as a mini-section
- semantic interpretation of descriptors beyond implementation role

### ResponsePlanner

- Main-text explanation length:
- medium
- Include a table:
- yes; response types plus controlled `ResponsePlan` fields
- Include pseudocode:
- optional, one short 4-step planning pipeline only
- Move details to appendix:
- full score weights
- tie-break rule list
- full debug snapshot schema
- Do not over-explain:
- broad musical rationale
- class-by-class internal helpers

### SkeletonBuilder

- Main-text explanation length:
- medium
- Include a table:
- yes; input/output and step-scoring components
- Include pseudocode:
- yes; short structural build pipeline is appropriate
- Move details to appendix:
- full `SkeletonStepMeta` field list
- full config values
- exhaustive constraint logic
- Do not over-explain:
- every debug field
- low-level helper-map construction

### FeatureTransformer

- Main-text explanation length:
- short-to-medium
- Include a table:
- yes; transformation modes and concrete actions
- Include pseudocode:
- no
- Move details to appendix:
- exact insertion offsets and velocity multipliers if space is tight
- Do not over-explain:
- semantic framing of transformations as sophisticated planning
- this module should be described plainly as the current audible generator

### Cross-module presentation recommendation

- Main text should show:
- one runtime-path figure or compact table
- one module summary table
- one explicit sentence that the planner/skeleton path runs live but is not yet the audible source
- Appendix should hold:
- thresholds
- expanded field schemas
- diagnostic snapshot schemas
- test inventory
- Do not include:
- a class-by-class walkthrough
- long narrative explanation of design intent
- claims that `ResponsePlan` or `SkeletonPattern` currently determine the sounding rhythm
