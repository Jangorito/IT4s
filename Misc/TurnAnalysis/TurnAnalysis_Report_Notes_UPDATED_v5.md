# Turn Analysis Architecture — Report Notes

## 1. Purpose of Turn Analysis

The Turn Analysis stage is responsible for transforming a captured and compiled rhythmic input (PatternTurn) into a structured set of descriptive features. These features provide an interpretable representation of the user's performance, enabling the system to generate musically meaningful responses.

Rather than operating directly on raw hit data, Turn Analysis works on a quantised step-grid representation. This ensures that extracted features are stable, comparable across turns, and computationally efficient.

The output of Turn Analysis is used as the primary input to the ResponsePlanner, which determines how the AI agent should respond musically.

---

## 2. Role within the System

Turn Analysis sits between:

- Pattern Compilation (raw input → structured PatternTurn)
- Response Planning (feature-driven decision making)

Its role is to bridge low-level input data and higher-level musical behaviour by producing features that are:

- computationally lightweight
- musically interpretable
- suitable for real-time use
- extensible for future improvements

---

## 3. Feature Set Overview

The Turn Analysis system is composed of multiple feature families, each capturing a different aspect of the rhythmic input.

Confirmed features:

- Density (occupancy)
- Energy (velocity-driven intensity)
- Anchor Detection (structural salience)
- End Activity (phrase-ending behaviour)
- Segment Activity Profile (derived phrase-shape interpretation)

Still optional / deferred:

- Repetition

---

## 4. Density (Occupancy Feature)

### 4.1 Definition

Density is defined as the proportion of quantised step positions within a turn that contain at least one onset.

A step is considered active if its velocity value is greater than zero.

Formally:

Density = (number of active steps) / (total number of steps)

---

### 4.2 Feature Structure

Density is treated as a feature family, consisting of:

- Global density (StepDensity)
- Local segment densities (SegmentDensities)

Supporting values:

- StepCount
- ActiveStepCount
- InactiveStepCount

---

### 4.3 Global Density

Global density represents overall rhythmic occupancy across the entire turn.

It provides a coarse measure of how sparse or busy the performance is.

---

### 4.4 Segment Density

To capture the distribution of activity across time, the turn is divided into four equal contiguous segments.

Density is computed independently for each segment using the same occupancy logic as global density.

This produces:

SegmentDensities = [D0, D1, D2, D3]

Where each value represents the proportion of active steps within that segment.

---

### 4.5 Motivation

Global density alone is insufficient to describe rhythmic structure, as different patterns may share identical occupancy but differ significantly in temporal distribution.

By incorporating segment densities, the system can distinguish between:

- evenly distributed patterns
- front-loaded activity
- back-loaded activity
- clustered rhythmic behaviour

This improves the system’s ability to generate contextually appropriate responses.

---

### 4.6 Musical Interpretation

Density captures:

- overall rhythmic busyness
- how activity is distributed across a turn

It provides a foundational description of the input but does not capture:

- accent structure
- structural importance of notes
- phrase endings
- repetition

These aspects are handled by other feature families.

---

### 4.7 Design Rationale

Density is intentionally defined at the level of the quantised step grid rather than raw event timing.

This ensures:

- consistency across turns
- robustness to performance timing variation
- compatibility with symbolic pattern manipulation

Additionally, multiple hits that fall within the same quantised step are treated as a single active step, reinforcing the interpretation of density as occupancy rather than event frequency.

---

### 4.8 Limitations

Density does not capture:

- where important notes occur (anchors)
- how energy changes over time
- whether a phrase leads into an ending
- repetition or motif structure

It is therefore used in conjunction with additional features rather than as a standalone descriptor.

---

## 5. Energy (Velocity-Driven Intensity Feature)

### 5.1 Definition

Energy is defined as the intensity profile of a turn, derived from hit velocity values rather than from occupancy alone.

Where density describes how much rhythmic space is occupied, energy describes how forcefully that space is articulated.

In this architecture, energy is intentionally treated as a velocity-driven feature family. Timing-based notions of intensity, such as rapid bursts or clustering, are left to density and other structural features so that feature responsibilities remain clean and non-overlapping.

---

### 5.2 Feature Structure

Energy is treated as a feature family consisting of:

- global energy metrics
- local segment energy metrics
- derived energy traits for planner use

This produces a layered representation in which low-level measurements remain available for inspection, while higher-level interpretations can be used directly by the ResponsePlanner.

---

### 5.3 Global Energy Metrics

The global energy description captures the overall intensity of the turn using three complementary values:

- MeanVelocity
- PeakVelocity
- VelocityVariance

MeanVelocity provides the baseline intensity of the turn and indicates whether the performance is generally soft or forceful.

PeakVelocity captures the strongest hit in the turn and therefore provides a simple measure of accent extremity.

VelocityVariance captures the degree of dynamic variation across hits. This is important because two turns may have similar mean intensity while differing greatly in expressive contour: one may be flat and mechanically even, while the other may contain clear accents and dynamic contrast.

---

### 5.4 Segment Energy

To capture how intensity changes across time, the turn is divided into the same four contiguous segments used by density analysis.

For each segment, a mean velocity value is computed. This yields:

SegmentMeanVelocities = [E0, E1, E2, E3]

Using the same segmentation strategy as density is a deliberate architectural choice. It ensures that occupancy and intensity can be compared within the same temporal frame, making it easier to reason about whether a turn becomes denser, louder, softer, or more sparse over time.

---

### 5.5 Derived Traits

Energy metrics are useful descriptively, but the ResponsePlanner benefits most from interpretable categorical traits. For this reason, the system derives a small number of higher-level energy descriptors.

Proposed traits:

- HighEnergy
- LowEnergy
- FlatEnergy
- Accented
- Crescendo
- Decrescendo

These traits are not independent feature families. Rather, they are interpretations derived from the global and local energy metrics.

HighEnergy and LowEnergy describe the overall intensity level of the turn.

FlatEnergy indicates low dynamic variance and therefore a relatively even, unshaped intensity profile.

Accented indicates that the strongest hit stands out significantly from the average level, suggesting deliberate emphasis.

Crescendo and Decrescendo describe directional intensity change across the turn, based on the segment mean velocity profile.

---

### 5.6 Musical Interpretation

Energy captures aspects of performance that density alone cannot represent.

It provides information about:

- how strongly the player is striking
- whether accents are present
- whether the turn feels dynamically flat or expressive
- whether intensity builds or recedes over time

This matters musically because a sparse but high-energy turn can feel assertive and punctuated, whereas a dense but low-energy turn may feel soft, textural, or ghosted. Likewise, a crescendo may imply forward motion or phrase shaping, while a decrescendo may imply release or closure.

Energy therefore plays an important role in allowing the system to distinguish between turns that are rhythmically similar but expressively different.

---

### 5.7 Design Rationale

The decision to define energy as velocity-driven rather than as a hybrid of timing and velocity is important.

This choice is motivated by four main considerations:

- the system already has direct access to velocity through the piezo-triggered hit input
- velocity is a reliable and interpretable source of expressive information
- timing-derived notions of perceived energy are already partially represented by density and future structural features
- separating these responsibilities reduces feature overlap and makes the architecture easier to justify and implement

This is especially appropriate given the project constraints. With only one drum pad and relatively short turns, there is limited value in overcomplicating energy with multiple interacting proxies. A clean, velocity-centred definition is more robust and easier to explain in the dissertation.

---

### 5.8 Relationship to Density

Energy and density are intentionally complementary.

Density describes how many steps are occupied.

Energy describes how intensely those occupied steps are played.

Their combination provides a more meaningful description than either feature alone. For example:

- high density + high energy may suggest an aggressive or driving input
- high density + low energy may suggest lighter, ghosted, or texture-oriented playing
- low density + high energy may suggest sparse but emphatic accents
- low density + low energy may suggest minimal or withdrawn playing

This interaction is likely to be highly valuable for response planning, because it enables the system to reason not only about busyness, but about expressive weight.

---

### 5.9 Architectural Role

Energy should follow the same architectural philosophy as density:

- raw measurements are extracted deterministically
- higher-level traits are derived from those measurements
- the ResponsePlanner primarily reasons over the interpreted traits, while still retaining access to underlying values if needed for debugging or future refinement

This supports both implementation clarity and dissertation clarity, since it cleanly separates signal description from decision logic.

---

### 5.10 Limitations

Energy does not directly capture:

- structural importance of note placement
- phrase-ending behaviour
- repeated motifs
- temporal clustering as a separate rhythmic phenomenon

Those aspects are handled elsewhere in the feature set.

Energy is therefore best understood as the system’s primary descriptor of dynamic intensity, not as a complete model of musical salience.

---

## 6. Anchor Detection (Structural Salience Feature)

### 6.1 Definition

Anchor Detection identifies structurally salient hits within a turn.

An anchor is defined as an active step whose computed salience score meets or exceeds a fixed anchor threshold. In this updated model, salience represents the degree to which a hit stands out as a point of structural importance within the turn based on a weighted combination of dynamic, contextual, metrical, and phrase-role cues.

This allows the system to move beyond describing how much activity occurs or how intense it is, and instead begin identifying which specific events matter most.

---

### 6.2 Feature Structure

Anchor Detection is treated as an event-level salience feature family consisting of:

- per-step salience scores
- per-step binary anchor flags
- aggregate anchor summaries
- strongest-anchor summary
- boundary-anchor indicators
- segment-level anchor counts

This layered structure is important. It preserves low-level inspectability for debugging and analysis, while also exposing compact summaries that can be used directly by the ResponsePlanner.

---

### 6.3 Why Anchor Detection Matters

Density describes occupancy.

Energy describes intensity.

Neither, however, identifies which events are structurally important.

Anchor Detection fills this gap by identifying hits that function as points of emphasis, arrival, or reference within the phrase. These are the events that the AI may later choose to preserve, mirror, reinforce, answer, or contrast.

In other words, Anchor Detection helps the system distinguish between a turn that is merely busy and a turn that contains clear musical pivots.

---

### 6.4 Inputs and Assumptions

The analyser operates directly on the compiled `PatternTurn` representation and expects access to:

- `velocity`
- `StepCount`
- `bars`
- `beatsPerBar`

The implementation assumes a 12-steps-per-quarter grid. Inactive steps are not scored as candidate anchors, but they still matter indirectly because surrounding silence affects local isolation.

This keeps the feature grounded in the same turn-level representation already used elsewhere in the analysis architecture, while still allowing modest structural reasoning from the available timing metadata.

---

### 6.5 Salience Components

Anchor salience is derived from five fixed components:

- `VelocityScore`
- `LocalAccentScore`
- `IsolationScore`
- `MetricalWeightScore`
- `PhraseRoleScore`

This is an important refinement over the earlier draft. The previous single positional bonus is replaced by two narrower structural terms: one for lightweight metrical placement and one for phrase-role relevance.

Together, these components provide a practical approximation of event salience without requiring motif tracking, style-specific beat templates, or long-range phrase parsing.

---

### 6.6 Core Formula

For each active step `i`, a continuous salience score is computed as:

    AnchorSalience(i) =
        0.30 * VelocityScore(i)
      + 0.20 * LocalAccentScore(i)
      + 0.15 * IsolationScore(i)
      + 0.25 * MetricalWeightScore(i)
      + 0.10 * PhraseRoleScore(i)

The fixed decision threshold is:

- `AnchorThreshold = 0.55`

For inactive steps, salience remains zero.

This score-first design is a deliberate architectural choice. Rather than classifying anchors directly, the system first estimates how anchor-like each active step is, then derives binary anchor flags from that score.

This has several advantages:

- it is easier to justify in the dissertation
- it is easier to inspect and debug
- it is easier to tune later
- it preserves more information for future planner refinement

---

### 6.7 Velocity Score

`VelocityScore` normalises raw hit strength:

    VelocityScore(i) = Clamp01(velocity[i] / 127)

This is the most direct cue of emphasis and carries the largest single weight in the model. It ensures that genuinely forceful hits remain central to anchor detection while still allowing other cues to shape the final result.

---

### 6.8 Local Accent Score

`LocalAccentScore` captures whether a hit stands out relative to nearby active neighbours. A local window of radius 2 is used around each step.

Within that window, the mean velocity of active neighbouring hits is computed and only positive contrast is retained:

    score = (v_i - localMean) / 127
    LocalAccentScore(i) = Clamp01(score)

This means the feature rewards contextual prominence without penalising quieter connective hits. It therefore captures accent-like behaviour rather than simple loudness.

---

### 6.9 Isolation Score

`IsolationScore` measures how exposed a hit is within its local neighbourhood. Using the same radius-2 window, the analyser counts inactive neighbouring slots and divides by the number of available neighbour positions:

    IsolationScore(i) = inactiveCount / totalNeighbourSlots

This rewards hits that are surrounded by silence or low occupancy, reflecting the musical intuition that exposed events often feel structurally important even when they are not the loudest hits in the turn.

---

### 6.10 Metrical Weight Score

`MetricalWeightScore` adds a lightweight beat-hierarchy cue derived from the turn metadata:

    stepsPerBar = StepCount / bars
    stepsPerBeat = stepsPerBar / beatsPerBar

The score is then assigned using simple rules:

- if `stepIndex % stepsPerBar == 0`, return `1.00`
- else if `stepIndex % stepsPerBeat == 0`, return `0.75`
- else if `stepsPerBeat` is even and `stepIndex % (stepsPerBeat / 2) == 0`, return `0.45`
- else return `0.20`

This is intentionally modest rather than theory-heavy. It gives the analyser a defensible sense of downbeats, beats, and half-beats without committing the system to a style-specific metrical model.

---

### 6.11 Phrase Role Score

`PhraseRoleScore` adds a lightweight phrase-position cue. The analyser first precomputes the first and last active indices in the turn, then evaluates whether each step occupies an opening, midpoint, or closing role.

For common turn lengths, the windows are defined as follows:

If `StepCount = 48`:

- opening: `0-11`
- midpoint: `24-35`
- closing: `36-47`

If `StepCount = 96`:

- opening: `0-11`
- midpoint: `48-59`
- closing: `84-95`

The scoring logic is:

- if the step is the first or last active hit, return `1.00`
- else if the step falls in an opening or closing window, return `0.75`
- else if the step falls in a midpoint window, return `0.45`
- else return `0.00`

This preserves a small amount of phrase awareness without turning anchor detection into a full phrase parser. It acknowledges that beginnings, endings, and central pivots often carry more structural weight than otherwise similar interior hits.

---

### 6.12 Binary Anchor Decision

A step is classified as an anchor only if:

- it is active
- its salience score meets or exceeds `0.55`

This means not every strong hit becomes an anchor automatically. A hit usually needs support from more than one cue, such as strength plus metrical weight, or contextual prominence plus isolation.

This produces the intended sparse-to-moderate anchor behaviour: anchors should be selective and meaningful, rather than common enough to dilute the signal.

---

### 6.13 Processing Loop

The extraction loop follows a simple deterministic pattern:

- skip any step whose velocity is less than or equal to zero
- compute the weighted salience score for each active step
- store the score in the per-step salience array
- if the score meets the threshold, mark the step as an anchor, record its index, and update the strongest-anchor summary

First and last active indices can be precomputed once before scoring, and segment counts can be accumulated through the shared segment-partition logic already used elsewhere.

This single-pass design keeps the analyser efficient and easy to reason about, which is especially important for real-time use and dissertation transparency.

---

### 6.14 Output Representation

The feature family should expose:

- `StepSalienceScores`
- `StepIsAnchor`
- `AnchorCount`
- `AnchorIndices`
- `StrongestAnchorIndex`
- `StrongestAnchorScore`
- `HasOpeningAnchor`
- `HasClosingAnchor`
- `AnchorCountsPerSegment`

This output structure is important because it supports both immediate planner use and later inspection. The planner may reason over compact summaries such as strongest anchor or opening/closing anchor status, while debugging tools can still inspect the full step-level salience profile.

---

### 6.15 Segment Alignment

Anchor summaries should use the same four-segment partitioning scheme already established for density and energy, with `AnchorCountsPerSegment` ideally reusing the shared `SegmentHelper`.

This ensures temporal alignment across feature families. For example, the system can later reason about whether a turn becomes denser, louder, and more anchor-heavy toward its ending, all within the same shared temporal frame.

Maintaining this consistency also improves dissertation clarity, since the reader can understand all local features in terms of a single segment model rather than multiple incompatible partitioning schemes.

---

### 6.16 Musical Interpretation

Anchor Detection provides the system with a first layer of phrase-structural awareness.

It allows the analysis stage to ask not only:

- how much happened
- how intensely it happened

but also:

- which hits mattered most

This is musically useful because salient events often define the remembered shape of a short rhythmic phrase. A player may include many hits, but only a few may function as the moments that characterise the gesture. By combining dynamic, contextual, metrical, and phrase-role cues, the analyser can treat certain hits as points of arrival, framing, or pivot rather than as ordinary activity.

Anchor Detection therefore helps the AI respond to the identity of the phrase rather than only to its surface statistics.

---

### 6.17 Design Rationale

The updated design remains intentionally conservative.

It avoids:

- long-range repetition logic
- heavy dependence on style-specific metrical templates
- complex probabilistic salience modelling
- multi-instrument orchestration assumptions

Instead, it uses only cues that are strongly defensible given the available input:

- hit strength
- local contrast
- local spacing
- basic metrical location
- light phrase-boundary relevance

Splitting the earlier positional bonus into `MetricalWeightScore` and `PhraseRoleScore` improves interpretability without sacrificing simplicity. This makes the feature practical to implement, easy to explain, and appropriate for a system built around a single drum pad and short turn windows.

---

### 6.18 Constraints

The implementation should remain:

- `O(n)` in runtime
- deterministic
- free of external dependencies
- free of per-step allocations inside the main loop other than the required output arrays

These constraints help keep the analyser suitable for real-time turn analysis while preserving implementation clarity.

---

### 6.19 Limitations

Anchor Detection does not yet model:

- motif recurrence across a turn
- adaptive or relative thresholding
- stylistic expectations about syncopation beyond a simple beat hierarchy
- multi-instrument orchestration
- higher-level phrase syntax beyond local salience, metrical placement, and fixed phrase-role windows

It should therefore be understood as a lightweight structural salience model rather than a complete theory of musical importance.

More specialised ending behaviour remains the responsibility of End Activity, while broader recurring-structure analysis remains the responsibility of later features such as Repetition.

---

### 6.20 Future Extensions

Possible later refinements include:

- relative thresholding or top-k anchor selection
- repetition score integration
- adaptive component weighting
- broader phrase-role templates for non-48-step and non-96-step turns
- tighter interaction with planner-level motif handling

---

## 7. End Activity (Phrase Ending Feature)

### 7.1 Definition

End Activity describes the rhythmic and dynamic behaviour of the final portion of a turn, capturing how a phrase is concluded.

This feature family is intended to give the system a lightweight representation of phrase-ending behaviour. Rather than only asking how dense, energetic, or salient a turn is overall, End Activity asks how the player closes the gesture.

---

### 7.2 Why End Activity Matters

Phrase endings are musically important because they often determine whether a gesture feels:

- resolved
- sustained
- punctuated
- left open for continuation

A short turn may contain similar overall density and energy to another turn while ending in a completely different way. One player may taper off into silence, while another may deliver a final accent or maintain intensity to the boundary. End Activity allows the system to distinguish between these cases.

This is especially important for response generation, because ending behaviour strongly affects whether the AI should:

- mirror a closing gesture
- answer a punctuated ending
- continue a sustained phrase
- contrast a taper with a more active reply

---

### 7.3 Window Definition

The end region is defined as the final 25% of the turn.

Formally:

endStart = floor(stepCount * 0.75)

The end window therefore includes all steps from `endStart` to `stepCount - 1`.

This fixed proportional definition was chosen because it scales naturally with turn length, remains simple to implement, and maps well onto the idea of a phrase-final region without introducing unnecessary complexity.

---

### 7.4 Feature Structure

End Activity consists of three components:

- EndDensity
- EndEnergy
- EndAccent

These are designed to capture complementary aspects of phrase-final behaviour.

EndDensity represents how rhythmically busy the ending is.

EndEnergy represents how forcefully the ending is articulated.

EndAccent represents the strongest individual hit in the ending, allowing the system to detect whether the closing region contains a final punch or emphatic accent.

---

### 7.5 Component Definitions

EndDensity is defined as:

(number of active steps in end window) / (number of steps in end window)

A step is active if its velocity is greater than zero.

EndEnergy is defined as the mean velocity of active steps in the end window.

If there are no active steps in the end window, EndEnergy is zero.

EndAccent is defined as the maximum velocity value in the end window.

If there are no active steps in the end window, EndAccent is zero.

---

### 7.6 Musical Interpretation

These three values together allow the system to distinguish between several important ending types.

A low EndDensity and low EndEnergy suggests a tapered or withdrawn ending.

A high EndDensity with moderate EndEnergy suggests a sustained ending that carries momentum through to the boundary.

A strong EndAccent indicates a punctuated ending, even if the ending is otherwise sparse.

A sparse but high-accent ending may represent a deliberate final hit rather than a continuous phrase ending.

This makes End Activity especially useful for shaping response behaviour in a musically meaningful way.

---

### 7.7 Relationship to Other Features

End Activity is intentionally separated from the global Density and Energy families.

Density describes occupancy across the whole turn.

Energy describes intensity across the whole turn.

End Activity focuses specifically on the final region of the turn.

This separation keeps feature responsibilities clean. End Activity is not intended to replace segment-level density or energy analysis, but to provide a specialised phrase-ending descriptor that the planner can reason about directly.

It also complements Anchor Detection. Anchors identify structurally salient events anywhere in the turn, whereas End Activity describes the behaviour of the ending region as a whole.

---

### 7.8 Design Rationale

The design is intentionally lightweight.

It does not attempt to model full phrase syntax, metrical closure theory, or long-range cadential behaviour. Instead, it captures a practical and interpretable approximation of phrase-ending shape that is suitable for:

- real-time use
- short turn lengths
- a single-pad input setting
- dissertation-friendly justification

Including EndAccent as a dedicated component is important. Without it, a sparse but emphatically punctuated ending might be misread as simply low-activity. The accent component preserves this musically meaningful distinction.

---

### 7.9 Architectural Role

End Activity should be treated as a specialised segment-level feature family that supports phrase-aware response planning.

Its role is not merely descriptive. It helps the system reason about how a phrase ends, which is likely to be valuable when deciding whether to:

- resolve
- continue
- echo
- contrast
- answer with a fill-like gesture

This makes End Activity an important bridge between low-level rhythmic measurement and higher-level conversational musical behaviour.

---

### 7.10 Limitations

End Activity does not yet capture:

- finer-grained substructure inside the ending window
- explicit comparison between beginning and ending activity
- metrical interpretations of cadential placement
- longer-range phrase relations across multiple turns

Those aspects can be handled later through Segment Activity Profile or more advanced temporal features.

---

## 8. Segment Activity Profile (Derived Phrase-Shape Feature)

### 8.1 Definition

Segment Activity Profile (SAP) is a derived interpretation layer that classifies the temporal shape of activity across the turn.

Unlike density, energy, anchor detection, or end activity, SAP does not introduce a new raw extractor over the PatternTurn. Instead, it operates on the four-segment values that already exist inside the Density and Energy feature families.

Its role is therefore interpretive rather than extractive.

---

### 8.2 Why SAP Matters

The existing feature families already describe what happens in a turn:

- Density describes how much activity occurs
- Energy describes how forcefully it is played
- Anchor Detection identifies salient events
- End Activity describes phrase closure

However, there is still value in asking how activity unfolds across the whole turn as a shape.

Two turns can have similar overall density and energy while exhibiting very different trajectories. One may gradually build. Another may begin strongly and tail off. Another may concentrate activity in the middle.

SAP allows the system to describe these broader temporal contours in a compact, planner-friendly form.

---

### 8.3 Derived Rather Than Raw

A key architectural decision is that SAP is built entirely on top of already-computed segment values:

- SegmentDensities from Density
- SegmentMeanVelocities from Energy

This is important for several reasons:

- it avoids unnecessary duplication of work
- it keeps feature responsibilities clean
- it makes the architecture easier to explain
- it preserves a clear distinction between measurement and interpretation

SAP should therefore be understood as a second-layer interpretive feature family, not as a parallel extractor.

---

### 8.4 Shared Shape Vocabulary

SAP uses a shared categorical vocabulary for both density shape and energy shape.

The confirmed v1 categories are:

- Flat
- Increasing
- Decreasing
- FrontLoaded
- BackLoaded
- MidPeak
- MidDip

Using the same shape set for both density and energy is a deliberate design choice. It makes the system more coherent and allows the planner to reason about phrase shape using a common language, even when density and energy behave differently.

For example, a turn might be density-front-loaded but energy-back-loaded, which could be musically meaningful for response generation.

---

### 8.5 What Each Category Means

Flat indicates that activity remains essentially even across the turn.

Increasing indicates a clear rise from beginning to end.

Decreasing indicates a clear fall from beginning to end.

FrontLoaded indicates that earlier segments dominate later ones, without the profile necessarily being strictly monotonic.

BackLoaded indicates that later segments dominate earlier ones, again without requiring a perfectly smooth upward trend.

MidPeak indicates that the centre of the turn is more active or energetic than the edges.

MidDip indicates that the middle is weaker than the edges.

These categories provide a practical descriptive language for phrase shape without overcomplicating the model.

---

### 8.6 Rule-Based Classification

SAP is intentionally deterministic and rule-based.

It does not use statistical modelling, learned clustering, or fuzzy probabilistic interpretation. Instead, it classifies the four-segment profiles through an ordered set of heuristics.

This is important for dissertation clarity and implementation clarity. A rule-based model is:

- easier to justify
- easier to debug
- easier to tune
- more transparent in a real-time interactive system

The confirmed classification ordering is:

1. Flat
2. Increasing / Decreasing
3. FrontLoaded / BackLoaded
4. MidPeak / MidDip

This ordering matters because some profiles could plausibly fit more than one description. The system therefore needs a stable priority scheme so that classification remains deterministic and interpretable.

---

### 8.7 Epsilon Tolerance

A further confirmed design decision is the use of an epsilon tolerance.

Small fluctuations between adjacent segment values should not automatically produce a shape label. Instead, tiny differences are treated as noise and ignored.

This prevents the classifier from over-interpreting insignificant variation, which is especially important in a short-turn system where a small number of hits can slightly perturb the segment values.

In practical terms, epsilon makes SAP more musically plausible and less brittle.

---

### 8.8 Relationship to Density and Energy

SAP does not replace segment-level density or energy values.

Those raw values remain important because they preserve the actual numerical description of the turn.

SAP adds a compact interpretive summary on top of them.

This layered relationship is valuable architecturally:

- Density and Energy provide the measurable substrate
- SAP provides a higher-level phrase-shape reading

This mirrors the broader philosophy of the analysis architecture, where lower-level measurements remain available while more planner-friendly abstractions are derived from them.

---

### 8.9 Why There Is No Combined Shape Feature in v1

An important locked v1 decision is that SAP should not produce a single combined phrase-shape label that merges density and energy into one output.

In other words, the system should produce:

- DensityShape
- EnergyShape

but not an additional fused trajectory category.

This is a good design compromise for v1 because it keeps the analysis layer simple and avoids prematurely collapsing two potentially different expressive dimensions into one label.

Instead, the ResponsePlanner can combine density and energy shapes later when making response decisions. This keeps the planner, rather than the analyzer, responsible for higher-level musical interpretation.

---

### 8.10 Musical Interpretation

SAP gives the system a clearer sense of gesture profile.

For example:

- an Increasing shape may suggest build, momentum, or arrival
- a Decreasing shape may suggest release or taper
- a FrontLoaded shape may suggest an opening gesture that settles
- a BackLoaded shape may suggest delayed emphasis
- a MidPeak shape may suggest a phrase that blooms in the centre
- a MidDip shape may suggest edge emphasis or a hollowed centre

These are exactly the kinds of phrase-level distinctions that can influence whether an AI reply should mirror, continue, answer, or contrast the user’s input.

---

### 8.11 Architectural Role

SAP is best understood as a bridge between local segment statistics and planner-level phrase reasoning.

It strengthens the analysis architecture without adding heavy complexity, because it reuses information that is already available.

This makes it a strong fit for the dissertation goals: it adds musically meaningful interpretive power while remaining computationally lightweight, modular, and easy to justify.

---

### 8.12 Limitations

SAP remains a deliberately simple phrase-shape classifier.

It does not model:

- long-range phrase structure across multiple turns
- motif recurrence
- beat-hierarchy awareness
- stylistic phrase norms
- nuanced mixed-shape ambiguity beyond the chosen rule ordering

It should therefore be understood as a lightweight interpretive layer rather than a full theory of rhythmic phrasing.

---
## 9. Repetition (Deferred Feature)

### 9.1 Motivation

Repetition is a fundamental aspect of musical structure. In rhythmic performance, repeated patterns or motifs contribute to:

- coherence and recognisability  
- groove formation  
- stylistic identity  
- expectation and anticipation  

In the context of interactive improvisation, recognising repetition would allow the system to respond more intelligently by:

- reinforcing motifs  
- developing existing ideas  
- contrasting repeated material  
- identifying when a player is “locking into” a groove  

This makes repetition an attractive candidate feature for improving musical responsiveness and perceived intelligence.

---

### 9.2 Conceptual Definition

Repetition refers to the presence of recurring rhythmic structures within a turn.

At a high level, this could include:

- exact repetition of step patterns  
- approximate repetition (with small variations)  
- repeated accents or anchor positions  
- recurring density or energy shapes  

A repetition feature would therefore aim to quantify how much of the turn reuses previously established material.

---

### 9.3 Possible Approaches

Several approaches were considered for detecting repetition:

**Exact pattern matching**
- Compare subsequences of the step grid for equality  
- Simple and deterministic  
- Limited to rigid repetition only  

**Approximate matching**
- Allow tolerance for velocity differences or missing hits  
- More musically realistic  
- Requires similarity thresholds and comparison logic  

**Feature-based repetition**
- Detect repetition in higher-level features (e.g. anchors, density segments)  
- More abstract and robust  
- Less precise at the step level  

**Sliding window comparison**
- Compare overlapping windows across the turn  
- Can detect local motifs  
- Computationally more expensive  

---

### 9.4 Design Challenges

Despite its musical relevance, repetition introduces several challenges in the context of this project:

**1. Short turn length**

The system operates on very short inputs (e.g. 1–2 bars).  
This significantly limits the amount of material available for meaningful repetition detection.

In many cases:
- there may be insufficient data for reliable pattern comparison  
- repetition may be ambiguous or coincidental  

---

**2. Single-pad input constraint**

With only one drum pad:

- rhythmic variation is already highly constrained  
- many patterns appear superficially similar  
- distinguishing intentional repetition from simple sparsity or density becomes difficult  

---

**3. Ambiguity of definition**

Repetition is not a single well-defined concept:

- exact vs approximate repetition  
- rhythmic vs dynamic repetition  
- local vs global repetition  

Choosing one interpretation risks oversimplifying the phenomenon, while supporting multiple interpretations increases system complexity.

---

**4. Computational and architectural cost**

Compared to other features:

- repetition requires pairwise comparisons or windowed analysis  
- introduces additional parameters (window size, similarity thresholds)  
- increases implementation complexity and testing overhead  

This conflicts with the design goal of keeping Turn Analysis:

- lightweight  
- deterministic  
- easy to justify  

---

### 9.5 Relationship to Existing Features

Some aspects of repetition are already partially captured by existing features:

- **Density + Segment Profile** can reveal repeated activity distributions  
- **Energy** can indicate repeated dynamic patterns  
- **Anchor Detection** can highlight recurring salient positions  

While these do not explicitly model repetition, they provide indirect signals that the ResponsePlanner can already use.

---

### 9.6 Justification for Exclusion from v1

Repetition is deliberately excluded from the initial prototype.

This decision is based on the following rationale:

- limited turn length reduces reliability of detection  
- single-pad input constrains expressive variation  
- added complexity is not proportional to expected benefit in v1  
- existing features already capture the most critical aspects of short-turn behaviour  
- prioritising clarity and robustness is more important than feature completeness  

From a dissertation perspective, this decision is defensible as a scope control choice rather than a limitation of understanding.

---

### 9.7 Future Work

Repetition remains a strong candidate for future extension of the system.

It would become more valuable in scenarios where:

- longer turn windows are available  
- multiple input sources (e.g. multiple pads) increase rhythmic richness  
- stylistic modelling becomes more important  

Potential future directions include:

- motif detection using sliding window similarity  
- anchor-sequence repetition analysis  
- integration with response planning for motif development  
- probabilistic or learned repetition models  

---

### 9.8 Summary

Repetition is musically significant but context-dependent.

Given the constraints of:

- short turn duration  
- single-pad input  
- real-time performance requirements  

it is not included in the v1 Turn Analysis architecture.

However, it is recognised as an important extension point that could enhance the system’s ability to engage in more sophisticated, motif-aware musical interaction in future iterations.
