# ResponsePlanner tuning report

## Summary
{
  "row_count": 300,
  "base_rows": 150,
  "variant_rows": 150,
  "selected_matches_winner_rate": 0.93,
  "mean_top_margin": 0.6404,
  "median_top_margin": 0.5,
  "pct_margin_lt_0_5": 0.2933,
  "pct_margin_lt_1_0": 0.79,
  "variant_change_rate": 0.2333,
  "response_distribution": {
    "Intensify": 118,
    "Mirror": 62,
    "Fill": 62,
    "Complement": 36,
    "Contrast": 22
  }
}

## Heuristic suggestions
- Selection mismatch detected in 7.0% of rows. First check planner/export logic before changing thresholds: some rows do not select the numerically highest-scoring response.
- 29.3% of rows have a top-score margin below 0.5. These are borderline decisions. Prefer tuning here first rather than retuning the whole planner.
- Base→variant response changes occur in 23.3% of pairs. Treat this as moderate sensitivity: good for responsiveness, but inspect whether changes happen near intended thresholds.
- Intensify is being chosen on average for patterns at least as sparse as Fill. That may be correct if Intensify is driven by anchor/gesture reinforcement rather than raw density, but if your musical intent is 'denser reply to denser input', review the Intensify density contribution or Fill bonuses.
- Response changes are much more associated with anchor-count changes than stable pairs. Anchor thresholds or anchor-weight terms are likely strong tuning levers.

## Mean feature profile by selected response
| SelectedResponseType   |   Density |   MeanEnergy |   AnchorCount |   EndDensity |   EndEnergy |   TopMargin |
|:-----------------------|----------:|-------------:|--------------:|-------------:|------------:|------------:|
| Complement             |     0.416 |       86.821 |         1.75  |        0.385 |      95.117 |       0.944 |
| Contrast               |     0.373 |       90.29  |         0.955 |        0.386 |     107.325 |       0.688 |
| Fill                   |     0.226 |       89.211 |         1.71  |        0.255 |     100.791 |       0.452 |
| Intensify              |     0.2   |       88.303 |         2.297 |        0.208 |      98.673 |       0.688 |
| Mirror                 |     0.331 |       87.936 |         2.919 |        0.335 |     102.297 |       0.546 |

## Base → variant transitions
| SelectedResponseType_base   | SelectedResponseType_var   |   count |
|:----------------------------|:---------------------------|--------:|
| Intensify                   | Intensify                  |      46 |
| Mirror                      | Mirror                     |      24 |
| Fill                        | Fill                       |      22 |
| Complement                  | Complement                 |      14 |
| Fill                        | Intensify                  |      14 |
| Contrast                    | Contrast                   |       9 |
| Mirror                      | Intensify                  |       6 |
| Mirror                      | Complement                 |       4 |
| Intensify                   | Fill                       |       3 |
| Intensify                   | Mirror                     |       2 |
| Complement                  | Mirror                     |       2 |
| Fill                        | Contrast                   |       1 |
| Contrast                    | Complement                 |       1 |
| Complement                  | Contrast                   |       1 |
| Intensify                   | Contrast                   |       1 |

## Most borderline cases
| CaseId                      | SelectedResponseType   | WinningTypes             |   TopMargin |   Score_Mirror |   Score_Complement |   Score_Simplify |   Score_Intensify |   Score_Contrast |   Score_Fill |
|:----------------------------|:-----------------------|:-------------------------|------------:|---------------:|-------------------:|-----------------:|------------------:|-----------------:|-------------:|
| Base_H36_Clustered_Flat     | Mirror                 | ['Mirror', 'Complement'] |        0    |          5.25  |              5.25  |            0     |             2.125 |            -0.25 |        0.625 |
| Base_H40_Clustered_Flat     | Mirror                 | ['Mirror', 'Complement'] |        0    |          5.25  |              5.25  |            0     |             2.125 |            -0.25 |        0.625 |
| Variant_H40_Clustered_Flat  | Mirror                 | ['Mirror', 'Complement'] |        0    |          5.25  |              5.25  |            0     |             2.125 |            -0.25 |        0.625 |
| Base_H44_Clustered_Flat     | Mirror                 | ['Mirror', 'Complement'] |        0    |          5.25  |              5.25  |            0     |             2.125 |            -0.25 |        0.625 |
| Base_H16_Clustered_Flat     | Mirror                 | ['Mirror', 'Intensify']  |        0    |          4     |              1.75  |           -2.5   |             4     |            -0.25 |        1.25  |
| Base_H20_Clustered_Flat     | Mirror                 | ['Mirror', 'Intensify']  |        0    |          4     |              1.75  |           -2.5   |             4     |            -0.25 |        1.25  |
| Variant_H20_Clustered_Flat  | Mirror                 | ['Mirror', 'Intensify']  |        0    |          4     |              1.75  |           -2.5   |             4     |            -0.25 |        1.25  |
| Base_H08_Even_Flat          | Intensify              | ['Intensify']            |        0.25 |          3.75  |              3.875 |           -2.5   |             4.125 |            -0.25 |        3.125 |
| Variant_H08_Even_Flat       | Intensify              | ['Intensify']            |        0.25 |          3.75  |              3.875 |           -2.5   |             4.125 |            -0.25 |        3.125 |
| Base_H08_BackLoaded_Flat    | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Variant_H08_BackLoaded_Flat | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Base_H12_Even_Flat          | Intensify              | ['Intensify']            |        0.25 |          3.75  |              3.875 |           -2.5   |             4.125 |            -0.25 |        3.125 |
| Base_H12_BackLoaded_Flat    | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Variant_H12_BackLoaded_Flat | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Base_H12_AnchorLed_Flat     | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Variant_H12_AnchorLed_Flat  | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Base_H16_Even_Flat          | Intensify              | ['Intensify']            |        0.25 |          3.75  |              3.875 |           -2.5   |             4.125 |            -0.25 |        3.125 |
| Base_H16_BackLoaded_Flat    | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Base_H16_AnchorLed_Flat     | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Variant_H16_AnchorLed_Flat  | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Base_H20_Even_Flat          | Intensify              | ['Intensify']            |        0.25 |          3.75  |              3.875 |           -2.5   |             4.125 |            -0.25 |        3.125 |
| Base_H20_BackLoaded_Flat    | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Base_H20_AnchorLed_Flat     | Fill                   | ['Intensify']            |        0.25 |          2.625 |              3.125 |           -2.125 |             4.125 |             0.75 |        3.875 |
| Variant_H20_AnchorLed_Flat  | Intensify              | ['Intensify']            |        0.25 |          3.75  |              3.875 |           -2.5   |             4.125 |            -0.25 |        3.125 |
| Base_H24_Even_Flat          | Intensify              | ['Intensify']            |        0.25 |          3.75  |              3.875 |           -2.5   |             4.125 |            -0.25 |        3.125 |
