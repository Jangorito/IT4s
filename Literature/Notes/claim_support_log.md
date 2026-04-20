# Claim Support Log

Use this file to track dissertation claims, the literature that supports them, and whether more literature is needed.

## Claim 1 - Trading fours provides a structured model for musical dialogue

### Claim

Trading fours is useful for Intelli-Trading Fours because it structures improvisation as alternating short turns in which listening, interpretation, and response are mutually dependent.

### Status

Well supported.

### Best Supporting Sources

| Source | How It Supports The Claim | Strength |
|---|---|---|
| Donnay et al. 2014 | Directly studies trading fours as interactive musical improvisation and frames it as collaborative musical communication. | strong |
| Hodson 2007 | Discusses jazz improvisation as interaction, interplay, and musical conversation. | strong |
| Hoffman and Weinberg 2011 | Supports call-and-response as an evaluable musical interaction task. | moderate |

### Needs More Literature?

No, unless a deeper jazz-specific trading-fours source is desired.

### Dissertation Location

Chapter 1.1; Chapter 2.5.

### Notes

Use Donnay et al. for the direct trading-fours claim and Hodson for the broader jazz conversation/interplay framing.

---

## Claim 2 - Turn-based interaction reduces ambiguity compared with continuous co-improvisation

### Claim

Segmenting interaction into explicit turns makes the system easier to structure computationally because capture, analysis, planning, playback, and re-entry can be represented as distinct phases.

### Status

Partially supported.

### Best Supporting Sources

| Source | How It Supports The Claim | Strength |
|---|---|---|
| Donnay et al. 2014 | Supports trading fours as structured alternation in interactive improvisation. | strong for musical basis |
| Petit and Serrano - Skini | Supports state-based/reactive structuring of interactive music. | moderate for CS architecture |
| Herrington 2010 | Supports layered sensing-processing-response design in interactive music systems. | moderate |
| Rowe 1991 | Likely relevant for real-time agents and machine listening, but needs OCR/manual check. | pending |

### Needs More Literature?

Maybe. One additional HCI or interactive music source specifically about turn-taking, dialogue systems, or interaction decomposition would strengthen this.

### Dissertation Location

Chapter 1.1; Chapter 3.2.

### Notes

Avoid claiming that the literature proves turn-taking is always superior. Phrase it as a practical design choice for this system.

---

## Claim 3 - IT4s should be evaluated as an interactive process, not only as generated output

### Claim

The success of IT4s depends on the responsiveness, timing, interpretability, and perceived musical exchange of the system, not only the standalone quality of generated rhythms.

### Status

Well supported.

### Best Supporting Sources

| Source | How It Supports The Claim | Strength |
|---|---|---|
| Smith 2022 | Frames AI music systems through partnership, agency, and performer experience. | strong |
| Ostermann et al. | Evaluates a reactive jazz accompaniment system through user study and creativity/perception criteria. | strong |
| Hoffman and Weinberg 2011 | Evaluates embodiment and response accuracy in an interactive musical task. | strong |
| Pasquier et al. 2016 | Places interactive systems within musical metacreation. | moderate |

### Needs More Literature?

No for dissertation-level framing. More would only be needed for a full user-study methodology.

### Dissertation Location

Chapter 2.7; Chapter 7; Chapter 8.6.

### Notes

Use this claim to justify tests, diagnostics, debug tooling, and future drummer evaluation.

---

## Claim 4 - Rhythm can be represented as a structured grid or cyclic pattern

### Claim

Quantised rhythmic representations are defensible because rhythm can be modelled computationally as a structured sequence of onsets and silences over a temporal cycle or grid.

### Status

Well supported.

### Best Supporting Sources

| Source | How It Supports The Claim | Strength |
|---|---|---|
| Toussaint 2004 | Directly represents rhythm as cyclic binary strings. | strong |
| Longuet-Higgins and Lee 1984 | Discusses rhythmic interpretation and metrical ambiguity. | strong |
| Gillick et al. 2019 | Uses quantised drum representations and inverse transformations. | moderate |

### Needs More Literature?

No.

### Dissertation Location

Chapter 2.4; Chapter 4.4; Chapter 4.5.

### Notes

Make clear that IT4s adds velocity, sample timing, and turn bounds beyond a simple binary representation.

---

## Claim 5 - Metrical salience is a defensible basis for anchor detection and skeleton generation

### Claim

Rhythmic response planning can give special status to metrically or perceptually salient events because rhythmic interpretation depends on metrical structure, grouping, and temporal regularity.

### Status

Well supported.

### Best Supporting Sources

| Source | How It Supports The Claim | Strength |
|---|---|---|
| Longuet-Higgins and Lee 1984 | Discusses metrical interpretation, regularity, grouping, and ambiguity. | strong |
| Temperley 2009 | Treats metrical analysis as a core symbolic music-analysis task. | strong |
| Levitin, Grahn, and London 2018 | Reviews rhythm, meter, entrainment, pulse, and movement. | strong |
| Toussaint 2004 | Supports structural analysis of onset placement in rhythmic cycles. | moderate |

### Needs More Literature?

No, although a rhythm salience-specific source could be added if the anchor chapter becomes very theory-heavy.

### Dissertation Location

Chapter 2.6; Chapter 5.4; Chapter 6.4.

### Notes

This supports the concept, not every weighting choice in `AnchorAnalyser`. Specific weights should be framed as implementation choices.

---

## Claim 6 - The current response-generation layer should be framed as interim

### Claim

IT4s implements response planning and prototype structural generation, but the final full response-realisation layer remains future work.

### Status

Well supported by comparison sources and codebase evidence.

### Best Supporting Sources

| Source | How It Supports The Claim | Strength |
|---|---|---|
| Pachet 2002 | Shows a mature example of interactive style-aware continuation. | strong comparison |
| Martin and Torresen 2019 | Shows learning-based predictive interaction as a more advanced alternative. | strong comparison |
| Hu et al. | Shows dataset/model-based call-response generation. | strong comparison |
| Gillick et al. 2019 | Shows learned expressive drum generation/humanization. | strong comparison |
| Toussaint 2010 | Shows algorithmic rhythm generation ideas for future constraints. | moderate comparison |

### Needs More Literature?

No.

### Dissertation Location

Chapter 6.5; Chapter 6.6; Chapter 8.3; Chapter 8.5.

### Notes

This claim should rely primarily on codebase evidence, with literature used as comparison and future-work context.

---

