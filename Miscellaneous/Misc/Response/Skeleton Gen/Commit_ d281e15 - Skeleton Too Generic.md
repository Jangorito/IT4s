## **Commit: d281e15 \- Skeleton Too Generic**

## 

## **1\. The skeleton is becoming too generic**

Across turns 0–8, the skeleton keeps falling into a very similar visual shape:

* strong preference for the same regular lattice of steps  
* recurring end-region fill pattern  
* similar spacing profile even when the source phrasing changes  
* only modest visible effect from anchor count, profile, or ending differences

So the system is responding, but not yet **discriminating enough**.

---

## **2\. Density expansion is probably too aggressive**

A few examples:

* source density **0.083** → target **0.283**  
* source density **0.094** → target **0.294**  
* source density **0.156** → target **0.326**  
* source density **0.281** → target **0.481**

That means the skeleton is often adding a **lot** of activity relative to the source, especially once Fill/Intensify wins.

That may be okay sometimes, but as a default it risks making the generator feel like:

* “sparse human in”  
* “busy grid-y AI out”

rather than a shaped response.

---

## **3\. Planner choice is dominating too early**

The planner is repeatedly landing on:

* **Fill**  
* **Intensify**

with many close-ish cases collapsing into one of those two.

That is not automatically wrong, but in practice it means the skeleton builder is often being fed a similar intent:

* more density  
* weak ending compensation  
* front-profile assumptions  
* no anchor preservation  
* no strong ending requirement

So the skeleton is not being asked to produce enough contrasting behaviours.

---

## **4\. The ending region looks over-standardised**

The cyan ending region repeatedly becomes a similar filled tail, especially in later examples.

That suggests one of two things:

* ending weighting is too strong in selection  
* or ending region candidates are too easy to admit once density budget rises

So even before motif work, the skeleton is already “spending” too much of the musical identity on a predictable tail.

---

## **5\. Anchor information is under-leveraged**

You have examples with:

* 1 anchor  
* 2 anchors  
* 3 anchors  
* opening only  
* opening \+ middle  
* opening \+ late

…but the skeleton output still often looks like a metric scaffold with light variation.

That suggests anchors are currently more like a **small influence** than a structural driver.

---

# **My verdict**

## **Tune planner and skeleton first**

I would absolutely do this **before** trying to rescue everything downstream in the motif stage.

Because if the skeleton already lacks enough structural individuality, the motif transformer will just decorate a mediocre backbone.

That usually leads to:

* nice local edits  
* but same-y global feel

---

# **What to debug first**

## **A. Planner inflation rules**

The first thing I would inspect is how the planner converts source features into:

* `TargetDensity`  
* `ComplementarityBias`  
* `PreserveAnchors`  
* `MirrorEnding`

These are currently shaping almost everything downstream.

### **Immediate suspicion**

Your density lift for Fill/Intensify looks too eager, especially for sparse inputs.

I would test whether target density should be:

* **less additive**  
* more bounded by source density band  
* more sensitive to anchor count / segment profile  
* more conservative when the source already has clear structure

A good rule of thumb:

* **sparse** should not automatically mean **substantially denser**  
* sparse \+ clear anchors may instead mean **structured selective support**

---

## **B. Skeleton scoring balance**

Your earlier tuning already helped, but based on these outputs I’d now inspect whether the score still over-rewards:

* safe metrical steps  
* repeated even spacing  
* end-region accumulation

In other words, the builder may be producing the “best scoring” skeleton, but the score itself may still undervalue:

* profile differentiation  
* anchor-led phrasing  
* segment contrast  
* response-type-specific structure

---

## **C. Response-type distinctiveness**

Right now Fill and Intensify do not look different enough at skeleton level.

That is a problem.

At this stage, each response type should produce visibly different skeleton tendencies.

For example:

### **Fill**

Should feel like:

* connective support  
* gap occupation  
* local completion  
* more in-between placements  
* less insistence on strong repeated accents

### **Intensify**

Should feel like:

* stronger accents  
* more assertive metrical reinforcement  
* more front or phrase-point emphasis  
* controlled addition, not just “more notes everywhere”

If the screenshots do not let you tell these apart quickly, the response contracts are not yet sharp enough.

---

# **Concrete stress-test dimensions**

I’d debug this with a small matrix, not random screenshots alone.

## **1\. Source density bands**

Test fixed groups like:

* very sparse: 0.06–0.10  
* sparse: 0.10–0.16  
* medium sparse: 0.16–0.24  
* upper sparse / light medium: 0.24–0.32

Observe:

* selected count  
* target density inflation  
* distribution by segment  
* ending occupancy  
* metric class distribution

---

## **2\. Anchor configurations**

Create or collect cases with:

* opening anchor only  
* closing anchor only  
* opening \+ closing  
* mid-turn anchor only  
* 3 distributed anchors  
* no meaningful anchors

Ask:

* does the skeleton visibly reorganise?  
* or does it mostly keep the same scaffold?

---

## **3\. Segment profiles**

Specifically compare:

* front-loaded  
* back-loaded  
* flat  
* increasing  
* decreasing

The skeleton should reflect these meaningfully, especially under Fill vs Intensify.

---

## **4\. Ending conditions**

Compare:

* open ending  
* weak ending  
* strong ending

This is important because your planner is clearly reacting to ending weakness, but the resulting skeleton tails may be too uniform.

---

# **Parameters I would tune before motif work**

## **Highest priority**

1. **TargetDensity mapping**  
2. **Fill vs Intensify decision boundaries**  
3. **Response-type-specific skeleton shaping**  
4. **Ending-region contribution cap**  
5. **Anchor influence strength**

## **Lower priority for now**

6. complementarity bias fine-grain tweaks  
7. jitter  
8. motif-transformer cleverness

---

# **Specific hypotheses from these screenshots**

## **Hypothesis 1**

**TargetDensity is overshooting for sparse sources.**

Especially when:

* source is sparse  
* ending is weak/open  
* energy is medium/high

This probably pushes the greedy selector into “accept many safe metric candidates.”

---

## **Hypothesis 2**

**Fill is acting too much like generic densification.**

It should be more selective and connective, less blanket-expansive.

---

## **Hypothesis 3**

**Intensify is too close to Fill.**

If both mostly raise density and keep similar metric preferences, they will converge visually.

---

## **Hypothesis 4**

**Anchor preservation is too binary and currently too absent.**

`PreserveAnchors = No` in all these examples is a big signal.

Even if you do not want hard preservation, you may need something like:

* soft anchor retention  
* anchor-neighbour reinforcement  
* anchor-sensitive candidate quotas

Otherwise anchors become descriptive, not generative.

