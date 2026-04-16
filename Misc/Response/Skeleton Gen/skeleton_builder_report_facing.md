# Skeleton Builder Design Notes (Report-Facing)

## 1. Purpose of this document

This document captures the current design rationale for the **Skeleton Builder** layer in the Intelli-Trading 4s response-generation pipeline. It is written to support later dissertation writing, especially Chapter 3 / System Design and any methodology or design-justification sections that explain why response generation is decomposed into several small, controllable stages rather than being handled by one opaque generator.

The focus here is not implementation detail first, but **musical purpose**, **architectural role**, and **literature-supported justification**.

The Skeleton Builder sits in the currently agreed lightweight response-generation pipeline:

`ResponsePlan -> SkeletonBuilder -> MotifTransformer -> EndingAdjuster -> ConstraintPass -> PatternTurn`

Within that pipeline, the Skeleton Builder is the **first generative stage**. Its job is to take a high-level response intention produced by the ResponsePlanner and convert that intention into a **sparse structural rhythm scaffold**: a set of candidate active time positions over the response turn that captures the intended rhythmic relationship to the human player's input before later stages add local variation, phrase-shaping, or corrective cleanup.

---

## 2. Why this layer exists

The key design problem in Intelli-Trading 4s is that the system must generate a rhythm quickly enough to preserve the feel of short call-and-response trading while still sounding responsive, intelligible, and musically grounded.

A single monolithic generator would be harder to justify, harder to debug, and harder to steer in musically meaningful ways. By contrast, a layered architecture allows the system to separate:

- **intent selection** from **temporal placement**
- **global structure** from **local variation**
- **generation** from **validation / cleanup**

The Skeleton Builder exists because response generation needs an intermediate stage that answers the question:

> **Where should the response happen in time?**

This is a different problem from deciding **what kind of response** should be produced overall (handled by the ResponsePlanner) and also different from deciding how that response should later be ornamented or polished (handled by downstream stages).

So the Skeleton Builder is deliberately narrow in scope. It does not attempt to produce a final expressive performance pattern. Instead, it produces a **structural scaffold** that later modules can refine.

---

## 3. Core definition

The current working definition is:

> **Skeleton Builder converts planner intent into a sparse structural step pattern by selecting active time positions over the response turn timeline using metrical weighting, target density, source-relationship bias, anchor handling, phrase-shape cues, and lightweight constraints.**

This definition matters because it makes clear that the module is:

- **planner-driven**
- **step-based**
- **structural rather than ornamental**
- **controllable**
- **inspectable**
- **suitable for real-time interaction**

---

## 4. Why a structural scaffold is musically appropriate

### 4.1 Rhythm is not just hit count

A rhythm does not become musically convincing merely because it contains the right number of hits. Rhythmic intelligibility depends on how events are distributed relative to meter, phrase boundaries, repetition, variation, and salience. Computational rhythm research has repeatedly emphasized that “good” rhythm generation needs more than random placement of onsets; it needs some sense of organization, balance, and perceivable structure.

For this project, that means the response generator cannot simply sprinkle hits until a density target is reached. It needs a stage that reasons about **temporal placement as structure**.

### 4.2 Repetition and variation support coherence

A recurring theme across music theory, MIR, and computational generation is that listeners perceive coherence through repeated or related temporal patterns rather than through fully independent event choices. Even when systems generate new material, they often sound more intelligible when they preserve or react to structural regularities.

In this dissertation context, that supports the idea that the AI should first establish a plausible **structural backbone**, which later stages can vary modestly, rather than attempting to decide every local detail at once.

### 4.3 Meter and salience matter

Research in rhythm perception and meter highlights that listeners do not hear all temporal positions as equally important. Some steps are felt as stronger, more stable, or more structurally central than others because of inferred or explicit metrical organization. This strongly supports a builder that scores time positions partly according to metrical salience, rather than treating the response grid as flat.

### 4.4 Interactive systems benefit from controllability

In co-creative and improvisatory AI systems, controllability and intelligibility are especially important. A performer should be able to understand that the machine is reacting in a meaningful way, and the developer should be able to inspect why a particular output occurred. This is one reason the project has moved away from a heavier, less transparent “realiser” concept and toward smaller, inspectable stages.

The Skeleton Builder aligns with that principle by making structural choice explicit and debuggable.

---

## 5. Architectural role in the response-generation system

The current architecture distinguishes the following roles:

### ResponsePlanner
Chooses the broad response intention, such as mirror, complement, simplify, intensify, contrast, or fill, and derives control values such as target density, complementarity bias, anchor policy, and ending intent.

### Skeleton Builder
Transforms that intention into a **structural time-position scaffold**.

### MotifTransformer
Applies lightweight local transformation or motif logic to introduce small-scale identity or variation without undoing the structural backbone.

### EndingAdjuster
Shapes the final region of the turn so that the output closes, opens, tapers, or punches appropriately.

### ConstraintPass
Performs final validation / cleanup so the pattern remains plausible and internally consistent.

This decomposition is useful academically because it shows that the design is neither arbitrary nor over-engineered. Each stage owns a specific musical problem.

---

## 6. Why the builder should be score-and-select

The currently agreed generation paradigm is a **hybrid, score-and-select approach**.

That means the Skeleton Builder should:

1. assign a suitability score to each time step
2. use those scores plus simple constraints to choose active positions
3. optionally use light phrase-shape priors rather than heavy pattern templates

This is preferred over two extremes:

### Not a fully template-driven system
A pure template bank would be fast and easy to explain, but it risks becoming repetitive and brittle, especially in short turns where variation matters.

### Not a purely unconstrained free generator
A loose free generator might allow variety, but it would be harder to explain, harder to control, and more likely to produce structurally messy outputs.

The hybrid score-and-select approach gives a good balance:
- enough **structure** to sound purposeful
- enough **flexibility** to remain responsive
- enough **transparency** to support debugging and report writing

---

## 7. Current contract-level design

### 7.1 Input contract

The current agreed design is for the module to consume a single request object:

`SkeletonBuildRequest`

Containing:

- `ResponsePlan Plan`
- `PatternTurn SourceTurn`
- `TurnAnalysisResult SourceAnalysis`
- `int TurnLengthSteps`
- `int StepsPerQuarter`
- `SkeletonBuilderConfig Config`

This is useful because it keeps the builder grounded in:
- **intent** (`ResponsePlan`)
- **context** (`SourceTurn`, `SourceAnalysis`)
- **temporal scope** (`TurnLengthSteps`, `StepsPerQuarter`)
- **policy** (`Config`)

### 7.2 Output contract

The current preferred output is an intermediate object:

`SkeletonPattern`

Containing:
- `int TurnLengthSteps`
- `bool[] ActiveSteps`
- `float[] SelectionScores`
- `SkeletonStepMeta[] StepMeta`
- `int[] SelectedStepIndices`
- `SkeletonPatternSummary Summary`

This is intentionally **not** a final `PatternTurn`.

That separation matters because the builder is only responsible for the **structural scaffold**, not the final expressive surface.

### 7.3 Behavioural contract

The Skeleton Builder should:
- approximately satisfy the planner's target density
- reflect the intended response relationship to the source turn
- respect anchor policy
- preserve rhythmic spacing and structural plausibility
- remain under-specified enough for downstream refinement

---

## 8. Current field-level understanding

### 8.1 ResponsePlan fields relevant to Skeleton Builder

The current builder-facing plan fields are:

- `ResponseType`
- `TargetDensity`
- `ComplementarityBias`
- `PreserveAnchors`
- `EndingMode`
- `TurnLengthSteps`

This is important because it keeps the builder from re-deciding the response concept. The planner decides the broad musical relationship; the builder realises it structurally.

### 8.2 Configuration fields

The current proposed configuration fields are:

- `MetricStrengthWeight`
- `MirrorWeight`
- `ComplementWeight`
- `AnchorInfluenceWeight`
- `EndingInfluenceWeight`
- `StrongBeatPreferenceWeight`
- `MinimumStepSpacing`
- `MaximumClusterSize`
- `DensityTolerance`
- `SelectionJitter`
- `AllowOffbeatClusters`
- `RebalanceAcrossSegments`

These belong in configuration because they are tuning and policy values, not musical intent.

### 8.3 Step metadata

The current preferred design also includes `SkeletonStepMeta`, which exposes the reasons each step was or was not selected. This is especially valuable for debug UI, system interpretation, and dissertation explanation because it makes the builder's behaviour inspectable rather than opaque.

---

## 9. Literature framing for the report

This section gathers the main literature ideas that can justify the Skeleton Builder chapter later. It is deliberately phrased as design support rather than finished dissertation prose.

### 9.1 Structural rhythm generation

Several computational rhythm-generation papers argue that convincing rhythmic output depends on structural properties rather than only on local event probabilities. This supports the decision to include an explicit scaffold-building stage rather than generating final output in one shot.

Useful takeaways:
- rhythm generation benefits from explicit organization
- temporal structure can be modelled separately from surface detail
- generated rhythm often sounds more coherent when shaped around structural regularities

### 9.2 Repetition, hierarchy, and temporal organization

Work on rhythmic and broader musical structure repeatedly shows that repetition and hierarchical organization contribute to intelligibility and form. Even when the project is not building full song-scale structure, the same principle still matters at short turn level: a two-bar response benefits from a recognisable internal organization rather than undifferentiated event placement.

This is one of the strongest reasons for describing the Skeleton Builder as a **structure first** module.

### 9.3 Meter, beat salience, and strong/weak positions

Research in rhythm cognition and computational meter reinforces the idea that time positions are not perceived equally. Some steps carry stronger metric or perceptual salience. This supports the use of **metrical weighting** and **strong-beat preference** in the builder.

For the dissertation, this gives a direct bridge between musical theory / cognition and algorithm design:
- metrical hierarchy -> weighting model
- beat salience -> step scoring
- strong/weak distinction -> placement bias

### 9.4 Co-creative control and transparency

Interactive improvisation and co-creative music systems place special value on responsiveness, user steering, and intelligibility. This supports a controllable, inspectable generation stage whose decisions can be explained and tuned. It also helps justify why the project favours smaller, explicit modules rather than a purely end-to-end black-box model at this stage of development.

### 9.5 Structure under latency constraints

The project has a specific real-time design pressure: the system must respond quickly enough for trading-fours-style interaction. That means the architecture needs a compromise between musical sophistication and computational simplicity. A lightweight structural scaffold builder is a strong fit because it allows meaningful response generation without the overhead of deeper search or more complex global realisation.

---

## 10. Dissertation-writing angles to preserve

When this material is later turned into dissertation prose, the most useful framing angles are likely:

### A. Separation of musical problems
The architecture separates:
- deciding response intent
- placing structural events in time
- applying local transformation
- shaping the ending
- validating the result

### B. Explainable, controllable generation
The system is not only generating rhythms; it is doing so in a way that can be inspected, reasoned about, and tuned.

### C. Music-theoretic grounding
The builder reflects concepts such as:
- metrical hierarchy
- salience
- phrase shaping
- anchor points
- structural balance
- complementarity vs mirroring

### D. Suitability for short interactive turns
The design is intentionally lighter than a full generative “composition” model because the application demands near-immediate call-and-response behaviour.

---

## 11. Current unresolved sections to add next

The following sections should be expanded next and may require retrospective edits to earlier parts of this document once they are locked:

### 11.1 Step scoring dimensions
To be added:
- how metric score is computed
- how source relation score is computed
- how anchors influence step desirability
- how ending-region bias works
- how these terms combine into a final step score

### 11.2 Selection algorithm
To be added:
- how target density is converted into a count
- how candidates are ranked / selected
- how spacing and clustering constraints are enforced
- how segment rebalance works if enabled

### 11.3 Stochasticity policy
To be added:
- what role bounded randomness should play
- where tie-breaking or jitter is allowed
- how to preserve responsiveness without over-randomising the result

### 11.4 Debug / analysis visibility
To be added:
- exact debug snapshot contents
- how the builder will expose reasons for selected steps
- how this supports implementation tuning and report explanation

### 11.5 Limitations and future work
To be added:
- what the builder does not yet model
- future possibilities such as larger phrase templates, history-aware structure, multi-instrument coordination, or learned priors

---

## 12. Current provisional summary

At the current stage, the Skeleton Builder can be summarised as follows:

- It is the first generative stage after planning.
- It converts high-level response intent into a sparse structural rhythm scaffold.
- It is designed to be lightweight, controllable, and inspectable.
- It is justified by literature on rhythmic structure, metrical salience, repetition, and interactive musical control.
- It is intentionally narrower than a full “realiser” so that the system remains suitable for short, responsive human–AI trading.

That makes it a strong fit for Intelli-Trading 4s both technically and dissertation-wise.

---

## 13. Source notes and literature links

These links are included so they can later be attached, cited, or mined for stronger dissertation phrasing. They are not yet formatted into the final dissertation citation style.

1. Toussaint, G. T. — *Generating “Good” Musical Rhythms Algorithmically*  
   https://cgm.cs.mcgill.ca/~godfried/publications/Hawaii-Paper-Rhythm-Generation.pdf

2. Paiement, J.-F. et al. — *A Generative Model for Rhythms*  
   https://research.google.com/pubs/archive/34393.pdf

3. Dai, S., Zhang, H., Dannenberg, R. B. — *Automatic Analysis and Influence of Hierarchical Structure on Melody, Rhythm and Harmony in Popular Music*  
   https://arxiv.org/abs/2010.07518

4. Wei, I.-C. et al. — *Generating Structured Drum Pattern Using Conditional GAN with Self-Similarity Matrix*  
   https://sma1033.github.io/site/paper/drum_generation_paper.pdf

5. Vuust, P., Witek, M. A. G. — *A novel approach to modeling rhythm and meter perception in music*  
   https://www.frontiersin.org/journals/psychology/articles/10.3389/fpsyg.2014.01111/pdf

6. Grahn, J. A., Brett, M. — *Rhythm and Beat Perception in Motor Areas of the Brain*  
   https://www.jessicagrahn.com/uploads/6/0/8/5/6085172/grahn_brett_rhythm_and_beat_perception_in_motor_areas_of_the_brain.pdf

7. Roman, I. R. et al. — *Dynamic models for musical rhythm perception and coordination*  
   https://qmro.qmul.ac.uk/xmlui/bitstream/handle/123456789/102540/Roman%20Dynamic%20models%20for%20musical%20rhythm%20perception%20and%20coordination%202023%20Published.pdf?isAllowed=y&sequence=2

8. Arias, J. et al. — *Automatic Construction of Interactive Machine Improvisation Scenarios from Audio Recordings*  
   https://musicalmetacreation.org/mume2016/proceedings/Arias_automatic_construction.pdf

9. Martin, C. P. et al. — *Performing with a Generative Electronic Music Controller*  
   https://hai-gen.github.io/2022/papers/paper-HAIGEN-MartinCharles.pdf

10. Thelle, N. J. W. et al. — *Co-Creative Spaces: The machine as a collaborator*  
    https://nime.org/proceedings/2023/nime2023_35.pdf

11. Boenn, G. — *Computational Music Theory*  
    https://musicalmetacreation.org/mume2012/content/proceedings/Computational%20Music%20Theory.pdf

12. Ellis, D. P. W., Poliner, G. E. — *Drum pattern basis sets for classification and generation*  
    https://www.ee.columbia.edu/~dpwe/pubs/ismir04-eigenrhythm.pdf