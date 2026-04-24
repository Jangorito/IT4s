Alright — this is where we lock it in so Chapter 5 basically writes itself.

What I’m giving you now is **not prose**, it’s a **production blueprint** you can follow step-by-step. If you stick to this, you won’t drift, you won’t forget evidence, and you won’t undercook any section.

---

# 🧱 Chapter 5 — Drafting Blueprint (Final)

## 🎯 Target length

* **Total**: ~2,500–3,000 words
* Bias toward **figures + tables over text**

---

# 🧠 5.1 Evaluation Framework and Scope

### 🎯 Target: 200–300 words

## Purpose

Define:

* what you evaluate
* what you don’t
* how evidence is structured

---

## Content blocks

### 1. Opening (2–3 sentences)

* This chapter evaluates IT4s as:

  * interactive system
  * closed-loop architecture
  * planner-driven system

---

### 2. Evaluation framing

Say explicitly:

* not evaluating:

  * perceptual musical quality
  * user study / co-creativity
* evaluating:

  * behaviour
  * structure
  * responsiveness
  * real-time execution

---

### 3. Evaluation questions

### 📊 Table 5.1 — Evaluation Questions and Evidence

| Claim               | Question                       | Evidence         | Section |
| ------------------- | ------------------------------ | ---------------- | ------- |
| Reactivity          | Does output change with input? | Variant analysis | 5.3     |
| Interpretability    | Are decisions explainable?     | Planner margins  | 5.3     |
| Structural validity | Are outputs distinct?          | Skeleton metrics | 5.4     |
| Real-time           | Is latency acceptable?         | Timing logs      | 5.5     |
| Fidelity            | Does output match plan?        | Plan vs Output   | 5.6     |

---

### 4. Evidence types (bullet list)

* unit tests
* batch datasets
* debug tools
* timing logs

---

## ⚠️ Rules

* No results
* No interpretation
* Just framing

---

# 🧠 5.2 Subsystem Correctness

### 🎯 Target: 250–350 words

## Purpose

Establish:

> the system works → evaluation is valid

---

## Content blocks

### 1. Pipeline recap (VERY short)

Just 1–2 sentences:

* capture → compile → analyse → plan → generate → playback

---

### 2. Subsystem verification

### 📊 Table 5.2 — Subsystem Evaluation Summary

| Subsystem | Evidence   | Verified Behaviour              |
| --------- | ---------- | ------------------------------- |
| Capture   | tests      | correct segmentation            |
| Compiler  | inspection | correct quantisation            |
| Analyser  | tests      | correct features                |
| Planner   | tests      | deterministic + interpretable   |
| Generator | tests      | structural constraints enforced |
| Loop      | debug      | closed-loop execution           |

---

### 3. Short conclusion

> System is stable enough for behavioural evaluation

---

## ⚠️ Rules

* No deep analysis
* No repeating Chapter 3
* Keep it tight

---

# 🧠 5.3 Planner Behaviour, Interpretability, and Reactivity

### 🎯 Target: 600–700 words (CORE SECTION)

---

## 🔥 Structure

---

## 5.3.1 Response Distribution

### 📊 Figure 5.1 — Planner Distribution

(bar chart)

Use F1:

* Intensify: 39.33%
* Fill/Mirror: 20.67%
* Complement/Contrast lower 

---

### Write:

* non-degenerate behaviour
* structured bias (NOT “balanced”)
* intensify dominance

---

## 5.3.2 Decision Confidence

### 📊 Figure 5.2 — Margin Distribution

Use F1 margins:

* 0 → ambiguous
* high → confident

---

### Key point

> planner exposes uncertainty

---

## 5.3.3 Reactivity (Variant Analysis)

### 📊 Figure 5.3 — Variant Change Rate

Use F3:

* 23.33% changed
* feature-dependent variation 

---

### Key interpretation

* not rigid
* not unstable

---

## 5.3.4 Transition Behaviour

Use:

* Fill → Intensify
* Mirror → Intensify
* Complement → Mirror

---

### Key insight

> structured decision boundaries (not random)

---

## 🧠 Section conclusion

> planner is interpretable, structured, and input-sensitive

---

# 🧠 5.4 Structural Response Distinctiveness

### 🎯 Target: 600–700 words (CORE SECTION)

---

## 🔥 Structure

---

## 5.4.1 Aggregate Structural Profiles

### 📊 Table 5.4 — Skeleton Metrics

Use F2:

* overlap vs gap-fill patterns
* anchor behaviour

---

### Write:

* Mirror/Simplify → source-aligned
* Complement/Contrast → gap-oriented
* Intensify/Fill → expansion

---

## 5.4.2 Pairwise Divergence

### 📊 Figure 5.5 — Divergence Heatmap

Use:

* Simplify most distinct
* Fill/Intensify closest

---

### 🔥 Key insight

> structural clustering emerges naturally

---

## 5.4.3 Case-Level Examples

### 📊 Figure 5.4 — Skeleton Comparisons

Use:

* 2–3 curated cases

---

## 🔥 MUST INCLUDE INSIGHTS

### Insight 1

> identical metrics ≠ identical structure

### Insight 2

> Simplify = structural compression

### Insight 3

> behaviour clusters emerge

---

## 🧠 Section conclusion

> response types are structurally meaningful but context-dependent

---

# 🧠 5.5 Closed-Loop Execution, Timing, and Observability

### 🎯 Target: 350–450 words

---

## 🔥 Structure

---

## 5.5.1 Closed-Loop Behaviour

### 📊 Figure 5.6 — Debug View

Show:

* phases
* loop state

---

### Write:

* full loop exists
* system is observable

---

## 5.5.2 Timing Analysis

### 📊 Figure 5.9 — Latency Breakdown

Use F5:

* mean ≈ 2.6 ms
* max ≈ 66 ms 

---

### 🔥 CRITICAL LINE

> use **post-capture latency**, not total latency

---

## 5.5.3 Stage Breakdown

Highlight:

* generation dominates
* others negligible

---

## 🧠 Section conclusion

> system is real-time, generation is bottleneck

---

# 🧠 5.6 Response Realisation Gap and Technical Limitations

### 🎯 Target: 500–600 words (HIGHEST MARK SECTION)

---

## 🔥 Structure

---

## 5.6.1 Plan vs Output Fidelity

### 📊 Figure 5.8 — Plan vs Output

Use F4:

* density error: 0.048
* gap mismatch: 0.149
* anchor mismatch: 0.578 

---

## 🔥 Interpretation

* density ✔
* gap ⚠️
* anchors ❌

---

## 5.6.2 Response-Type Breakdown

* Mirror → strong alignment
* Complement → good
* Intensify / Fill → severe mismatch

---

## 🔥 CORE CLAIM

> planner intent ≠ realised output

---

## 5.6.3 Root Cause

Connect:

* FeatureTransformer
* generator not enforcing constraints

---

## 5.6.4 Broader Limitations

* no perceptual validation
* no tempo adaptation
* no expressive timing
* no user study

---

### 📊 Table 5.5 — Claim Strength

| Claim               | Status  |
| ------------------- | ------- |
| Planner works       | Strong  |
| Structure valid     | Strong  |
| Real-time           | Strong  |
| Output fidelity     | Partial |
| Interaction quality | Partial |

---

## 🧠 Section conclusion

> system stronger as architecture than final performer

---

# 🧠 5.7 Evaluation Synthesis

### 🎯 Target: 200–300 words

---

## Structure

### 1. Strong claims

* planner ✔
* structure ✔
* real-time ✔

---

### 2. Partial claims

* output fidelity ⚠️
* musical plausibility ⚠️

---

### 3. Deferred

* user studies
* ML realiser
* co-creative evaluation

---

## Final sentence (VERY important)

> IT4s successfully demonstrates an interpretable, closed-loop, real-time interaction architecture, while highlighting the challenge of translating structured planning into fully realised musical response.

---

# 🚀 Final Checklist (DO THIS)

Before writing:

### ✔ You have:

* all CSV outputs
* all figures ready
* curated case studies
* debug screenshots

---

### ✔ You MUST:

* lead with figures
* reference findings (F1–F5)
* avoid repetition
* avoid vague claims

---

# 🧠 Final advice (real talk)

If you follow this:

* Chapter 5 will feel **tight, technical, and confident**
* not essay-y
* not waffle
* not defensive

And most importantly:

> It will read like you **understand your system deeply**, not like you’re trying to impress someone.

---

If you want next:

👉 we can start writing **5.1 + 5.2 together (clean, fast, locked tone)**
👉 or jump straight into **5.3 (the heavy hitter)**
