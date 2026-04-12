# MotifTransformer Specification
## Intelli-Trading 4s – Implementation Guide

---

## 1. Objective

Modify PatternTurn using a transformed motif.

---

## 2. Inputs

- sourcePattern
- anchors
- currentPattern
- ResponsePlan

---

## 3. Output

Modified PatternTurn

---

## 4. Pipeline

transform():
    motif = extract()
    if not shouldApply():
        return currentPattern
    type = chooseTransform()
    return apply(type)

---

## 5. Extraction

- scan windows (2–6)
- score by anchors, density, position

---

## 6. Transform Types

- reuse
- shift
- thin
- gap-fill
- ending echo

---

## 7. Constraints

- local modification only
- enforce spacing
- avoid density spikes

---

## 8. Performance

- single pass
- no loops over candidates

---

## 9. Structure

MotifTransformer  
MotifExtractor  
MotifApplier  

---

## 10. Principle

Extract → Transform → Apply

---

## 11. Next Step

EndingAdjuster
