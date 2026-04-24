# AnchorAnalyser V2 — Implementation Specification

## 1. Purpose

Compute structural anchor features from a PatternTurn.

Outputs:
- per-step salience scores
- anchor flags
- anchor indices and counts
- strongest anchor
- structural summaries

---

## 2. Core Formula

For each active step i:

AnchorSalience(i) =
    0.30 * VelocityScore(i)
  + 0.20 * LocalAccentScore(i)
  + 0.15 * IsolationScore(i)
  + 0.25 * MetricalWeightScore(i)
  + 0.10 * PhraseRoleScore(i)

AnchorThreshold = 0.55

IsAnchor(i) = salience >= threshold

---

## 3. Inputs

PatternTurn:
- int[] velocity
- int StepCount
- int bars
- int beatsPerBar

Assume:
- stepsPerQuarter = 12

---

## 4. Component Functions

---

### 4.1 VelocityScore

float VelocityScore(int velocity)
{
    return Clamp01(velocity / 127f);
}

---

### 4.2 LocalAccentScore

Use local window (radius = 2):

- compute mean velocity of active neighbours
- return positive contrast only

float LocalAccentScore(...)
{
    score = (v_i - localMean) / 127
    return Clamp01(score)
}

---

### 4.3 IsolationScore

Count inactive neighbours in radius window:

float IsolationScore(...)
{
    return inactiveCount / totalNeighbourSlots
}

---

### 4.4 MetricalWeightScore

Derive:

stepsPerBar = stepCount / bars  
stepsPerBeat = stepsPerBar / beatsPerBar  

Rules:

if stepIndex % stepsPerBar == 0
    return 1.00

if stepIndex % stepsPerBeat == 0
    return 0.75

if stepsPerBeat % 2 == 0 AND stepIndex % (stepsPerBeat / 2) == 0
    return 0.45

return 0.20

---

### 4.5 PhraseRoleScore

Precompute:
- firstActiveIndex
- lastActiveIndex

Define windows (based on stepCount):

If 48 steps:
    opening: 0–11
    midpoint: 24–35
    closing: 36–47

If 96 steps:
    opening: 0–11
    midpoint: 48–59
    closing: 84–95

Logic:

if i == firstActiveIndex OR i == lastActiveIndex
    return 1.00

if i in opening OR closing window
    return 0.75

if i in midpoint window
    return 0.45

return 0.00

---

## 5. Processing Loop

For each step i:

if velocity[i] <= 0
    continue

salience = weighted sum

store salienceScores[i]

if salience >= threshold:
    mark anchor
    track index
    update strongest anchor

---

## 6. Post-processing

Compute:

- AnchorCount
- AnchorIndices
- StrongestAnchorIndex
- StrongestAnchorScore
- HasOpeningAnchor
- HasClosingAnchor
- AnchorCountsPerSegment (reuse SegmentHelper)

---

## 7. Output

Return AnchorFeatures with:

- StepSalienceScores
- StepIsAnchor
- AnchorCount
- AnchorIndices
- StrongestAnchorIndex
- StrongestAnchorScore
- HasOpeningAnchor
- HasClosingAnchor
- AnchorCountsPerSegment

---

## 8. Constraints

- O(n) runtime
- no allocations inside loop (except arrays)
- deterministic
- no external dependencies

---

## 9. Notes

- Replace PositionalScore with PhraseRoleScore
- Add MetricalWeightScore
- Keep existing structure of AnchorAnalyser intact
- Do not modify other analysers

---

## 10. Future Extensions

- relative thresholding (top-k anchors)
- repetition score integration
- adaptive weighting