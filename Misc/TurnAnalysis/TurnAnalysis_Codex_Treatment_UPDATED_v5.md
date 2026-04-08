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

Use the shared segment partition helper created in Section 0.

Do NOT reimplement segment logic in this analyser.

The helper must:
- return 4 contiguous segments
- distribute remainder from the front
- handle N = 0 safely

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


### 5.8 Implementation Reqs

Implementation requirements:

- Implement as EnergyAnalyser : IAnalyser<EnergyFeatures>
- Use the shared SegmentHelper from the foundations section
- Do NOT reimplement segment partition logic
- Accept EnergyThresholds via constructor injection
- Do NOT hardcode thresholds inside the analyser
- Do NOT mutate PatternTurn
- Do NOT depend on external state
- EnergyFeatures should contain results only, not thresholds/config

Deliverables:
- EnergyThresholds class
- EnergyAnalyser implementation
- Any required refinement to EnergyFeatures
- Unit tests covering raw metrics, derived traits, and edge cases

Important edge-case rule:
For turns with no active steps:
- IsLowEnergy = true
- all other traits = false

---

## 6. Anchor Detection (Structural Salience and Support Feature)

### 6.1 Definition

Anchor Detection identifies structurally salient hits within a turn.

An anchor is defined as an active step whose computed salience score exceeds a fixed anchor threshold. In this architecture, salience represents the degree to which a hit stands out as a point of structural importance within the turn.

In the upgraded formulation, Anchor Detection also captures whether salient activity is **structurally supported**. This allows the system to distinguish between hits that are merely prominent in isolation and hits that contribute to a more grounded and musically coherent rhythmic structure.

This allows the system to move beyond describing how much activity occurs or how intense it is, and instead begin identifying which specific events matter most, and how strongly the turn as a whole is supported by them.

---

### 6.2 Feature Structure

Anchor Detection is treated as an event-level salience and support feature family consisting of:

- per-step salience scores
- per-step binary anchor flags
- aggregate anchor summaries
- boundary-anchor indicators
- segment-level anchor counts
- support-quality summaries

This layered structure is important. It preserves low-level inspectability for debugging and analysis, while also exposing compact summaries that can be used directly by the ResponsePlanner.

The support layer does not replace anchor detection. Rather, it extends it by summarising how much of the turn’s active material is grounded by stronger positions and how much remains weak and unsupported.

---

### 6.3 Why Anchor Detection Matters

Density describes occupancy.

Energy describes intensity.

Neither, however, identifies which events are structurally important.

Anchor Detection fills this gap by identifying hits that function as points of emphasis, arrival, or reference within the phrase. These are the events that the AI may later choose to preserve, mirror, reinforce, answer, or contrast.

The support extension adds a second question:

- not only which hits matter most,
- but also whether the turn’s activity is rhythmically grounded or unstable.

This is musically useful because short drum phrases often derive their identity not only from loud or isolated hits, but from the balance between grounded points of emphasis and weaker activity around them.

---

### 6.4 Salience Components

Anchor salience remains derived from four fixed components:

- absolute velocity
- local accent
- local isolation
- positional bonus

These components are retained because they are lightweight, interpretable, and suitable for the project’s constrained input setting.

Absolute velocity captures raw emphasis.

Local accent captures whether a hit stands out relative to nearby active hits.

Local isolation captures whether a hit is exposed by surrounding space.

Positional bonus captures the light additional structural importance often associated with turn openings and turn endings.

Together, these components provide a practical approximation of event salience without requiring complex metrical assumptions or long-range motif analysis.

---

### 6.5 Support Layer

In addition to salience scoring, the upgraded feature family derives a lightweight notion of structural support.

Support is intended to approximate whether active hits occur in relation to stronger rhythmic reference points rather than as isolated weak events.

Given the project’s constraints, support is defined conservatively using a fixed step-weight hierarchy and local neighbourhood checks rather than a full style-specific metrical model.

This support layer is deliberately lightweight. It does not attempt to model full meter induction or probabilistic beat inference. Instead, it captures a simpler distinction between:

- activity that is structurally grounded
- activity that is weak but supported by nearby stronger material
- activity that is weak and unsupported

This makes it suitable for a one-pad, short-turn interactive drumming system.

---

### 6.6 Support Components

The support layer is derived from two sources:

- positional metrical weight
- local support context

Positional metrical weight assigns each step a fixed structural weight according to its place in the turn grid. Stronger positions receive higher weights; weaker subdivisions receive lower weights.

Local support context checks whether weaker active steps occur near stronger active steps within a small neighbourhood window.

This produces a graded distinction between:

- strong-position hits
- weak-position hits that are supported
- weak-position hits that are unsupported

The purpose is not to impose a rigid theory of meter, but to provide a lightweight approximation of rhythmic grounding that is defensible within the project’s limited input setting.

---

### 6.7 Salience Model

For each active step, a continuous salience score is computed as a weighted combination of the four salience components.

For inactive steps, salience is always zero.

This score-first design is retained deliberately. Rather than classifying anchors directly, the system first estimates how anchor-like each active step is, then derives binary anchor flags from that score.

This has several advantages:

- it is easier to justify in the dissertation
- it is easier to inspect and debug
- it is easier to tune later
- it preserves more information for future planner refinement

The support layer is computed in parallel as a separate descriptive summary and does not alter the anchor thresholding rule itself.

---

### 6.8 Binary Anchor Decision

A step is classified as an anchor only if:

- it is active
- its salience score meets or exceeds the anchor threshold

This means not every strong hit becomes an anchor automatically. A hit generally needs support from more than one cue, such as strength plus isolation, or strength plus contextual prominence.

This produces the intended sparse-to-moderate anchor behaviour: anchors should be selective and meaningful, rather than common enough to dilute the signal.

The support layer should therefore be understood as complementary to anchor classification, not a replacement for it.

---

### 6.9 Output Representation

The feature family should expose:

- a salience score for every step
- a binary anchor flag for every step
- the set of anchor indices
- the total anchor count
- the strongest anchor and its score
- whether the turn begins with an anchor
- whether the turn ends with an anchor
- anchor counts per segment
- average metrical support of active hits
- ratio of strong-position active hits
- ratio of supported weak hits
- ratio of unsupported weak hits

This output structure is important because it supports both immediate planner use and later inspection. The planner may reason over compact summaries such as strongest anchor or support quality, while debugging tools can still inspect the full step-level salience profile.

---

### 6.10 Segment Alignment

Anchor summaries should continue to use the same four-segment partitioning scheme already established for density and energy.

This ensures temporal alignment across feature families. For example, the system can later reason about whether a turn becomes denser, louder, and more anchor-heavy toward its ending, all within the same shared temporal frame.

The support layer may initially remain global rather than segment-wise if implementation simplicity is preferred. However, the shared segment frame leaves room for future expansion to segment-level support summaries if required.

---

### 6.11 Musical Interpretation

Anchor Detection provides the system with a first layer of phrase-structural awareness.

It allows the analysis stage to ask not only:

- how much happened
- how intensely it happened

but also:

- which hits mattered most
- whether the turn’s activity was rhythmically grounded

This is musically useful because salient events often define the remembered shape of a short rhythmic phrase, while support quality helps distinguish between gestures that feel coherent and gestures that feel scattered or unstable.

Anchor Detection therefore helps the AI respond not only to the identity of the phrase, but also to the degree of structural grounding within that identity.

---

### 6.12 Design Rationale

The upgraded design remains intentionally conservative.

It still avoids:

- heavy dependence on style-specific beat models
- long-range repetition logic
- multi-instrument reasoning
- full probabilistic metrical inference

Instead, it adds only a lightweight support layer built from:

- fixed positional weighting
- local neighbourhood support

This is a pragmatic compromise.

It improves the analyser’s ability to describe rhythmic grounding while remaining easy to implement, easy to explain, and appropriate for a system built around a single drum pad and short turn windows.

---

### 6.13 Limitations

Anchor Detection still does not model:

- motif recurrence across a turn
- learned stylistic expectations about strong beats
- multi-instrument orchestration
- higher-level phrase syntax beyond local salience and turn boundaries
- culturally specific syncopation conventions

The support layer should therefore be understood as a lightweight approximation of rhythmic grounding rather than a complete theory of metrical stability.

More specialised ending behaviour remains the responsibility of End Activity, while broader recurring-structure analysis remains the responsibility of later features such as Repetition.

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
