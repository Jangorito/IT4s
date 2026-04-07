# Turn Analysis Architecture — Codex Treatment

## 1. System Role

Turn Analysis converts a compiled PatternTurn into a structured set of feature objects used by the ResponsePlanner.

It operates on quantised step-grid data and must be:

- deterministic
- lightweight
- real-time safe
- modular (feature families separated)

---

## 2. Input Contract

Turn Analysis expects a valid PatternTurn:

Required fields:

- velocity[] (int array, length = StepCount)
- StepCount (int)

Assumptions:

- velocity[i] >= 0
- velocity[i] > 0 indicates a hit
- arrays are fully populated and aligned

---

## 3. Output Contract

Turn Analysis produces a structured result composed of feature families.

Example structure:

```csharp
public sealed class TurnAnalysisResult
{
    public DensityFeatures Density { get; init; }
    public EnergyFeatures Energy { get; init; }
    public AnchorFeatures Anchor { get; init; }
    public EndActivityFeatures EndActivity { get; init; }
    public SegmentActivityProfileFeatures SegmentActivityProfile { get; init; }
}
```

---

## 4. Density Feature Specification

### 4.1 Purpose

Density measures occupancy of the step grid at both global and local levels.

---

### 4.2 Data Structure

```csharp
public sealed class DensityFeatures
{
    public int StepCount { get; init; }
    public int ActiveStepCount { get; init; }
    public int InactiveStepCount { get; init; }

    public float StepDensity { get; init; }

    public IReadOnlyList<float> SegmentDensities { get; init; } // length = 4
}
```

---

### 4.3 Algorithm

#### Step 1 — Global counts

```csharp
int N = pattern.StepCount;
int active = 0;

for (int i = 0; i < N; i++)
{
    if (pattern.velocity[i] > 0)
        active++;
}
```

---

#### Step 2 — Global density

```csharp
float stepDensity = N > 0
    ? (float)active / N
    : 0f;
```

---

#### Step 3 — Segment splitting

Fixed segment count:

```csharp
const int SEGMENTS = 4;
```

Compute base size and remainder:

```csharp
int baseSize = N / SEGMENTS;
int remainder = N % SEGMENTS;
```

Segment sizes:

```csharp
int size0 = baseSize + (remainder > 0 ? 1 : 0);
int size1 = baseSize + (remainder > 1 ? 1 : 0);
int size2 = baseSize + (remainder > 2 ? 1 : 0);
int size3 = baseSize;
```

---

#### Step 4 — Segment densities

For each segment:

```csharp
int segmentActive = 0;

for (int i = start; i < end; i++)
{
    if (pattern.velocity[i] > 0)
        segmentActive++;
}

float segmentDensity = segmentLength > 0
    ? (float)segmentActive / segmentLength
    : 0f;
```

Store all 4 values in order.

---

### 4.4 Edge Cases

Empty turn (N == 0):
- StepDensity = 0
- SegmentDensities = [0,0,0,0]

No hits:
- StepDensity = 0
- all segments = 0

Fully dense:
- StepDensity = 1
- all segments = 1

Multiple hits per step:
- count as a single active step

---

### 4.5 Constraints

- Must run in O(N)
- Segment array length is fixed at 4
- No dependency on external state

---

### 4.6 Integration Notes

- Should be implemented as a dedicated DensityAnalyzer or within a modular analysis pipeline
- Output should be immutable after creation

---

## 5. Energy Feature Specification

### 5.1 Purpose

Energy measures the velocity-driven intensity profile of a turn at both global and local levels.

This feature family is intentionally velocity-based only.

Do not mix timing-derived intensity proxies into EnergyFeatures. Perceived energy arising from note clustering, burst frequency, or occupancy should remain the responsibility of density or later structural features.

---

### 5.2 Data Structure

```csharp
public sealed class EnergyFeatures
{
    public float MeanVelocity { get; init; }
    public int PeakVelocity { get; init; }
    public float VelocityVariance { get; init; }

    public IReadOnlyList<float> SegmentMeanVelocities { get; init; } // length = 4

    public bool IsHighEnergy { get; init; }
    public bool IsLowEnergy { get; init; }
    public bool IsFlatEnergy { get; init; }
    public bool IsAccented { get; init; }
    public bool IsCrescendo { get; init; }
    public bool IsDecrescendo { get; init; }
}
```

Notes:

- MeanVelocity should be computed over active steps only
- PeakVelocity is the maximum velocity among active steps
- VelocityVariance should be computed over active-step velocities only
- SegmentMeanVelocities should use the same 4-segment partitioning scheme as DensityFeatures
- A segment with no active steps should produce a mean velocity of 0

---

### 5.3 Algorithm

#### Step 1 — Gather active-step velocities

```csharp
int N = pattern.StepCount;

int activeCount = 0;
int peak = 0;
float sum = 0f;

for (int i = 0; i < N; i++)
{
    int v = pattern.velocity[i];
    if (v > 0)
    {
        activeCount++;
        sum += v;
        if (v > peak)
            peak = v;
    }
}
```

---

#### Step 2 — Mean velocity

```csharp
float meanVelocity = activeCount > 0
    ? sum / activeCount
    : 0f;
```

---

#### Step 3 — Velocity variance

Use population variance over active-step velocities:

```csharp
float varianceSum = 0f;

for (int i = 0; i < N; i++)
{
    int v = pattern.velocity[i];
    if (v > 0)
    {
        float delta = v - meanVelocity;
        varianceSum += delta * delta;
    }
}

float velocityVariance = activeCount > 0
    ? varianceSum / activeCount
    : 0f;
```

---

#### Step 4 — Segment mean velocities

Use the same fixed 4-segment splitting logic as density.

For each segment:

```csharp
float segmentSum = 0f;
int segmentActiveCount = 0;

for (int i = start; i < end; i++)
{
    int v = pattern.velocity[i];
    if (v > 0)
    {
        segmentSum += v;
        segmentActiveCount++;
    }
}

float segmentMeanVelocity = segmentActiveCount > 0
    ? segmentSum / segmentActiveCount
    : 0f;
```

Store all 4 values in order.

---

### 5.4 Trait Derivation

Traits should be derived after raw metrics are computed.

Threshold values should be configurable, not hardcoded into planner logic.

Suggested threshold container:

```csharp
public sealed class EnergyThresholds
{
    public float HighEnergyMeanVelocity { get; init; }
    public float LowEnergyMeanVelocity { get; init; }
    public float FlatVarianceThreshold { get; init; }
    public float AccentPeakOverMeanThreshold { get; init; }
    public float CrescendoMinDelta { get; init; }
    public float DecrescendoMinDelta { get; init; }
}
```

#### High / low energy

```csharp
bool isHighEnergy = meanVelocity >= thresholds.HighEnergyMeanVelocity;
bool isLowEnergy = activeCount == 0 || meanVelocity <= thresholds.LowEnergyMeanVelocity;
```

#### Flat energy

```csharp
bool isFlatEnergy = velocityVariance <= thresholds.FlatVarianceThreshold;
```

#### Accented

Accent detection should compare peak against average level.

Simple version:

```csharp
bool isAccented = activeCount > 0 &&
                  (peak - meanVelocity) >= thresholds.AccentPeakOverMeanThreshold;
```

A ratio-based variant can be substituted later if required, but the first implementation should stay simple and interpretable.

#### Crescendo / decrescendo

These traits should be based on the segment mean profile, not on individual-hit ordering.

Recommended first implementation:

```csharp
float first = segmentMeans[0];
float last = segmentMeans[3];
float delta = last - first;

bool isCrescendo = delta >= thresholds.CrescendoMinDelta;
bool isDecrescendo = delta <= -thresholds.DecrescendoMinDelta;
```

A stricter monotonic or regression-based trend test can be added later if needed, but v1 should remain lightweight.

---

### 5.5 Edge Cases

Empty turn (N == 0):
- MeanVelocity = 0
- PeakVelocity = 0
- VelocityVariance = 0
- SegmentMeanVelocities = [0,0,0,0]
- IsLowEnergy = true
- all other traits = false

No hits:
- same as empty turn

Single active hit:
- MeanVelocity = PeakVelocity = hit velocity
- VelocityVariance = 0
- IsAccented should typically be false unless thresholds explicitly allow it

Segments with no hits:
- segment mean = 0

---

### 5.6 Constraints

- Must run in O(N)
- Segment array length is fixed at 4
- Must not depend on external state
- Must be deterministic for identical PatternTurn input and thresholds
- Should not allocate unnecessary intermediate collections inside per-frame hot paths

---

### 5.7 Integration Notes

- Implement as a dedicated EnergyAnalyzer or equivalent modular analysis stage
- Use the same segment partitioning helper as DensityAnalyzer to avoid divergence
- Output should be immutable after creation
- ResponsePlanner should primarily consume derived traits, but raw metrics should remain accessible for debugging, tuning, and future planner refinements

---

## 6. Anchor Feature Specification

### 6.1 Purpose

Anchor Detection identifies structurally salient active steps within a PatternTurn.

An anchor is an active step whose computed salience score meets or exceeds a fixed anchor threshold.

This feature family is intended to provide event-level structural importance, not just global descriptive summaries. It should therefore expose both per-step data and compact aggregate summaries for planner use.

---

### 6.2 Data Structure

```csharp
public sealed class AnchorFeatures
{
    public int StepCount { get; init; }

    public IReadOnlyList<float> StepSalienceScores { get; init; }   // length = StepCount
    public IReadOnlyList<bool> StepIsAnchor { get; init; }          // length = StepCount

    public int AnchorCount { get; init; }
    public IReadOnlyList<int> AnchorIndices { get; init; }          // ascending order

    public int? StrongestAnchorIndex { get; init; }
    public float StrongestAnchorScore { get; init; }

    public bool HasOpeningAnchor { get; init; }
    public bool HasClosingAnchor { get; init; }

    public IReadOnlyList<int> AnchorCountsPerSegment { get; init; } // length = 4
}
```

Output guarantees:

- StepSalienceScores length = StepCount
- StepIsAnchor length = StepCount
- inactive steps must always have salience 0 and anchor flag false
- AnchorIndices must be sorted in ascending order
- AnchorCount must equal AnchorIndices.Count
- StrongestAnchorIndex must be null when AnchorCount == 0
- StrongestAnchorScore must be 0 when AnchorCount == 0
- AnchorCountsPerSegment length = 4

---

### 6.3 Fixed Constants

Use the following fixed v1 constants:

```csharp
const float VELOCITY_WEIGHT = 0.40f;
const float ACCENT_WEIGHT = 0.30f;
const float ISOLATION_WEIGHT = 0.20f;
const float POSITION_WEIGHT = 0.10f;

const float ANCHOR_THRESHOLD = 0.55f;
const int MAX_VELOCITY = 127;
const int LOCAL_RADIUS = 2;
const int SEGMENTS = 4;
```

These values are part of the locked v1 specification.

---

### 6.4 Salience Formula

For each active step `i`:

```csharp
AnchorSalience(i) =
    0.40f * VelocityScore(i)
  + 0.30f * LocalAccentScore(i)
  + 0.20f * IsolationScore(i)
  + 0.10f * PositionalScore(i);
```

For inactive steps:

```csharp
AnchorSalience(i) = 0f;
```

Binary rule:

```csharp
IsAnchor(i) = pattern.velocity[i] > 0 && AnchorSalience(i) >= 0.55f;
```

---

### 6.5 Component Definitions

#### 6.5.1 VelocityScore

```csharp
float VelocityScore(int velocity)
{
    return Math.Clamp((float)velocity / MAX_VELOCITY, 0f, 1f);
}
```

This is the absolute velocity contribution.

---

#### 6.5.2 LocalAccentScore

Neighbourhood window:
- indices from `i - 2` to `i + 2`
- exclude `i`
- include only active neighbouring steps when computing local mean

Algorithm:

```csharp
float LocalAccentScore(PatternTurn pattern, int i)
{
    float sum = 0f;
    int count = 0;

    for (int j = i - LOCAL_RADIUS; j <= i + LOCAL_RADIUS; j++)
    {
        if (j == i)
            continue;
        if (j < 0 || j >= pattern.StepCount)
            continue;

        int v = pattern.velocity[j];
        if (v > 0)
        {
            sum += v;
            count++;
        }
    }

    if (count == 0)
        return 0f;

    float localMean = sum / count;
    float score = (pattern.velocity[i] - localMean) / MAX_VELOCITY;
    return Math.Clamp(score, 0f, 1f);
}
```

Notes:
- negative differences should clamp to 0
- no active neighbours means no local accent bonus

---

#### 6.5.3 IsolationScore

Neighbourhood window:
- indices from `i - 2` to `i + 2`
- exclude `i`
- treat out-of-bounds positions as inactive

Algorithm:

```csharp
float IsolationScore(PatternTurn pattern, int i)
{
    int inactiveCount = 0;

    for (int j = i - LOCAL_RADIUS; j <= i + LOCAL_RADIUS; j++)
    {
        if (j == i)
            continue;

        if (j < 0 || j >= pattern.StepCount)
        {
            inactiveCount++;
            continue;
        }

        if (pattern.velocity[j] == 0)
            inactiveCount++;
    }

    return inactiveCount / 4f;
}
```

Interpretation:
- 0 = fully surrounded by activity
- 1 = fully isolated in the local window

---

#### 6.5.4 PositionalScore

A step receives a positional bonus only if it is the first active step or the last active step in the turn.

```csharp
float PositionalScore(int i, int? firstActiveIndex, int? lastActiveIndex)
{
    if (!firstActiveIndex.HasValue || !lastActiveIndex.HasValue)
        return 0f;

    if (i == firstActiveIndex.Value || i == lastActiveIndex.Value)
        return 1f;

    return 0f;
}
```

If the same step is both first and last active, the score remains 1.

---

### 6.6 Algorithm

#### Step 1 — Precompute boundary indices

Find:
- first active step index
- last active step index

If there are no active steps, both remain null.

---

#### Step 2 — Compute salience and flags

Allocate:
- float salience[StepCount]
- bool isAnchor[StepCount]

For each step:
- if inactive: salience = 0, isAnchor = false
- if active: compute component scores, combine via weighted sum, then threshold

---

#### Step 3 — Collect aggregate summaries

Build:
- AnchorIndices
- AnchorCount
- StrongestAnchorIndex
- StrongestAnchorScore
- HasOpeningAnchor
- HasClosingAnchor
- AnchorCountsPerSegment

When two anchors share the same salience, StrongestAnchorIndex must resolve to the earliest such index for deterministic behaviour.

---

#### Step 4 — Segment counts

Use the same 4-segment partitioning logic already used by DensityFeatures and EnergyFeatures.

Count how many anchor indices fall into each segment.

---

### 6.7 Edge Cases

Empty turn (StepCount == 0):
- StepSalienceScores = []
- StepIsAnchor = []
- AnchorCount = 0
- AnchorIndices = []
- StrongestAnchorIndex = null
- StrongestAnchorScore = 0
- HasOpeningAnchor = false
- HasClosingAnchor = false
- AnchorCountsPerSegment = [0,0,0,0]

No active hits:
- same behaviour as above except StepSalienceScores and StepIsAnchor should still have length StepCount if StepCount > 0

Single active hit:
- VelocityScore computed normally
- LocalAccentScore = 0
- IsolationScore = 1
- PositionalScore = 1
- that hit may become an anchor if total salience >= threshold

Dense flat pattern:
- LocalAccentScore tends toward 0
- IsolationScore tends toward low values
- anchors should be uncommon unless velocity is sufficiently high

Equal strongest anchor scores:
- earliest index wins for StrongestAnchorIndex

---

### 6.8 Constraints

- Must run in O(N)
- Must be deterministic
- Must not use randomness
- Must not depend on external state
- Must be real-time safe
- Should avoid unnecessary allocations inside hot paths beyond the required result containers

---

### 6.9 Integration Notes

- Implement as a dedicated AnchorAnalyzer or equivalent modular analysis stage
- Reuse the same segment partition helper used by DensityAnalyzer and EnergyAnalyzer
- Output should be immutable after creation
- ResponsePlanner may consume:
  - AnchorCount
  - StrongestAnchorIndex
  - HasOpeningAnchor
  - HasClosingAnchor
  - AnchorCountsPerSegment
- Full step salience and flags should remain available for debugging, testing, and future planner refinement

---

## 7. End Activity Feature Specification

### 7.1 Purpose

End Activity captures phrase-ending behaviour by analysing the final portion of a turn.

It is intended to help the system distinguish between endings that taper away, remain sustained, or land with a final accent. This feature family is therefore specifically about phrase closure rather than whole-turn description.

---

### 7.2 Data Structure

```csharp
public sealed class EndActivityFeatures
{
    public float EndDensity { get; init; }
    public float EndEnergy { get; init; }
    public int EndAccent { get; init; }
}
```

Notes:

- EndDensity is normalized to the range 0–1
- EndEnergy is the mean velocity of active steps in the end window
- EndAccent is the maximum velocity found in the end window
- if the end window contains no active steps, EndEnergy = 0 and EndAccent = 0

---

### 7.3 Window Definition

The end window is fixed as the final 25% of the turn.

```csharp
int endStart = (int)(N * 0.75f);
```

The window includes all steps from `endStart` to `N - 1`.

This is a locked v1 decision.

---

### 7.4 Algorithm

#### Step 1 — Initialise

```csharp
int N = pattern.StepCount;
int endStart = (int)(N * 0.75f);

int activeCount = 0;
float velocitySum = 0f;
int maxVelocity = 0;
```

#### Step 2 — Scan end window

```csharp
for (int i = endStart; i < N; i++)
{
    int v = pattern.velocity[i];

    if (v > 0)
    {
        activeCount++;
        velocitySum += v;

        if (v > maxVelocity)
            maxVelocity = v;
    }
}
```

#### Step 3 — Derive outputs

```csharp
int windowSize = N - endStart;

float endDensity = windowSize > 0
    ? (float)activeCount / windowSize
    : 0f;

float endEnergy = activeCount > 0
    ? velocitySum / activeCount
    : 0f;

int endAccent = maxVelocity;
```

---

### 7.5 Semantics

Interpretation of the three outputs:

- EndDensity describes how busy the ending region is
- EndEnergy describes how forcefully the ending region is articulated
- EndAccent captures whether the ending contains a pronounced final punch

These values are intentionally simple and planner-friendly.

---

### 7.6 Edge Cases

Empty turn (N == 0):
- EndDensity = 0
- EndEnergy = 0
- EndAccent = 0

No active steps in end window:
- EndDensity = 0
- EndEnergy = 0
- EndAccent = 0

Single active step in end window:
- EndDensity depends on window size
- EndEnergy = that hit velocity
- EndAccent = that hit velocity

Fully active end window:
- EndDensity = 1

---

### 7.7 Constraints

- Must run in O(N), though practically only the final quarter is scanned
- Must be deterministic
- Must not depend on external state
- Must allocate no unnecessary intermediate collections

---

### 7.8 Integration Notes

- Implement as a dedicated EndActivityAnalyzer or equivalent modular stage
- Add EndActivity to TurnAnalysisResult
- ResponsePlanner may later use this feature family to drive:
  - closing mirrors
  - contrastive replies
  - fill-like answers
  - continuation vs resolution choices

---

## 8. Segment Activity Profile Feature Specification

### 8.1 Purpose

Segment Activity Profile (SAP) is a derived interpretation layer built on top of the existing four-segment density and energy values.

It does not introduce a new raw extractor. Instead, it classifies the temporal shape of activity across the turn so that the ResponsePlanner can reason about whether density and intensity are flat, rising, falling, edge-weighted, or centre-weighted.

This keeps the architecture compact while still adding phrase-shape awareness.

---

### 8.2 Design Position

SAP must be derived from:

- `DensityFeatures.SegmentDensities`
- `EnergyFeatures.SegmentMeanVelocities`

It must not rescan the PatternTurn or duplicate density / energy extraction logic.

This is a locked v1 design decision.

---

### 8.3 Shared Enum

Density shape and energy shape must use the same shared categorical vocabulary:

```csharp
public enum ActivityShape
{
    Flat,
    Increasing,
    Decreasing,
    FrontLoaded,
    BackLoaded,
    MidPeak,
    MidDip
}
```

This shared enum improves consistency and makes planner rules easier to express.

---

### 8.4 Data Structure

```csharp
public sealed class SegmentActivityProfileFeatures
{
    public ActivityShape DensityShape { get; init; }
    public ActivityShape EnergyShape { get; init; }
}
```

v1 deliberately stops here.

Do not add a combined trajectory label such as `CombinedShape`, `OverallShape`, or `PhraseProfile` in v1. The planner should combine `DensityShape` and `EnergyShape` later if needed.

---

### 8.5 Input Contract

The classifier expects exactly four ordered segment values.

For density shape:

```csharp
IReadOnlyList<float> densitySegments // length = 4
```

For energy shape:

```csharp
IReadOnlyList<float> energySegments // length = 4
```

Values must already be normalised or semantically valid according to their source feature family.

---

### 8.6 Epsilon Tolerance

Shape classification must ignore small fluctuations caused by noise or quantisation artifacts.

Use an epsilon threshold:

```csharp
public sealed class SegmentActivityProfileThresholds
{
    public float ShapeEpsilon { get; init; }
}
```

Helper:

```csharp
float NormalizeDelta(float delta, float epsilon)
{
    return Math.Abs(delta) <= epsilon ? 0f : delta;
}
```

All adjacent differences must be passed through this epsilon filter before rule evaluation.

---

### 8.7 Derived Deltas

Given four segment values:

```csharp
float s0 = segments[0];
float s1 = segments[1];
float s2 = segments[2];
float s3 = segments[3];
```

Compute:

```csharp
float d01 = NormalizeDelta(s1 - s0, epsilon);
float d12 = NormalizeDelta(s2 - s1, epsilon);
float d23 = NormalizeDelta(s3 - s2, epsilon);
```

These deltas are used by the ordered heuristic rules below.

---

### 8.8 Ordered Classification Rules

The classifier must apply rules in this exact order for deterministic behaviour.

#### Rule 1 — Flat

Return `Flat` if all adjacent deltas are zero after epsilon filtering.

```csharp
if (d01 == 0f && d12 == 0f && d23 == 0f)
    return ActivityShape.Flat;
```

---

#### Rule 2 — Increasing

Return `Increasing` if the overall trajectory is upward without contradictory movement strong enough to break the shape.

Recommended v1 rule:

```csharp
bool nonDecreasing = d01 >= 0f && d12 >= 0f && d23 >= 0f;
bool overallUp = (s3 - s0) > epsilon;

if (nonDecreasing && overallUp)
    return ActivityShape.Increasing;
```

---

#### Rule 3 — Decreasing

Return `Decreasing` if the overall trajectory is downward without contradictory movement strong enough to break the shape.

Recommended v1 rule:

```csharp
bool nonIncreasing = d01 <= 0f && d12 <= 0f && d23 <= 0f;
bool overallDown = (s0 - s3) > epsilon;

if (nonIncreasing && overallDown)
    return ActivityShape.Decreasing;
```

---

#### Rule 4 — FrontLoaded

Return `FrontLoaded` when the early part of the turn clearly dominates the later part.

Recommended v1 heuristic:

```csharp
float earlyMean = (s0 + s1) * 0.5f;
float lateMean = (s2 + s3) * 0.5f;

if ((earlyMean - lateMean) > epsilon && s0 >= s3)
    return ActivityShape.FrontLoaded;
```

This category is intended for profiles that are stronger at the front edge without being cleanly monotonic enough to count as `Decreasing`.

---

#### Rule 5 — BackLoaded

Return `BackLoaded` when the later part of the turn clearly dominates the early part.

Recommended v1 heuristic:

```csharp
float earlyMean = (s0 + s1) * 0.5f;
float lateMean = (s2 + s3) * 0.5f;

if ((lateMean - earlyMean) > epsilon && s3 >= s0)
    return ActivityShape.BackLoaded;
```

This category is intended for profiles that are stronger at the back edge without being cleanly monotonic enough to count as `Increasing`.

---

#### Rule 6 — MidPeak

Return `MidPeak` when the middle of the turn is stronger than both edges.

Recommended v1 heuristic:

```csharp
float middleMean = (s1 + s2) * 0.5f;
float edgeMean = (s0 + s3) * 0.5f;

if ((middleMean - edgeMean) > epsilon)
    return ActivityShape.MidPeak;
```

---

#### Rule 7 — MidDip

Return `MidDip` when the middle of the turn is weaker than both edges.

Recommended v1 heuristic:

```csharp
float middleMean = (s1 + s2) * 0.5f;
float edgeMean = (s0 + s3) * 0.5f;

if ((edgeMean - middleMean) > epsilon)
    return ActivityShape.MidDip;
```

---

#### Rule 8 — Fallback

If no earlier rule matches, return `Flat`.

```csharp
return ActivityShape.Flat;
```

This conservative fallback is intentional for v1.

---

### 8.9 Why the Rule Order Matters

The ordering is part of the locked design.

The classifier should privilege:

1. obvious no-shape behaviour (`Flat`)
2. clear directional trends (`Increasing`, `Decreasing`)
3. edge-dominant shapes (`FrontLoaded`, `BackLoaded`)
4. middle-dominant shapes (`MidPeak`, `MidDip`)

This ordering keeps the categories interpretable and prevents a profile from being classified as a weaker stylistic shape when it already satisfies a stronger directional reading.

---

### 8.10 Edge Cases

If the input is `[0,0,0,0]`, return `Flat`.

If density segments are all zero, `DensityShape = Flat`.

If energy segments are all zero, `EnergyShape = Flat`.

If a profile is nearly flat with only tiny numerical changes, epsilon filtering should force `Flat`.

---

### 8.11 Constraints

- Must run in O(1)
- Must be deterministic
- Must not inspect raw PatternTurn data
- Must not allocate unnecessary intermediate collections
- Must accept exactly 4 segment values
- Must use the same logic for density shape and energy shape

---

### 8.12 Integration Notes

- Implement as a `SegmentActivityProfileAnalyzer` or equivalent derived-feature stage
- Run after Density and Energy analyzers
- Add `SegmentActivityProfile` to `TurnAnalysisResult`
- ResponsePlanner should consume `DensityShape` and `EnergyShape` separately
- Do not add a combined density+energy trajectory feature in v1

---

## 9. Next Features (Optional / Deferred)

- RepetitionFeatures
