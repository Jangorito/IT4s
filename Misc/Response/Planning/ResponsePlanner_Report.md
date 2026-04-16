# ResponsePlanner Design
## Intelli-Trading 4s – Report-Facing Documentation

---

## 1. Overview

The ResponsePlanner is the **musical decision layer** of the Intelli-Trading 4s response generation system.

Its purpose is to interpret the analysed human turn and decide what kind of response the AI should attempt. Rather than generating rhythm directly, it converts extracted rhythmic features into a compact **response strategy** that can be realised by downstream components.

In the architecture, the ResponsePlanner sits between **Turn Analysis** and **Response Generation**.

---

## 2. Role in the System

Human Turn  
→ Turn Analysis  
→ **ResponsePlanner**  
→ ResponsePlan  
→ StructureBuilder  
→ MotifTransformer  
→ EndingAdjuster  
→ ConstraintPass  
→ PatternTurn

The ResponsePlanner does not place hits itself. Instead, it determines the **intent** of the response and expresses that intent in a form that later stages can implement.

---

## 3. Design Rationale

The project operates under several constraints:

- short turn lengths (typically 1–2 bars)
- single-pad input in the current implementation
- requirement for near-instant reply generation
- need for a musically interpretable system suitable for dissertation justification

Earlier, more elaborate ideas such as full candidate generation, deep search, or larger-scale evaluative planning were considered. However, these were judged too heavy for the temporal scale and responsiveness required by trading-fours-style interaction.

This led to a deliberately lightweight planner design.

The ResponsePlanner therefore exists to solve a very specific problem:

> given a compact description of the human turn, decide how the AI should respond in a musically meaningful way.

This separation is important. It prevents the generation pipeline from becoming monolithic, while also making the system easier to explain, tune, and justify.

---

## 4. Core Concept

The ResponsePlanner maps **analytical understanding** onto **musical response intent**.

In practice, it takes a `TurnAnalysisResult` and produces a `ResponsePlan`.

The analysis side describes what happened in the human turn.  
The planning side decides what the system should do about it.

This is the point in the architecture where the system moves from:

- describing rhythmic behaviour

to:

- selecting an interaction strategy

---

## 5. Inputs from Turn Analysis

The planner operates on the structured output of Turn Analysis. The exact internal feature models may evolve, but the planner is designed around five established analytical families.

### 5.1 Density

Density describes how much rhythmic activity is present.

This may include:

- global density across the full turn
- local or segment-based density variation
- whether the pattern is sparse, balanced, or busy

Density is one of the strongest factors in deciding whether the response should simplify, intensify, mirror, or fill.

### 5.2 Energy

Energy describes the perceived force or intensity of the turn.

In the current system this is especially useful for distinguishing between:

- restrained versus assertive input
- stable versus highly active gesture profiles

Energy helps prevent the planner from treating all equally dense patterns as musically equivalent.

### 5.3 Anchor Structure

Anchor-related features identify structurally salient hits.

These indicate whether the source contains events that feel especially important in shaping the turn’s identity. In planning terms, anchors matter because they can be:

- preserved
- echoed
- opposed
- ignored

depending on the response strategy.

### 5.4 End Activity

End activity captures what happens in the final portion of the turn.

This is important because, in short musical exchanges, endings strongly affect perceived phrase shape. A turn may end with:

- strong closure
- tapering release
- late activity burst
- open continuation

These distinctions help the planner decide how the AI should close its own response.

### 5.5 Segment Activity Profile

The segment activity profile captures how activity is distributed across the turn.

Rather than asking only how active the turn is overall, this allows the planner to consider whether activity is:

- front-loaded
- back-loaded
- evenly distributed
- concentrated in particular regions

This supports more musically aware complement or contrast decisions.

---

## 6. Planner Responsibilities

The ResponsePlanner has three main responsibilities.

### 6.1 Interpret the Human Turn

It must convert numerical or structural features into a musically meaningful internal interpretation.

For example:

- sparse with strong anchors
- busy and high-energy
- balanced but open-ended
- quiet start with active ending

This interpretive step makes the rest of planning more coherent.

### 6.2 Select a Response Strategy

The planner must choose the broad musical character of the reply.

This is expressed through a small set of response types:

- Mirror
- Complement
- Simplify
- Intensify
- Contrast
- Fill

These are not arbitrary labels. They define the intended relationship between the source turn and the AI response.

### 6.3 Derive Control Parameters

Once response type is selected, the planner must derive a compact set of parameters that downstream stages can realise.

Typical outputs include:

- target density
- complementarity bias
- anchor preservation
- ending behaviour
- turn length

The planner therefore acts as a translator from **analysis** to **generation control**.

---

## 7. Response Types

The planner’s main high-level decision is the selection of `ResponseType`.

### 7.1 Mirror

Mirror aims to preserve a recognisable relationship with the source turn.

This does not require exact copying. Instead, it favours:

- alignment with source activity
- moderate density similarity
- possible reuse of salient anchors
- coherent ending relationship

Mirror is useful when the source already exhibits a strong, balanced identity.

### 7.2 Complement

Complement aims to create a call-and-response relationship.

Rather than occupying the same spaces as the source, it tends to:

- favour gaps
- preserve dialogue
- avoid excessive overlap
- maintain recognisable relation without duplication

This is likely to be one of the most musically useful default strategies in conversational rhythmic interaction.

### 7.3 Simplify

Simplify reduces complexity relative to the source.

It is appropriate when the input is already busy, dense, or highly active. Its goal is not to flatten the music, but to produce a clearer answer by:

- reducing hit count
- favouring stronger positions
- limiting clustering
- preserving only key structure

### 7.4 Intensify

Intensify increases activity or emphasis relative to the source.

This is useful when the input is sparse, underdeveloped, or musically inviting of escalation. It may involve:

- modest density increase
- greater spread
- stronger ending emphasis
- slightly more assertive response character

### 7.5 Contrast

Contrast deliberately shifts away from the source profile.

This does not mean random opposition. Rather, it means creating a noticeably different response when mirroring or complementing would feel too predictable or too congested.

Contrast may be useful when the source is strongly patterned, over-dense, or aesthetically improved by a different contour.

### 7.6 Fill

Fill targets the sense that the source leaves space that could be answered more actively.

Unlike general intensification, Fill often has a more local or phrase-shaping role, especially when:

- activity is very sparse
- the ending is weak or under-articulated
- the response should help complete the exchange

---

## 8. Planning Logic

The planner uses a **rule-guided, weighted decision process** rather than a full search or optimisation system.

A useful way to understand this is as a two-stage process.

### 8.1 Stage A: Response Type Selection

The planner first decides the broad strategic response type.

This decision is driven by combinations of features, such as:

- source density
- source energy
- presence and placement of anchors
- end activity profile
- segment distribution

For example:

- high density + high energy may favour Simplify or Contrast
- sparse input + weak ending may favour Fill or Intensify
- balanced input + clear anchors may favour Mirror or Complement
- strong source occupancy with meaningful gaps may favour Complement

This stage answers the question:

> what kind of reply should this be?

### 8.2 Stage B: Parameter Derivation

Once response type is chosen, the planner derives the control values that instantiate it.

For example:

- how dense should the response be?
- how strongly should it avoid or align with source hits?
- should anchors be preserved?
- should the ending mirror, remain open, taper, or punch?

This stage answers the question:

> how should that chosen strategy be expressed numerically and structurally?

---

## 9. ResponsePlan

The planner outputs a compact `ResponsePlan`.

The aim is to keep this representation **small, interpretable, and implementation-ready**.

### 9.1 Core Fields

A practical v1 ResponsePlan should include:

- `ResponseType`
- `TargetDensity`
- `ComplementarityBias`
- `PreserveAnchors`
- `MirrorEnding`
- `TurnLengthSteps`

### 9.2 Why This Output Should Stay Lean

It is tempting to let the planner control every detail of generation. However, this would make the architecture harder to justify and more difficult to maintain.

A lean plan is preferable because it:

- preserves modularity
- keeps responsibilities clear
- limits overfitting of rules
- lets downstream stages remain lightweight

This is consistent with the overall architecture of the response generation system.

---

## 10. Relationship with Downstream Components

The ResponsePlanner only works well if its output maps cleanly onto the behaviour of the later stages.

### 10.1 StructureBuilder

The StructureBuilder uses planner output to decide where hits should go.

It is the main generative component, so the planner must provide the core structural guidance for:

- density target
- alignment versus complementarity
- anchor treatment
- turn length

### 10.2 MotifTransformer

The MotifTransformer introduces limited rhythmic reuse.

The planner should influence motif behaviour indirectly, mainly through response type and anchor decisions, rather than micromanaging transformation details.

### 10.3 EndingAdjuster

The EndingAdjuster shapes the final region of the response.

This makes end-activity-sensitive planning especially important. The planner should decide whether the response ending ought to:

- mirror the source
- remain open
- taper
- punch
- fill

### 10.4 ConstraintPass

The ConstraintPass validates and cleans the final result.

The planner should not attempt to replace this stage. Instead, it should assume that downstream generation may require deterministic cleanup.

---

## 11. Advantages of This Design

The ResponsePlanner provides several architectural advantages.

### 11.1 Interpretability

Because the planner explicitly maps features to response strategies, it is much easier to justify than a black-box generator.

### 11.2 Responsiveness

The planner is lightweight enough for short-form interactive use.

### 11.3 Separation of Concerns

Analysis, planning, generation, ending shaping, and validation remain distinct.

### 11.4 Dissertation Strength

This architecture supports clear academic framing around:

- feature-driven musical interaction
- interpretable AI behaviour
- modular real-time system design
- practical constraint-aware improvisation

---

## 12. Relation to Explored Alternatives

Several richer designs were considered during development, including:

- candidate generation and evaluation loops
- large scoring systems
- heavier motif planning
- more elaborate phrase-level modelling
- wider optimisation-based response selection

These ideas were not discarded because they were theoretically poor, but because they offered limited practical benefit within the current project constraints.

They remain useful as discussion points for future work and dissertation reflection.

---

## 13. Dissertation Framing

In dissertation terms, the ResponsePlanner can be framed as:

> a rule-guided musical decision module that translates extracted rhythmic features into a compact response strategy for real-time AI-human interaction.

This framing is powerful because it positions the planner as:

- musically grounded
- systemically interpretable
- technically pragmatic
- justified by real-time interaction constraints

It also helps distinguish the project from both naïve rule systems and opaque end-to-end generation approaches.

---

## 14. Future Extensions

Possible future improvements include:

- adaptive weighting learned from interaction history
- style-sensitive response preferences
- richer ending mode control
- explicit motif reuse strength
- multi-pad or multi-instrument planning
- probabilistic planning rather than bounded rule weighting
- integration with a larger evaluation model

These are valuable extensions, but they are not required for a strong and coherent v1 architecture.

---

## 15. Conclusion

The ResponsePlanner is the stage that gives the response generation system **musical purpose**.

Turn Analysis tells the system what happened.  
The ResponsePlanner decides how to answer.

By converting analysed rhythmic features into a compact, interpretable `ResponsePlan`, it enables the later generation stages to remain fast, modular, and musically directed.

For Intelli-Trading 4s, this makes the ResponsePlanner the essential bridge between **rhythmic understanding** and **responsive musical action**.
