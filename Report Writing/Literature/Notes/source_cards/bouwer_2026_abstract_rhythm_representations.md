# Bouwer et al. 2026 - Abstract Rhythm Representations

## Bibliographic Info

**Citation key:** bouwer2026abstractrhythm  
**Authors:** Fleur L. Bouwer, Atser Damsma, Thomas M. Kaplan, Mohsen Ghorashi Sarvestani, and Marcus T. Pearce  
**Year:** 2026  
**Title:** Abstract representations underlie rhythm perception and production: Evidence from a probabilistic model of temporal structure  
**Venue / publisher:** Cognition, 268, 106345  
**DOI / URL:** 10.1016/j.cognition.2025.106345  
**PDF filename:** Abstract representations underlie rhythm perception and production.pdf

## Processing Status

**Status:** parsed  
**Parsing quality:** good  
**Needs manual check:** no

## Keywords

- rhythm perception
- rhythm production
- temporal expectation
- probabilistic modelling
- inter-onset interval
- ratio
- contour
- tapping
- complexity

## Tags

- #rhythm-representation
- #metrical-salience
- #quantisation
- #machine-listening
- #evaluation

## One-Sentence Use

Use this source to support the claim that rhythmic pattern perception and production rely on abstract, partly categorical temporal representations rather than exact raw timing alone.

## Core Concepts

- Rhythmic patterns can support temporal prediction in perception and action.
- The paper models rhythmic expectation at three representational levels: absolute inter-onset intervals, interval ratios, and contour/direction of interval change.
- Behaviour across implicit perception, explicit complexity rating, and tapping tasks was affected by ratio and contour predictability.
- Absolute IOI predictability mattered more when the task explicitly required rhythm processing or motor synchronisation.
- The findings support flexible, task-dependent rhythmic representations.

## Summary

Bouwer et al. investigate how humans represent rhythmic patterns by combining probabilistic modelling with behavioural experiments. They generate rhythmic stimuli and model their predictability at three levels of abstraction: exact absolute inter-onset intervals, ratios between successive intervals, and contour or direction of interval change. Participants complete an implicit target-detection task, an explicit complexity-rating task, and a tapping task. Across tasks, behaviour is affected by the predictability of ratio and contour representations, while absolute IOI representations have a more limited and task-dependent effect. The authors argue that humans rely substantially on abstract and imprecise representations of rhythmic structure, although task demands can increase reliance on more precise timing information. This is useful for IT4s because it supports treating rhythm as structured temporal information that can be abstracted for analysis and planning, rather than as only a stream of exact raw timestamps.

## Relevance To Intelli-Trading Fours

This paper strengthens the justification for converting captured drum events into an abstract symbolic representation before analysis and response planning. IT4s preserves sample-level timing and offset information, but its main analysis and planning layers reason over grid steps, density, energy shape, anchors, and response structure. Bouwer et al. provide cognitive support for this design direction: rhythmic understanding can operate at representational levels above exact IOI timing. The paper is especially useful for explaining why a quantised `PatternTurn` can still be musically meaningful, provided the dissertation remains honest about what expressive timing is simplified or deferred.

## Useful Dissertation Sections

| Section | Use |
|---|---|
| Chapter 2.4 | Support abstraction from raw timing to rhythmic representation. |
| Chapter 2.6 | Strengthen the argument for structural rhythmic features beyond exact onset times. |
| Chapter 4.4 / 4.5 | Support `PatternTurn` as a symbolic representation, while noting microtiming trade-offs. |
| Chapter 5 | Support feature extraction over abstract rhythmic structure. |
| Chapter 7 / 8 | Support evaluating timing and rhythmic complexity separately from raw output quality. |

## Claims This Source Can Support

- Humans can process rhythmic patterns through abstract representations such as interval ratios and contour.
- Exact absolute timing is not the only level at which rhythmic structure is represented.
- Task demands affect the level of rhythmic representation used in perception and production.
- Probabilistic models of rhythmic expectation can be used to study perceived complexity and tapping behaviour.

## Claims This Source Does Not Support

- It does not evaluate interactive AI music systems.
- It does not support claims about trading fours, turn-taking, or human-AI collaboration.
- It does not validate IT4s' specific quantisation algorithm or response planner.

## Methods / Systems Mentioned

- Probabilistic modelling of auditory expectations.
- Prediction by Partial Matching and IDyOM-inspired modelling.
- Implicit target detection, explicit complexity ratings, and tapping tasks.

## Evaluation Notes

- Useful as cognitive support, not as direct system evaluation.
- The paper separates perceptual, explicit judgement, and motor tasks, which can inspire IT4s evaluation criteria.
- It supports the idea that musical plausibility may depend on different timing representations under different task demands.

## Useful Paraphrased Points

- Rhythmic pattern processing can be modelled at multiple levels of abstraction.
- Relative and contour-based temporal relationships can affect perception and action even when absolute timing is less central.
- Symbolic rhythmic abstraction is a defensible design choice when paired with an honest discussion of expressive timing loss.

## Short Quotable Excerpts

Keep excerpts short and page-referenced.

| Page | Excerpt | Use |
|---|---|---|
| 1 | "ratio and contour affected behavioral responses" | Supports abstract rhythmic representation. |

## Related Sources

- Longuet-Higgins and Lee 1984
- Levitin, Grahn, and London 2018
- Thaut, Trimarchi, and Parsons 2014
- Gillick et al. 2019

## Notes For Writing

- Best used to make Chapter 2.6 more current and specific.
- Avoid overloading Chapter 2 with modelling detail; save deeper discussion for representation or analysis chapters.
