# EndingAdjuster Specification
## Intelli-Trading 4s – Implementation Guide

---

## 1. Objective

Modify the final region of a PatternTurn to align with response intent.

---

## 2. Inputs

EndingInput:
- sourcePattern (bool[])
- currentPattern (bool[])
- ResponsePlan plan

---

## 3. Output

PatternTurn (modified)

---

## 4. Pipeline

adjust():
    window = getEndingWindow()
    mode = chooseMode(plan)
    return applyMode(mode, window)

---

## 5. Ending Window

Select final N steps:
- small fixed size based on turn length

---

## 6. Mode Selection

if plan.MirrorEnding:
    mode = Mirror
elif plan.ResponseType == FILL:
    mode = Fill
elif plan.ResponseType == SIMPLIFY:
    mode = Taper
elif plan.ResponseType == INTENSIFY:
    mode = Punch
elif plan.ResponseType in [COMPLEMENT, CONTRAST]:
    mode = Open
else:
    mode = None

---

## 7. Mode Behaviours

Mirror:
    copy source ending pattern into window

Open:
    reduce density
    avoid final cluster

Fill:
    increase density
    allow local clustering

Taper:
    remove late hits
    increase spacing

Punch:
    ensure one strong final hit

---

## 8. Constraints

- only modify within window
- enforce spacing rules
- limit density deviation
- skip if already valid

---

## 9. Performance

- constant time relative to pattern size
- no iteration loops
- no candidate evaluation

---

## 10. Structure

EndingAdjuster
    adjust()

EndingMode (enum)
    Mirror, Open, Fill, Taper, Punch, None

---

## 11. Principle

Detect → Decide → Adjust

---

## 12. Next Step

ConstraintPass
