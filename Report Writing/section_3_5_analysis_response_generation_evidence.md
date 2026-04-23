# Section 3.5 Evidence Extraction - Analysis and Response Generation

Status key:

- **Implemented/live**: executed in the current Unity turn loop.
- **Implemented/scaffold**: code exists and runs or can run, but is not yet the audible final response source.
- **Interim**: current practical bridge used to keep the loop playable.
- **Deferred/not implemented**: described, implied, or structurally prepared for, but not present in the live response path.

Scope note: this file only covers how symbolic rhythmic input is analysed and transformed into response intent / output. It deliberately avoids re-explaining capture, segmentation, FSM behaviour, and full `PatternTurn` representation except where directly needed for response generation.

---

## 1. Feature Extraction

### 1.1 Density

Status: **implemented/live**.

Implementation:

- Class: `DensityAnalyser : IAnalyser<DensityFeatures>`.
- Evidence: `GameEngine/Unity/My project/Assets/Scripts/Rhythm/TurnAnalysis/Analysers/DensityAnalyser.cs:8-58`.

Inputs:

- `PatternTurn pattern`.
- Uses `pattern.StepCount` and the symbolic velocity grid.
- A step is active when `pattern.velocity[i] > 0`.
- Evidence: `DensityAnalyser.cs:15-27`, `DensityAnalyser.cs:39-49`.

Outputs:

- `DensityFeatures`.
- Fields: `StepCount`, `ActiveStepCount`, `InactiveStepCount`, `StepDensity`, `SegmentDensities`.
- Evidence: `DensityFeatures.cs:7-13`, constructor assignment at `DensityFeatures.cs:20-31`.

Musical idea captured:

- Occupancy / busyness of the quantised rhythmic grid.
- Global density describes how much rhythmic space is used.
- Segment density describes where activity sits across the turn.

Thresholding / configuration / simplifications:

- No density thresholds inside `DensityAnalyser`.
- Segmentation is fixed to four near-even segments through `SegmentHelper.SegmentCount = 4`.
- Evidence: `SegmentHelper.cs:5-32`; density uses it at `DensityAnalyser.cs:29-50`.
- Higher-level sparse/busy interpretation happens later in `ResponsePlannerConfig`, not in the analyser.
- Evidence: `ResponsePlannerConfig.cs:5-9`.

Behavioural impact:

- Density becomes the main source for planner categories such as sparse, balanced, busy, conversational space, and congestion.
- Evidence: `ResponsePlanner.cs:57`, `ResponsePlanner.cs:78-87`.

---

### 1.2 Energy

Status: **implemented/live**.

Implementation:

- Class: `EnergyAnalyser : IAnalyser<EnergyFeatures>`.
- Evidence: `EnergyAnalyser.cs:8-81`.

Inputs:

- `PatternTurn pattern`.
- Uses active-step velocities only: inactive or zero-velocity steps are skipped.
- Evidence: `EnergyAnalyser.cs:22-38`, `EnergyAnalyser.cs:95-103`, `EnergyAnalyser.cs:123-135`.

Outputs:

- `EnergyFeatures`.
- Numeric outputs: `MeanVelocity`, `PeakVelocity`, `VelocityVariance`, `SegmentMeanVelocities`.
- Boolean traits: `IsHighEnergy`, `IsLowEnergy`, `IsFlatEnergy`, `IsAccented`, `IsCrescendo`, `IsDecrescendo`.
- Evidence: `EnergyFeatures.cs:7-18`, constructor assignment at `EnergyFeatures.cs:25-46`.

Musical idea captured:

- Dynamic intensity, separate from occupancy.
- Lets the system distinguish similarly dense patterns that differ in force, accent, or broad dynamic direction.

Thresholding / configuration:

- `EnergyThresholds` contains:
  - `HighEnergyMeanVelocity`
  - `LowEnergyMeanVelocity`
  - `FlatVarianceThreshold`
  - `AccentPeakOverMeanThreshold`
  - `CrescendoMinDelta`
  - `DecrescendoMinDelta`
- Evidence: `EnergyThresholds.cs:6-29`.
- Defaults wired in `TurnLoopBootstrap`:
  - high mean velocity = `90`
  - low mean velocity = `10`
  - flat variance = `1`
  - accent peak over mean = `45`
  - crescendo / decrescendo min delta = `50`
- Evidence: `TurnLoopBootstrap.cs:22-28`, `TurnLoopBootstrap.cs:159-168`.

Implemented decisions:

- Mean velocity is computed over active hits only.
- Empty input returns low-energy true and other expressive flags false.
- Evidence: `EnergyAnalyser.cs:40-60`.
- Crescendo/decrescendo are coarse: last segment mean minus first segment mean.
- Evidence: `EnergyAnalyser.cs:67-69`.

Behavioural impact:

- Planner normalises mean velocity by `127` for source energy.
- Evidence: `ResponsePlanner.cs:58`, `ResponsePlanner.cs:735-742`.
- Energy flags and normalised energy both influence low/high energy descriptors.
- Evidence: `ResponsePlanner.cs:80-81`.

---

### 1.3 Anchor Detection

Status: **implemented/live**.

Implementation:

- Class: `AnchorAnalyser : IAnalyser<AnchorFeatures>`.
- Evidence: `AnchorAnalyser.cs:9-77`.

Inputs:

- `PatternTurn pattern`.
- Uses:
  - velocity magnitude
  - local accent compared with neighbouring active velocities
  - local isolation
  - metrical position
  - phrase role / opening and closing position
- Evidence: score composition at `AnchorAnalyser.cs:42-47`; component methods at `AnchorAnalyser.cs:100-210`.

Outputs:

- `AnchorFeatures`.
- Fields include:
  - `StepSalienceScores`
  - `StepIsAnchor`
  - `AnchorCount`
  - `AnchorIndices`
  - `StrongestAnchorIndex`
  - `StrongestAnchorScore`
  - `HasOpeningAnchor`
  - `HasClosingAnchor`
  - `AnchorCountsPerSegment`
- Evidence: `AnchorFeatures.cs:7-18`, constructor assignment at `AnchorFeatures.cs:25-46`.

Musical idea captured:

- Event-level structural salience: which hits behave like pivots, accents, openings, or endings rather than merely counting how many hits occurred.

Scoring / thresholds:

- Fixed weighted salience model:
  - velocity weight = `0.30`
  - accent weight = `0.20`
  - isolation weight = `0.15`
  - metrical weight = `0.25`
  - phrase-role weight = `0.10`
- Evidence: `AnchorAnalyser.cs:11-15`.
- Anchor threshold = `0.55`.
- Evidence: `AnchorAnalyser.cs:17`, binary decision at `AnchorAnalyser.cs:51-56`.
- Local neighbourhood radius = `2` steps; velocity normalised by max `127`.
- Evidence: `AnchorAnalyser.cs:18-19`, `AnchorAnalyser.cs:100-131`.

Simplifications:

- Metrical weighting is hard-coded from step position.
- For 96-step turns, `stepsPerBar = 48`; otherwise the whole turn is treated as one bar.
- Evidence: `AnchorAnalyser.cs:162-177`.
- Phrase-role scoring uses first/last active hit, opening/closing windows, and simple mid-turn ranges.
- Evidence: `AnchorAnalyser.cs:179-210`.

Behavioural impact:

- Planner treats anchors as "meaningful" only if both count and strongest-score thresholds are met.
- Defaults: at least `1` anchor and strongest score at least `0.65`.
- Evidence: `ResponsePlannerConfig.cs:14-15`; planner condition at `ResponsePlanner.cs:62-64`.

---

### 1.4 End Activity

Status: **implemented/live**.

Implementation:

- Class: `EndActivityAnalyser : IAnalyser<EndActivityFeatures>`.
- Evidence: `EndActivityAnalyser.cs:7-51`.

Inputs:

- `PatternTurn pattern`.
- Uses only the final quarter of the turn: `endStart = (int)(N * 0.75f)`.
- Evidence: `EndActivityAnalyser.cs:14-21`.

Outputs:

- `EndActivityFeatures`.
- Fields: `EndDensity`, `EndEnergy`, `EndAccent`.
- Evidence: `EndActivityFeatures.cs:6-24`.

Musical idea captured:

- Closure / final-region momentum.
- Measures whether the source ending is sparse, forceful, accented, or still active.

Thresholding / configuration:

- No thresholds inside the analyser.
- Planner interprets the end using:
  - strong ending density = `0.50`
  - strong ending energy = `0.60`
  - strong ending accent = `0.75`
  - open ending density = `0.20`
  - open ending energy = `0.30`
  - open ending accent = `0.35`
- Evidence: `ResponsePlannerConfig.cs:17-23`.

Simplifications:

- End window is a fixed final 25% region.
- End energy is active-hit mean velocity.
- End accent is max velocity in that window.
- Evidence: `EndActivityAnalyser.cs:21-50`.

Behavioural impact:

- Planner uses end activity to infer `HasStrongEnding` and `EndingIsOpen`.
- Evidence: `ResponsePlanner.cs:65-74`.
- `MirrorEnding` is only true for mirror responses with a strong, non-open ending.
- Evidence: `ResponsePlanner.cs:214-219`.

---

### 1.5 Segment Activity Profile

Status: **implemented/live as derived analysis**.

Implementation:

- Class: `SegmentActivityProfileAnalyser`.
- Evidence: `SegmentActivityProfileAnalyser.cs:8-88`.
- There is also an empty `SapAnalyser` class.
- Evidence: `SapAnalyser.cs:1-6`.

Inputs:

- Does not operate directly on `PatternTurn`.
- Uses `DensityFeatures.SegmentDensities` and `EnergyFeatures.SegmentMeanVelocities`.
- Evidence: `SegmentActivityProfileAnalyser.cs:17-30`.

Outputs:

- `SegmentActivityProfileFeatures`.
- Fields: `DensityShape`, `EnergyShape`.
- Evidence: `SegmentActivityProfileFeatures.cs:6-22`.
- Shape enum values:
  - `Flat`
  - `Increasing`
  - `Decreasing`
  - `FrontLoaded`
  - `BackLoaded`
  - `MidPeak`
  - `MidDip`
- Evidence: `ActivityShape.cs:3-12`.

Musical idea captured:

- Broad phrase contour: whether activity increases, decreases, sits early, sits late, peaks in the middle, dips in the middle, or stays flat.

Thresholding / configuration:

- Requires exactly four segment values.
- Evidence: `SegmentActivityProfileAnalyser.cs:32-35`.
- Uses `ShapeEpsilon` to ignore small deltas.
- Evidence: `SegmentActivityProfileAnalyser.cs:37`, `SegmentActivityProfileAnalyser.cs:84-87`.
- Default epsilon = `0.02`.
- Evidence: `TurnLoopBootstrap.cs:28`, `TurnLoopBootstrap.cs:170-172`.

Simplifications:

- The derived shape is coarse and categorical.
- The analyser returns `Flat` as fallback if none of the shape rules match.
- Evidence: `SegmentActivityProfileAnalyser.cs:48-81`.

Behavioural impact:

- Planner uses shapes for descriptors such as back-loaded, front-loaded, balanced, and predictable.
- Evidence: `ResponsePlanner.cs:75-77`, `ResponsePlanner.cs:656-696`.

---

### 1.6 Analysis Scaffolds / Non-Computed Models

`AnchorSupportFeatures` exists but is not computed by any analyser in the current `TurnAnalyser` flow.

Evidence:

- `TurnAnalysisResult` contains `AnchorSupportFeatures`.
- Evidence: `TurnAnalysisResult.cs:8-13`.
- The constructor overload used by `TurnAnalyser` does not pass anchor support; it defaults to `new AnchorSupportFeatures()`.
- Evidence: `TurnAnalyser.cs:41-46`; defaulting at `TurnAnalysisResult.cs:26-39`.
- `AnchorSupportFeatures` has fields for average support and weak-hit ratios, but no corresponding analyser was found in the live analysis flow.
- Evidence: `AnchorSupportFeatures.cs:6-28`.

Impact:

- Anchor support is currently **scaffold/model-only**, not live feature evidence.
- The planner does not consume it.

---

## 2. TurnAnalyser as Analysis Orchestrator

Status: **implemented/live**.

Implementation:

- Class: `TurnAnalyser`.
- Constructor requires:
  - `DensityAnalyser`
  - `EnergyAnalyser`
  - `AnchorAnalyser`
  - `EndActivityAnalyser`
  - `SegmentActivityProfileAnalyser`
- Evidence: `TurnAnalyser.cs:8-28`.

Coordination:

- `Analyze(PatternTurn pattern)` runs:
  - density from `pattern`
  - energy from `pattern`
  - anchors from `pattern`
  - end activity from `pattern`
  - segment activity profile from density + energy
- Evidence: `TurnAnalyser.cs:30-40`.

Packaging:

- Returns `TurnAnalysisResult`.
- Evidence: `TurnAnalyser.cs:41-46`.
- `TurnAnalysisResult` packages:
  - `Density`
  - `Energy`
  - `Anchor`
  - `AnchorSupport`
  - `EndActivity`
  - `SegmentActivityProfile`
- Evidence: `TurnAnalysisResult.cs:6-13`, constructor null-defaulting at `TurnAnalysisResult.cs:42-55`.

Runtime wiring:

- `TurnLoopBootstrap` creates the analyser with the implemented modules and default thresholds.
- Evidence: `TurnLoopBootstrap.cs:149-172`.
- `AiResponsePreparationFlow` runs `TurnAnalyser.Analyze`, invokes `onAnalysed`, then passes the result into `IResponsePlanner.Plan`.
- Evidence: `AiResponsePreparationFlow.cs:67-85`.

Implementation-supported interpretation:

- Analysis is modular and explicitly inspectable: each feature family is a separate object, and the orchestrator only composes them.
- Segment Activity Profile is a second-order feature derived from density/energy rather than a new pass over raw symbolic data.

---

## 3. Response Planning

### 3.1 Planner Interface and Output Type

Status: **implemented/live**.

Interface:

- `IResponsePlanner.Plan(TurnAnalysisResult analysis) -> ResponsePlan`.
- Evidence: `IResponsePlanner.cs:6-9`.

Response types:

- `Mirror`
- `Complement`
- `Simplify`
- `Intensify`
- `Contrast`
- `Fill`
- Evidence: `ResponseType.cs:3-10`.

ResponsePlan fields:

- `ResponseType`
- `TargetDensity`
- `ComplementarityBias`
- `PreserveAnchors`
- `MirrorEnding`
- `TurnLengthSteps`
- Evidence: `ResponsePlan.cs:6-34`.

Design meaning:

- The response is first represented as a compact musical intention, not as a final hit pattern.

---

### 3.2 PlanningContext

Status: **implemented/internal**.

Implementation:

- Class: `PlanningContext`.
- Evidence: `PlanningContext.cs:3-63`.

Inputs / derived descriptors:

- Built from `TurnAnalysisResult` inside `ResponsePlanner.BuildContext`.
- Evidence: `ResponsePlanner.cs:49-107`.
- Captures:
  - source density
  - source energy
  - anchor count
  - meaningful anchors
  - strong/open ending
  - back/front/balanced activity
  - sparse/busy/balanced density
  - low/medium/high energy
  - conversational space
  - congestion
  - predictable profile
  - turn length
- Evidence: `PlanningContext.cs:5-22`, properties at `PlanningContext.cs:43-62`.

Key mappings:

- `sourceDensity = Clamp01(density.StepDensity)`.
- `sourceEnergy = NormalizeVelocity(energy.MeanVelocity)`.
- Evidence: `ResponsePlanner.cs:57-61`.
- Meaningful anchors require both count and score thresholds.
- Evidence: `ResponsePlanner.cs:62-64`.
- Strong ending can come from a closing anchor or from end density plus energy/accent.
- Evidence: `ResponsePlanner.cs:65-69`.
- Open ending requires no closing anchor and low density/energy/accent.
- Evidence: `ResponsePlanner.cs:70-74`.

Assumption:

- Planning reduces richer analysis objects to a small set of symbolic descriptors before choosing a response type.

---

### 3.3 Planner Configuration

Status: **implemented/live tunable config**.

Implementation:

- Class: `ResponsePlannerConfig`.
- Evidence: `ResponsePlannerConfig.cs:3-41`.

Thresholds and weights:

- Density thresholds:
  - sparse = `0.35`
  - very sparse = `0.20`
  - busy = `0.70`
  - conversational space = `0.60`
  - congested = `0.75`
- Evidence: `ResponsePlannerConfig.cs:5-9`.
- Energy thresholds:
  - low = `0.35`
  - high = `0.70`
- Evidence: `ResponsePlannerConfig.cs:11-12`.
- Anchor thresholds:
  - count = `1`
  - strongest score = `0.65`
- Evidence: `ResponsePlannerConfig.cs:14-15`.
- Output clamps:
  - min target density = `0.10`
  - max target density = `0.80`
  - max target density delta = `0.22`
- Evidence: `ResponsePlannerConfig.cs:25-27`.
- Scoring weights and tie margin are also config values.
- Evidence: `ResponsePlannerConfig.cs:29-40`.

Assumption:

- Musical categories are hand-tuned thresholds rather than learned boundaries.

---

### 3.4 Response Type Selection

Status: **implemented/live deterministic rule scoring**.

Core flow:

- `Plan` builds context, chooses response type, derives plan, creates debug snapshot.
- Evidence: `ResponsePlanner.cs:27-38`.
- `ChooseResponseType` scores all response types and resolves close-score ties.
- Evidence: `ResponsePlanner.cs:109-119`.
- Score dictionary includes all six response types.
- Evidence: `ResponsePlanner.cs:221-231`.

Rules by response type:

- **Mirror**
  - favoured by balanced density, medium energy, meaningful anchors, strong ending, balanced activity.
  - penalised by open ending and predictable profile.
  - Evidence: `ResponsePlanner.cs:234-245`.
- **Complement**
  - favoured by balanced density/energy, meaningful anchors, conversational space, lack of strong ending, balanced activity.
  - penalised if no anchors and if busy.
  - Evidence: `ResponsePlanner.cs:247-259`.
- **Simplify**
  - favoured by busy, high energy, congestion, predictable profile.
  - penalised by sparse and low energy inputs.
  - Evidence: `ResponsePlanner.cs:261-271`.
- **Intensify**
  - favoured by sparse, low/medium energy, lack of anchors, lack of strong ending.
  - penalised by busy/congested input.
  - Evidence: `ResponsePlanner.cs:273-284`.
- **Contrast**
  - requires profile reason: predictable, back-loaded, or front-loaded.
  - favoured by predictable/asymmetric profile and high energy.
  - penalised by meaningful anchors, balanced activity, and lack of predictable profile.
  - Evidence: `ResponsePlanner.cs:286-306`.
- **Fill**
  - favoured by sparse input, open/weak ending, back-loaded activity, low energy.
  - penalised by strong ending and busy input.
  - Evidence: `ResponsePlanner.cs:308-319`.

Tie resolution:

- Close-score candidates are those within `ScoreTieMargin`.
- Evidence: `ResponsePlanner.cs:334-348`.
- Specific tie rules prefer:
  - complement over mirror when conversational gap play is useful
  - simplify over contrast when congested
  - fill over intensify when weak closure is the primary issue
  - mirror when identity is balanced and anchored
- Evidence: `ResponsePlanner.cs:350-380`.
- Final stable tie preference:
  - Mirror
  - Complement
  - Simplify
  - Intensify
  - Fill
  - Contrast
- Evidence: `ResponsePlanner.cs:450-469`.

Determinism evidence:

- Unit test asserts same input produces same plan.
- Evidence: `ResponsePlannerTests.cs:216-229`.

---

### 3.5 Response Parameter Derivation

Status: **implemented/live**.

Derivation flow:

- `DerivePlan` outputs a `ResponsePlan` from context + selected response type.
- Evidence: `ResponsePlanner.cs:121-132`.

Target density:

- Starts with source density plus response-type delta.
- Adds context modifier.
- Clamps density change and final range.
- Evidence: `ResponsePlanner.cs:142-148`.

Response-type density deltas:

- Mirror: `+0.00`
- Complement: `-0.05`
- Simplify: `-0.15`
- Intensify: `+0.12`
- Contrast: `+0.00`
- Fill: `+0.15`
- Evidence: `ResponsePlanner.cs:471-482`.

Complementarity bias:

- Baseline values:
  - Mirror `0.30`
  - Complement `0.75`
  - Simplify `0.40`
  - Intensify `0.55`
  - Contrast `0.65`
  - Fill `0.70`
- Evidence: `ResponsePlanner.cs:485-497`.
- Context adjusts baseline according to response type.
- Evidence: `ResponsePlanner.cs:150-190`.

Anchor preservation:

- If anchors are not meaningful, always false.
- Mirror preserves anchors.
- Complement preserves anchors only when not congested.
- Simplify preserves anchors only when not congested and strong-ending/balanced-profile conditions hold.
- Contrast, Intensify, and Fill do not preserve anchors.
- Evidence: `ResponsePlanner.cs:192-212`.

Ending mirroring:

- True only for mirror responses with strong, non-open endings.
- Evidence: `ResponsePlanner.cs:214-219`.

Clamping:

- `TargetDensity` and `ComplementarityBias` clamped to `[0,1]`.
- `TurnLengthSteps` clamped to at least 1.
- Evidence: `ResponsePlanner.cs:134-140`.

Debug output:

- Planner stores `LastSnapshot`.
- Snapshot contains source descriptors, numeric summary, per-response-type scores, selected type, and final plan.
- Evidence: `ResponsePlanner.cs:25`, `ResponsePlanner.cs:556-620`; model at `ResponsePlannerDebugSnapshot.cs:8-38`.
- Unit test verifies snapshot population and all six scores.
- Evidence: `ResponsePlannerTests.cs:231-260`.

---

## 4. Structural Generation

### 4.1 SkeletonBuilder Role

Status: **implemented/scaffold**.

Important distinction:

- `SkeletonBuilder` is implemented and wired in the current bootstrap.
- Evidence: `TurnLoopBootstrap.cs:72-74`, injected at `TurnLoopBootstrap.cs:90-101`.
- It is executed before `FeatureTransformer` when present.
- Evidence: `AiResponsePreparationFlow.cs:87-102`.
- However, its `SkeletonPattern` is stored/debugged, not converted into the audible `PatternTurn`.
- Evidence: `AiResponsePreparationFlow.cs:110-119`; `TurnLoopController.cs:1029-1042`.

Interface:

- `ISkeletonBuilder.BuildSkeleton(SkeletonBuildRequest request) -> SkeletonPattern`.
- Evidence: `ISkeletonBuilder.cs:5-8`.

Request structure:

- `ResponsePlan Plan`
- `PatternTurn SourceTurn`
- `TurnAnalysisResult SourceAnalysis`
- `int TurnLengthSteps`
- `int StepsPerQuarter`
- `SkeletonBuilderConfig Config`
- Evidence: `SkeletonBuildRequest.cs:7-15`.

Skeleton output:

- `SkeletonPattern` includes:
  - `TurnLengthSteps`
  - `ActiveSteps`
  - `SelectionScores`
  - `StepMeta`
  - `SelectedStepIndices`
  - `Summary`
- Evidence: `SkeletonPattern.cs:6-56`.
- `SkeletonPatternSummary` includes:
  - active count
  - achieved density
  - source overlap count
  - anchor aligned count
  - density target met
  - stochastic tie-break flag
- Evidence: `SkeletonPatternSummary.cs:6-29`.

---

### 4.2 Derived Maps

Status: **implemented/scaffold**.

`SkeletonDerivedMaps.Build` creates the structural maps used for scoring.

Evidence:

- `SkeletonDerivedMaps.cs:73-115`.

Maps include:

- Source occupancy and gaps.
- Source anchor maps: combined, explicit, fallback.
- Ending region and steps from end.
- Metrical salience, strong beats, metric strength levels.
- Step-to-segment map.
- Source neighbourhood maps: adjacent, near, interstitial, distance, local density, segment source weights.
- Evidence: fields at `SkeletonDerivedMaps.cs:8-27`.

Source map logic:

- Source occupancy is `sourceTurn.velocity[i] > 0`.
- If source and response lengths differ, only the aligned prefix is mapped.
- Evidence: `SkeletonSourceMapBuilder.cs:64-83`.
- Neighbourhood derives:
  - gap steps
  - adjacent-to-source steps
  - near-source steps within distance `<= 2`
  - local source density within radius `2`
  - interstitial gaps between source hits
  - segment source weights
- Evidence: `SkeletonSourceMapBuilder.cs:86-184`.

Anchor map logic:

- Explicit anchors come from `AnchorFeatures.StepIsAnchor`.
- Fallback anchors come from `AnchorFeatures.AnchorIndices` when flags are absent and index is within the aligned source prefix.
- Combined anchors are explicit OR fallback.
- Evidence: `SkeletonSourceMapBuilder.cs:186-212`, `SkeletonSourceMapBuilder.cs:214-258`.

Metric map logic:

- Metric strength levels:
  - strongest: step aligned with `stepsPerQuarter`
  - strong: subdivision by 2
  - medium: subdivision by 4
  - weak: otherwise
- Evidence: `SkeletonMetricMapBuilder.cs:55-69`.
- Salience:
  - strongest = `1.00`
  - strong = `0.75`
  - medium = `0.50`
  - weak = `0.20`
- Evidence: `SkeletonMetricMapBuilder.cs:71-80`.

Segment map:

- Uses the same four-segment `SegmentHelper` approach as analysis.
- Evidence: `SkeletonSegmentMapBuilder.cs:15-27`.

Ending map:

- Ending-sensitive region is final quarter-note window: `windowLength = min(turnLengthSteps, stepsPerQuarter)`.
- Evidence: `SkeletonEndingRegionHelper.cs:19-40`.

---

### 4.3 Behaviour Profiles

Status: **implemented/scaffold**.

Implementation:

- `ResponseBehaviourProfile` maps `ResponsePlan` intent into structural-generation policies.
- Evidence: `ResponseBehaviourProfile.cs:52-98`.
- Factory builds a profile from `ResponsePlan`, `SkeletonBuilderConfig`, and derived maps.
- Evidence: `ResponseBehaviourProfile.cs:221-281`.

Policy families:

- Source overlap policy:
  - preserve
  - reduce to anchors
  - prefer gaps
  - displace
  - interstitial
  - expand around source
- Metric tolerance policy:
  - spine only
  - strong and medium
  - broad
  - offbeat friendly
  - medium propulsion
- Weak-step policy:
  - suppress
  - defer
  - allow
  - prefer interstitial
- Ending policy:
  - mirror source
  - structural only
  - fill gap
  - avoid accent
  - late drive
- Segment target policy:
  - follow source
  - structural spine
  - sparse segments
  - invert source
  - interstitial
  - active late expansion
- Evidence: `ResponseBehaviourProfile.cs:6-50`.

Response-type mappings:

- **Mirror**
  - preserve source overlap
  - strong/medium metric tolerance
  - weak steps deferred
  - follows source segment shape
  - may mirror source ending
  - Evidence: `ResponseBehaviourProfile.cs:284-307`.
- **Simplify**
  - reduce to anchors
  - spine-only metric tolerance
  - suppress weak steps
  - structural spine segment policy
  - limits direct overlap and far gaps
  - Evidence: `ResponseBehaviourProfile.cs:309-335`.
- **Complement**
  - sets `PreserveAnchors = false`
  - prefer gaps
  - fill-gap ending policy
  - sparse segment targeting
  - strong direct-overlap limits
  - Evidence: `ResponseBehaviourProfile.cs:337-364`.
- **Contrast**
  - sets `PreserveAnchors = false`
  - displaces source
  - broad metric tolerance
  - allows weak steps
  - avoids final accent
  - inverts source segment shape
  - Evidence: `ResponseBehaviourProfile.cs:366-394`.
- **Fill**
  - sets `PreserveAnchors = false`
  - interstitial placement
  - offbeat-friendly metric tolerance
  - prefer interstitial weak-step policy
  - guarantees interstitial content
  - Evidence: `ResponseBehaviourProfile.cs:396-425`.
- **Intensify**
  - sets `PreserveAnchors = false`
  - expands around source
  - medium-propulsion metric tolerance
  - late-drive ending policy
  - active late expansion
  - guarantees expansion and late drive
  - Evidence: `ResponseBehaviourProfile.cs:427-458`.

Important design implication:

- Planner may output `PreserveAnchors = true` for Complement, but the current skeleton profile overrides complement to `PreserveAnchors = false`.
- Evidence: planner rule at `ResponsePlanner.cs:197-203`; skeleton override at `ResponseBehaviourProfile.cs:337-340`.
- Impact: the scaffold generator prioritises complementarity over anchor preservation for Complement responses.

---

### 4.4 Scoring Logic

Status: **implemented/scaffold**.

Build flow:

- Validate request.
- Build derived maps.
- Build behaviour profile.
- Build per-step metadata.
- Convert to step scores.
- Build selection context.
- Select active steps.
- Return `SkeletonPattern`.
- Evidence: `SkeletonBuilder.cs:28-62`.

Raw score decomposition:

- `rawScore = metricScore + sourceRelationScore + anchorScore + endingScore + phraseBalanceScore + densityShapingScore + jitterOffset`.
- Evidence: `SkeletonBuilder.cs:171-179`.
- Each component is stored in `SkeletonStepMeta`.
- Evidence: `SkeletonStepMeta.cs:8-39`; assignment at `SkeletonBuilder.cs:219-252`.

Metric score:

- Uses metric salience, strong beat boost, and profile metric bias.
- Evidence: `SkeletonBuilder.cs:1998-2015`.
- The builder config gives metric weight high default influence: `MetricStrengthWeight = 0.88`, `StrongBeatPreferenceWeight = 0.85`.
- Evidence: `SkeletonBuilderConfig.cs:5-10`.

Source relation score:

- Uses:
  - source occupied
  - source anchor
  - source gap
  - adjacent/near/far source relation
  - interstitial source gap
  - local source density
  - segment source weight
  - source overlap policy
  - complementarity bias
- Evidence: `SkeletonBuilder.cs:2017-2099`.

Anchor score:

- Only active when profile preserves anchors and the step is a source anchor.
- Explicit anchors receive larger scale than fallback anchors.
- Evidence: `SkeletonBuilder.cs:2101-2113`.

Ending score:

- Only applies inside ending region.
- Depends on ending policy:
  - mirror source
  - structural only
  - fill gap
  - avoid accent
  - late drive
- Evidence: `SkeletonBuilder.cs:2115-2175`.

Phrase balance and density shaping:

- Phrase balance uses segment target weights and optional rebalancing.
- Evidence: `SkeletonBuilder.cs:2177-2195`.
- Density shaping adjusts metric class preferences for low/high target density and response metric tolerance policy.
- Evidence: `SkeletonBuilder.cs:2197-2304`.

Deterministic jitter:

- Default `SelectionJitter = 0.00`, so no jitter by default.
- Evidence: `SkeletonBuilderConfig.cs:18`.
- If enabled, jitter uses deterministic hash noise, not runtime randomness.
- Evidence: `SkeletonBuilder.cs:2306-2329`.

---

### 4.5 Selection Logic

Status: **implemented/scaffold**.

Selection target:

- `targetCount = round(targetDensity * totalSteps)`, clamped to at least one and at most total steps.
- Evidence: `SkeletonBuilder.cs:75`, `SkeletonBuilder.cs:1869-1882`.

Candidate ordering:

- Candidates sorted by score descending, then metrical weight descending, then lower step index.
- Evidence: `SkeletonBuilder.cs:72-73`, `SkeletonBuilder.cs:1630-1641`.

Selection passes:

- First pass uses profile spacing and soft constraints.
- If under target, spacing is relaxed.
- Then ending and behaviour-profile guarantees are enforced.
- Evidence: `SkeletonBuilder.cs:79-107`.

Anchor preservation:

- If `PreserveAnchors` is true, explicit anchors are selected first, then fallback anchors, then non-anchors.
- Evidence: `SkeletonBuilder.cs:416-454`.

Constraints:

- Rejects already selected steps, spacing violations, hard behaviour caps, reserved ending slots, weak candidates, behaviour policy deferrals, and segment-balance deferrals.
- Evidence: `SkeletonBuilder.cs:501-541`.
- Hard caps include structural-spine enforcement, direct overlap ratios, weak-step ratios, far-gap ratios, and adjacent run length.
- Evidence: `SkeletonBuilder.cs:683-729`.

Guarantees:

- Can force ending selection.
- Evidence: `SkeletonBuilder.cs:1100-1131`.
- Can ensure interstitial, adjacent displacement, expansion, medium metric, late segment, and late-drive ratios.
- Evidence: `SkeletonBuilder.cs:1133-1224`.

Debug / inspectability:

- `SkeletonReasonFlags` records reasons such as metric strong/weak, mirror boost, complement boost, anchor boost, ending boost, protected anchor, forced ending, interstitial boost, late-drive boost, segment target boost.
- Evidence: `SkeletonReasonFlags.cs:5-23`.
- `SkeletonDebugSnapshot` exposes active steps, selected indices, annotations, target density, achieved density, anchor alignment, and source overlap.
- Evidence: `SkeletonDebugSnapshot.cs:9-55`, annotations at `SkeletonDebugSnapshot.cs:128-172`.

---

## 5. Output Realisation

### 5.1 Live Audible Path

Status: **implemented/live but interim**.

Live preparation order:

- Analysis.
- Planning.
- Optional skeleton building.
- Feature transformation.
- Evidence: `AiResponsePreparationFlow.cs:67-119`.

Critical implementation caveat:

- The code explicitly says:
  - `ResponsePlan` is produced and exposed first.
  - generation still transforms the compiled human pattern directly.
  - "That gap remains explicit for now."
- Evidence: `AiResponsePreparationFlow.cs:110-115`.

Unit-test evidence:

- With no skeleton builder, generated pattern equals `new FeatureTransformer().Transform(compiledPattern)`.
- Evidence: `AiResponsePreparationFlowTests.cs:51-59`.
- With a skeleton builder, callback order is analysed -> planned -> skeleton -> generated, but generated pattern still equals `FeatureTransformer().Transform(compiledPattern)`.
- Evidence: `AiResponsePreparationFlowTests.cs:61-100`.

Runtime playback:

- `TurnLoopController` calls the preparation flow, stores analysis, plan, skeleton debug snapshot, and generated pattern.
- Evidence: `TurnLoopController.cs:881-895`, storage at `TurnLoopController.cs:1019-1048`.
- It plays `LastGeneratedAiPatternTurn`, not the `SkeletonPattern`.
- Evidence: `TurnLoopController.cs:918-927`.

Conclusion:

- The planner and skeleton are live as reasoning/debug stages.
- The audible AI response is still the `FeatureTransformer` output.

---

### 5.2 FeatureTransformer

Status: **implemented/live interim audible generator**.

Implementation:

- Class: `FeatureTransformer`.
- Evidence: `FeatureTransformer.cs:15-279`.

Inputs:

- `PatternTurn src`.
- Requires populated `velocity` and `offsetSamples`.
- Evidence: `FeatureTransformer.cs:45-60`, validation at `FeatureTransformer.cs:66-75`.

Internal features:

- `stepCount`
- `activeSteps`
- `density`
- `maxVelocity`
- `meanVelocity`
- `onsetIndices`
- gaps after onsets
- `maxGap`
- `meanGap`
- Evidence: `FeatureTransformer.cs:25-38`, extraction at `FeatureTransformer.cs:77-143`.

Modes:

- `Auto`
- `EchoAccent`
- `EndFill`
- `SparseOrnament`
- Evidence: `FeatureTransformer.cs:17-23`.

Auto rules:

- No active steps: clone original pattern.
- `maxGap < 6`: apply `SparseOrnament`.
- `meanGap < 3f`: apply `EchoAccent`.
- Otherwise: apply `EndFill`.
- Evidence: `FeatureTransformer.cs:146-164`.

Transform rules:

- `EchoAccent`:
  - accents are hits at least `0.8 * maxVelocity`
  - adds an echo 3 steps later if empty
  - echo velocity = `0.7 * source velocity`
  - offset for new hit = `0`
  - Evidence: `FeatureTransformer.cs:166-189`.
- `EndFill`:
  - inspects final 25% of turn
  - if at most two active hits there, adds hits at `N-6`, `N-3`, and `N-1`
  - velocity = `max(60, meanVelocity)`
  - offset for new hit = `0`
  - Evidence: `FeatureTransformer.cs:191-219`.
- `SparseOrnament`:
  - for each onset with following gap at least 2, adds onset+1 if empty
  - new velocity = `0.6 * source onset velocity`
  - offset for new hit = `0`
  - Evidence: `FeatureTransformer.cs:221-246`.

Output symbolic form:

- Transformer clones `PatternTurn`, preserving timing metadata and copying `velocity` / `offsetSamples`.
- It then mutates the cloned arrays.
- Evidence: `FeatureTransformer.cs:248-278`.

Playback:

- `IT4ChuckTurnPlayer.PlayTurn` pushes symbolic arrays to ChucK:
  - `stepCount`
  - `samplesPerStep`
  - `stepsPerQuarter`
  - `velocity`
  - `offsetSamples`
  - then broadcasts `playTurn`.
- Evidence: `IT4ChuckTurnPlayer.cs:79-159`.

Important limitation:

- New generated hits are grid-aligned because their `offsetSamples` are set to `0`.
- Evidence: `FeatureTransformer.cs:183-184`, `FeatureTransformer.cs:212-213`, `FeatureTransformer.cs:240-241`.

---

### 5.3 Relationship Between Planner, Skeleton, and Transformer

Implemented/live:

- `TurnAnalysisResult` is generated.
- `ResponsePlan` is generated.
- `SkeletonPattern` is generated when `SkeletonBuilder` is wired.
- `PatternTurn` is generated by `FeatureTransformer`.
- Evidence: `AiResponsePreparationFlow.cs:67-119`.

Interim:

- `FeatureTransformer` produces the currently audible response.
- It does not consume `ResponsePlan` or `SkeletonPattern`.
- Evidence: transform call receives only `compiledPattern` at `AiResponsePreparationFlow.cs:112-115`.

Implemented/scaffold:

- `SkeletonBuilder` derives a structural scaffold from source material plus plan intent.
- Evidence: `SkeletonBuilder.cs:28-62`.
- Its output is stored as a debug snapshot and emitted through `OnSkeletonGenerated`.
- Evidence: `TurnLoopController.cs:1029-1042`.

Deferred / not implemented:

- No live module converts `SkeletonPattern` into the final `PatternTurn`.
- No implemented live `PatternRealiser`, `MotifTransformer`, `EndingAdjuster`, or `ConstraintPass` chain appears in the current scripts.
- Local planning docs describe the intended chain as `ResponsePlan -> SkeletonBuilder -> MotifTransformer -> EndingAdjuster -> ConstraintPass -> PatternTurn`.
- Evidence: `Misc/Response/Skeleton Gen/skeleton_builder_spec_unified.md:32-37`.
- Earlier progress notes explicitly planned a later generator overhaul to replace the current mode-based generator.
- Evidence: `Misc/progress.md:80-100`.

---

## 6. Design Decisions and Rationale

### 6.1 Feature-Based Analysis

Implementation-supported rationale:

- The system reduces symbolic input into named, inspectable feature families.
- Evidence: separate feature classes and `TurnAnalysisResult` packaging at `TurnAnalysisResult.cs:8-13`.
- Progress notes name the intended feature families and describe `TurnAnalysisResult` as stable and interpretable.
- Evidence: `Misc/progress.md:5-23`.

Why this matters:

- The planner can reason using explicit descriptors rather than opaque latent state.
- Debug UI and tests can inspect individual feature families.

Careful inference:

- This is not evidence of a complete cognitive model of rhythm. It is evidence of an intentionally bounded, interpretable feature model.

---

### 6.2 Planning Separated from Generation

Implementation-supported rationale:

- `IResponsePlanner` consumes analysis and returns `ResponsePlan`; it has no `PatternTurn` generation API.
- Evidence: `IResponsePlanner.cs:6-9`.
- `ResponsePlan` is a compact data object, not a pattern.
- Evidence: `ResponsePlan.cs:6-34`.
- ResponsePlanner spec explicitly says the planner is a decision layer, not a generator.
- Evidence: `Misc/Response/Planning/ResponsePlanner_Spec.md:6-12`, non-responsibilities at `ResponsePlanner_Spec.md:67-73`.

Why this matters:

- Musical decision-making is separated from structural placement and final realisation.
- The planner can be evaluated independently of the audible generator.

---

### 6.3 Structural Generation Is Step-Based

Implementation-supported rationale:

- Skeleton generation operates over per-step maps and selects active step indices.
- Evidence: `SkeletonPattern.cs:8-13`; scoring loop at `SkeletonBuilder.cs:115-256`.
- Metric map defines salience per step.
- Evidence: `SkeletonMetricMapBuilder.cs:40-52`.
- The skeleton spec explicitly frames the builder as score-and-select, not template lookup, heavy search, or opaque generation.
- Evidence: `Misc/Response/Skeleton Gen/skeleton_builder_spec_unified.md:68-89`.

Why this matters:

- Step-based structure matches the quantised symbolic representation.
- It supports deterministic scoring, visible reason flags, and predictable constraints.

---

### 6.4 Current Output Realisation Relies on FeatureTransformer

Implementation-supported rationale:

- `AiResponsePreparationFlow` explicitly documents the current gap: plan is produced and exposed, but generation still transforms the compiled pattern directly.
- Evidence: `AiResponsePreparationFlow.cs:110-115`.
- Tests name skeleton generation as occurring before "temporary pattern generation".
- Evidence: `AiResponsePreparationFlowTests.cs:61-100`.
- Progress notes identify replacement of the current mode-based generator as later work.
- Evidence: `Misc/progress.md:80-100`.

Why this matters:

- The current prototype remains playable.
- The dissertation should distinguish between the implemented reasoning pipeline and the simpler audible realisation layer.

---

### 6.5 Deterministic / Inspectable Rather than End-to-End Learned

Implementation-supported rationale:

- Planner scoring is rule-based and threshold-configured.
- Evidence: `ResponsePlannerConfig.cs:3-41`, score rules at `ResponsePlanner.cs:234-319`.
- Planner tie-breaking is stable and explicit.
- Evidence: `ResponsePlanner.cs:350-380`, `ResponsePlanner.cs:450-469`.
- Skeleton selection is score-sorted and constraint-driven.
- Evidence: `SkeletonBuilder.cs:72-107`, `SkeletonBuilder.cs:501-541`.
- Default skeleton jitter is zero; if enabled, jitter is deterministic hash noise.
- Evidence: `SkeletonBuilderConfig.cs:18`, `SkeletonBuilder.cs:2306-2329`.
- Debug snapshots expose planner scores and skeleton selections.
- Evidence: `ResponsePlannerDebugSnapshot.cs:8-38`, `SkeletonDebugSnapshot.cs:9-55`.

Why this matters:

- Behaviour is easier to explain, tune, and test.
- The system is not currently an end-to-end learned generative model.

---

## 7. Trade-Offs and Limitations

1. **Interpretable feature extraction vs richer musical nuance**
   - Evidence: analysis is reduced to density, energy, anchors, end activity, and segment shapes (`TurnAnalysisResult.cs:8-13`).
   - Impact: the planner can explain its decisions, but subtle rhythmic identity, motif contour, performer intention, and style are only indirectly represented.

2. **Static thresholds vs adaptive performer-sensitive interpretation**
   - Evidence: energy defaults in `TurnLoopBootstrap.cs:22-28`; planner thresholds in `ResponsePlannerConfig.cs:5-27`.
   - Impact: the same velocity or density values have the same interpretation for all performers and contexts; there is no calibration or adaptation layer.

3. **Anchor salience heuristic vs deeper phrase analysis**
   - Evidence: anchor detection is a fixed weighted formula and threshold (`AnchorAnalyser.cs:11-17`, `AnchorAnalyser.cs:42-56`).
   - Impact: anchors are inspectable, but the system may miss phrase-level salience that depends on repetition, expectation, syncopation, or style.

4. **Four-segment phrase profile vs fine-grained temporal shape**
   - Evidence: `SegmentHelper.SegmentCount = 4` (`SegmentHelper.cs:5-8`); SAP requires exactly four values (`SegmentActivityProfileAnalyser.cs:32-35`).
   - Impact: the system can detect broad front/back/middle tendencies, but not detailed contour across smaller subdivisions.

5. **Deterministic planning vs emergent variation**
   - Evidence: deterministic tie preference (`ResponsePlanner.cs:450-469`) and deterministic same-input test (`ResponsePlannerTests.cs:216-229`).
   - Impact: behaviour is reproducible and debuggable, but less surprising or improvisational.

6. **Plan/skeleton sophistication vs current audible simplicity**
   - Evidence: skeleton is built before generation (`AiResponsePreparationFlow.cs:87-102`), but audible pattern comes from `FeatureTransformer.Transform(compiledPattern)` (`AiResponsePreparationFlow.cs:110-115`).
   - Impact: the system may make a sophisticated response plan that the audible response does not yet realise.

7. **Compact ResponsePlan vs richer musical control**
   - Evidence: `ResponsePlan` contains only type, target density, complementarity, anchor preservation, mirror ending, and length (`ResponsePlan.cs:8-13`).
   - Impact: there is no explicit target energy, velocity contour, timbre, rhythmic motif, microtiming feel, or orchestration plan.

8. **Metrical hierarchy vs syncopated/groove nuance**
   - Evidence: metric salience is fixed at 1.00 / 0.75 / 0.50 / 0.20 (`SkeletonMetricMapBuilder.cs:71-80`) and heavily weighted by defaults (`SkeletonBuilderConfig.cs:5-10`).
   - Impact: structural generation is stable and metrically legible, but weak/offbeat material needs extra justification and may be under-valued in syncopated contexts.

9. **Turn-local reasoning vs long-term musical memory**
   - Evidence: planner API consumes a single `TurnAnalysisResult` (`IResponsePlanner.cs:6-9`); preparation flow consumes one compiled `PatternTurn` (`AiResponsePreparationFlow.cs:48-53`).
   - Impact: the system does not currently build multi-turn strategy, remember motifs across exchanges, or develop a larger musical arc.

10. **Microtiming preservation for playback vs weak microtiming interpretation**
    - Evidence: playback sends `offsetSamples` to ChucK (`IT4ChuckTurnPlayer.cs:128-145`), but analyzers use velocity/step occupancy and not offsets (`DensityAnalyser.cs:18-22`, `EnergyAnalyser.cs:27-38`, `AnchorAnalyser.cs:36-47`).
    - Impact: captured microtiming can be preserved for existing hits, but planning does not reason about looseness, swing, drag, or expressive timing; new transformer hits use offset `0`.

11. **Single-stream symbolic rhythm vs richer multi-voice interaction**
    - Evidence: `PatternTurn` has one `velocity` array and one `offsetSamples` array (`PatternTurn.cs:23-27`); ChucK playback consumes one velocity array (`IT4ChuckTurnPlayer.cs:128-145`).
    - Impact: the system reasons about a single rhythmic stream rather than separate drum voices, timbral roles, or orchestrated call-and-response.

12. **Constraint-aware skeletons vs expressive spontaneity**
    - Evidence: skeleton selection enforces target density, spacing, behaviour caps, segment balance, and guarantees (`SkeletonBuilder.cs:501-541`, `SkeletonBuilder.cs:683-729`, `SkeletonBuilder.cs:1133-1224`).
    - Impact: the scaffold is controlled and inspectable, but generation is bounded by hand-authored constraints and may feel less fluid than unconstrained improvisation.

13. **Current complement behaviour vs anchor continuity**
    - Evidence: planner can allow complement anchor preservation (`ResponsePlanner.cs:197-203`), but skeleton complement profile sets `PreserveAnchors = false` (`ResponseBehaviourProfile.cs:337-340`).
    - Impact: complement responses may avoid source material more strongly than the plan nominally suggests, reducing continuity with salient source events.

14. **FeatureTransformer immediacy vs planner alignment**
    - Evidence: `FeatureTransformer` auto mode only uses gap/density-like features and velocity summaries (`FeatureTransformer.cs:25-38`, `FeatureTransformer.cs:146-164`).
    - Impact: the audible output is quick and simple, but it cannot express response types such as Contrast or Complement in the way the planner and skeleton describe them.

---

## 8. Negative Evidence: What Is Not Implemented

1. **No long-horizon memory or multi-turn strategy**
   - Evidence: `IResponsePlanner.Plan` accepts only one `TurnAnalysisResult` (`IResponsePlanner.cs:6-9`); `AiResponsePreparationFlow.Prepare` accepts one compiled `PatternTurn` (`AiResponsePreparationFlow.cs:48-53`).
   - Effect: responses are locally reactive, not strategically shaped across multiple calls and responses.

2. **No fully realised plan-to-performance generator**
   - Evidence: `ResponsePlan` is created, skeleton may be built, but final generated pattern comes from `FeatureTransformer.Transform(compiledPattern)` (`AiResponsePreparationFlow.cs:110-115`).
   - Effect: the intended response plan is not yet the direct source of the audible performance.

3. **No learned model in the live response loop**
   - Evidence: response choice is implemented through explicit scoring rules (`ResponsePlanner.cs:234-319`); skeleton through score-and-select functions (`SkeletonBuilder.cs:28-62`, `SkeletonBuilder.cs:1998-2304`); transformer through hand-authored rules (`FeatureTransformer.cs:146-246`).
   - Effect: behaviour is inspectable and tunable, but not data-learned or style-adaptive.

4. **No microtiming-aware planning**
   - Evidence: analysis features inspect `velocity` and step indices, not `offsetSamples`; planner inputs contain density, energy, anchors, end activity, and SAP only (`TurnAnalysisResult.cs:8-13`).
   - Effect: expressive timing is not interpreted as a planning signal.

5. **No rich timbral or orchestration reasoning**
   - Evidence: `PatternTurn` represents a single velocity stream (`PatternTurn.cs:23-27`); `ResponsePlan` has no timbre/instrument fields (`ResponsePlan.cs:8-13`).
   - Effect: generated responses are rhythmic placements/velocities, not multi-instrument orchestrations.

6. **No autonomous aesthetic evaluation of generated responses**
   - Evidence: controller plays `LastGeneratedAiPatternTurn` immediately after preparation (`TurnLoopController.cs:918-927`); skeleton summary reports counts/density/overlap, not musical quality (`SkeletonPatternSummary.cs:6-29`).
   - Effect: there is no critic/evaluator stage that accepts, rejects, or revises a generated response before playback.

7. **No implemented final motif-transform / ending-adjust / constraint-pass chain**
   - Evidence: skeleton spec describes that intended chain (`skeleton_builder_spec_unified.md:32-37`), but the live flow after skeleton calls `FeatureTransformer` directly (`AiResponsePreparationFlow.cs:110-115`).
   - Effect: structural generation has not yet become a full response-realisation pipeline.

8. **No computed anchor-support feature**
   - Evidence: `AnchorSupportFeatures` exists (`AnchorSupportFeatures.cs:6-28`) but `TurnAnalyser` returns the constructor overload that defaults it (`TurnAnalyser.cs:41-46`, `TurnAnalysisResult.cs:26-39`).
   - Effect: potential weak-hit/support information is not available to the planner.

9. **No stochastic improvisational choice by default**
   - Evidence: planner deterministic tie logic (`ResponsePlanner.cs:450-469`); skeleton `SelectionJitter = 0.00` by default (`SkeletonBuilderConfig.cs:18`) and summary reports `usedStochasticTieBreak: false` (`SkeletonBuilder.cs:2369-2375`).
   - Effect: repeat inputs normally produce repeat decisions and scaffolds, which supports evaluation but limits spontaneous variation.

---

## 9. Claim Candidates

1. **The system's musical intelligence is mediated through interpretable feature extraction rather than latent inference.**
   - Evidence: explicit feature families in `TurnAnalysisResult.cs:8-13`; rule modules in `TurnAnalyser.cs:35-40`.

2. **The analysis layer separates occupancy, intensity, salience, closure, and broad phrase contour into distinct feature families.**
   - Evidence: `DensityAnalyser`, `EnergyAnalyser`, `AnchorAnalyser`, `EndActivityAnalyser`, `SegmentActivityProfileAnalyser`.

3. **Density is treated as grid occupancy, not as raw event timing.**
   - Evidence: active steps are counted using `velocity[i] > 0` in `DensityAnalyser.cs:18-27`.

4. **Energy is treated as velocity-driven intensity over active hits, not simply as rhythmic busyness.**
   - Evidence: `EnergyAnalyser.cs:27-45`; `EnergyFeatures.cs:9-18`.

5. **Anchor detection gives the system a first-order model of structural salience within a turn.**
   - Evidence: weighted salience model in `AnchorAnalyser.cs:42-47`; anchor outputs in `AnchorFeatures.cs:9-18`.

6. **End activity provides a separate signal for closure rather than folding endings into global density or energy.**
   - Evidence: final-quarter analysis in `EndActivityAnalyser.cs:14-50`; planner ending descriptors in `ResponsePlanner.cs:65-74`.

7. **Segment Activity Profile converts local density and energy into coarse phrase-shape descriptors used by planning.**
   - Evidence: `SegmentActivityProfileAnalyser.cs:17-30`, shape classifier at `SegmentActivityProfileAnalyser.cs:32-81`.

8. **Response planning separates musical decision-making from response realisation.**
   - Evidence: `IResponsePlanner.Plan(TurnAnalysisResult) -> ResponsePlan` (`IResponsePlanner.cs:6-9`); spec says planner is not a generator (`ResponsePlanner_Spec.md:6-12`).

9. **The planner chooses between named musical relationships rather than generating notes directly.**
   - Evidence: `ResponseType.cs:3-10`; `ResponsePlanner.cs:221-231`.

10. **ResponsePlan is a compact, inspectable intent representation.**
    - Evidence: explicit fields in `ResponsePlan.cs:8-13`; debug snapshot stores final plan at `ResponsePlannerDebugSnapshot.cs:34-38`.

11. **Planner behaviour is deterministic and tunable through explicit thresholds and weights.**
    - Evidence: `ResponsePlannerConfig.cs:3-41`; deterministic test at `ResponsePlannerTests.cs:216-229`.

12. **Skeleton generation provides a scaffold for response structure even where final output remains rule-based.**
    - Evidence: `SkeletonBuilder.BuildSkeleton` returns `SkeletonPattern` (`SkeletonBuilder.cs:28-62`), but audible generation still uses `FeatureTransformer` (`AiResponsePreparationFlow.cs:110-115`).

13. **Structural generation derives response positions through metrical weighting, source relationship, anchors, endings, phrase balance, and density shaping.**
    - Evidence: raw score decomposition at `SkeletonBuilder.cs:171-179`.

14. **The skeleton generator is response-type-sensitive: mirror, complement, simplify, contrast, fill, and intensify map to different placement policies.**
    - Evidence: profile configurations at `ResponseBehaviourProfile.cs:284-458`.

15. **The current response pipeline is hybrid: structurally ambitious in planning and skeleton generation, but interim in audible generation.**
    - Evidence: plan/skeleton stages in `AiResponsePreparationFlow.cs:67-102`; transformer caveat at `AiResponsePreparationFlow.cs:110-115`.

16. **The current audible response is a symbolic rule transformation of the source pattern, not a realised performance of the ResponsePlan.**
    - Evidence: `FeatureTransformer.Transform(compiledPattern)` at `AiResponsePreparationFlow.cs:112-115`; transformer modes at `FeatureTransformer.cs:17-23`.

17. **The system favours inspectability and research legibility over opaque end-to-end learned generation.**
    - Evidence: planner debug snapshots (`ResponsePlannerDebugSnapshot.cs:8-38`), skeleton reason flags (`SkeletonReasonFlags.cs:5-23`), deterministic rule code throughout planner/skeleton.

18. **The most important implementation limitation for Section 3.5 is the gap between planner/skeleton intent and live audio realisation.**
    - Evidence: explicit code comment at `AiResponsePreparationFlow.cs:110-115`; unit test confirming generated output remains `FeatureTransformer` output at `AiResponsePreparationFlowTests.cs:91-100`.

