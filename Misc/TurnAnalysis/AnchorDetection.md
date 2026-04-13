1. Anchor Detection — Report Document
# Anchor Detection (Structural Salience Model)

## 1. Overview

Anchor Detection identifies structurally salient events within a rhythmic turn.

An *anchor* is defined as a hit that exhibits sufficient salience across a combination of:
- dynamic prominence
- structural placement
- contextual separation

Unlike simpler activity-based features (e.g. density or energy), Anchor Detection operates at the **event level**, identifying *which specific moments* in a turn are musically important.

This allows the system to move beyond describing *how much happens* to identifying *what matters*.

---

## 2. Motivation

In human rhythmic perception, certain events function as structural reference points. These may:
- establish the beginning of a phrase
- articulate the underlying pulse
- reinforce metric structure
- signal closure or transition

These events are not defined solely by loudness or timing, but by a combination of perceptual cues.

Anchor Detection formalises this intuition into a computational model suitable for:
- real-time analysis
- short-turn interaction (1–2 bars)
- single-pad input systems

---

## 3. Feature Structure

Anchor Detection produces a multi-level feature representation:

### 3.1 Event-level features
- Per-step salience scores
- Binary anchor flags

### 3.2 Aggregate features
- Total anchor count
- Anchor indices
- Strongest anchor (index + score)

### 3.3 Structural indicators
- Opening anchor presence
- Closing anchor presence
- Anchor counts per segment

This layered structure allows both:
- fine-grained reasoning (event-level)
- higher-level structural interpretation

---

## 4. Salience Model

Each active step is assigned a continuous salience score:


AnchorSalience(i) =
0.30 * VelocityScore(i)

0.20 * LocalAccentScore(i)
0.15 * IsolationScore(i)
0.25 * MetricalWeightScore(i)
0.10 * PhraseRoleScore(i)

A step is classified as an anchor if:


AnchorSalience(i) ≥ AnchorThreshold


Where:


AnchorThreshold = 0.55


---

## 5. Evidence Types

The model combines three categories of musical evidence.

---

### 5.1 Dynamic Prominence

Captures how much an event stands out in terms of intensity.

#### VelocityScore
Normalised hit velocity:
- higher velocity → higher salience

#### LocalAccentScore
Measures contrast with neighbouring hits:
- events louder than their local context are emphasised
- prevents uniform patterns from producing excessive anchors

---

### 5.2 Contextual Separation

#### IsolationScore
Measures how isolated an event is within its local neighbourhood:
- events surrounded by silence are more perceptually salient
- supports identification of sparse, punctuating hits

---

### 5.3 Structural Placement

This is the key extension beyond the initial model.

---

#### 5.3.1 Metrical Weight

Captures the strength of a step within the temporal grid.

Stronger positions include:
- bar boundaries
- beat positions
- major subdivisions

Weaker positions include:
- fine subdivisions
- ornamental placements

This reflects the perceptual importance of temporal hierarchy in rhythm.

---

#### 5.3.2 Phrase Role

Captures the contribution of an event to the **shape of the turn as a phrase**.

Events are emphasised if they:
- initiate the phrase
- articulate structural regions
- contribute to closure

This is particularly important in short, interactive turns.

---

## 6. Metrical Weight Model

The metrical hierarchy is derived from the quantised grid.

Given:
- steps per quarter = 12
- 4/4 time

Then:
- 48 steps per bar
- 96 steps for 2 bars

### Strength tiers

| Position Type       | Score |
|--------------------|------|
| Bar start          | 1.00 |
| Beat start         | 0.75 |
| Half-beat          | 0.45 |
| Weak subdivision   | 0.20 |

This hierarchy ensures that anchor detection reflects temporal structure rather than purely local contrast.

---

## 7. Phrase Role Model

Phrase-role weighting is based on structural regions within the turn.

### Scoring scheme

| Role                         | Score |
|------------------------------|------|
| First or last active event   | 1.00 |
| Opening / closing region     | 0.75 |
| Midpoint structural region   | 0.45 |
| Elsewhere                    | 0.00 |

---

### 7.1 Window definitions (4/4, 12 SPQ)

#### 1-bar (48 steps)
- Opening: steps 0–11
- Midpoint: steps 24–35
- Closing: steps 36–47

#### 2-bar (96 steps)
- Opening: steps 0–11
- Midpoint: steps 48–59
- Closing: steps 84–95

---

## 8. Interpretation

A hit is considered an anchor when it is:

- perceptually prominent (velocity / accent)
- not masked by surrounding activity (isolation)
- located at a structurally strong temporal position (metrical weight)
- contributing to phrase articulation (phrase role)

This results in a model that balances:
- local perceptual salience
- global structural context

---

## 9. Design Rationale

This formulation was chosen to satisfy:

### Musical validity
- aligns with perceptual and theoretical notions of rhythmic salience
- incorporates both timing and intensity

### Computational efficiency
- operates per-step with O(n) complexity
- suitable for real-time systems

### System constraints
- works with single-pad input
- robust for short (1–2 bar) turns

### Extensibility
- additional evidence (e.g. repetition) can be added later without redesign

---

## 10. Limitations and Future Work

Potential extensions include:
- repetition / reinforcement detection
- adaptive thresholding
- style-dependent weighting
- integration with response generation constraints

---

## 11. Summary

Anchor Detection provides a principled method for identifying structurally important events in short rhythmic sequences.

By combining:
- dynamic prominence
- structural placement
- contextual separation

the model produces a musically meaningful representation that supports downstream processes such as:
- response planning
- motif transformation
- constraint evaluation