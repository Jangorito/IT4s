import pandas as pd
import numpy as np
import argparse
import os

FEATURES = [
    "Density",
    "MeanEnergy",
    "PeakEnergy",
    "AnchorCount",
    "EndDensity",
    "EndEnergy",
]

SCORE_COLS = [
    "Score_Mirror",
    "Score_Complement",
    "Score_Simplify",
    "Score_Intensify",
    "Score_Contrast",
    "Score_Fill",
]

def compute_margin(df):
    scores = df[SCORE_COLS].values
    sorted_scores = np.sort(scores, axis=1)
    return sorted_scores[:, -1] - sorted_scores[:, -2]

def find_boundaries(df):
    df["TopMargin"] = compute_margin(df)
    return df[df["TopMargin"] < 0.25]  # tighter than before

def analyse_pair(df, a, b):
    subset = df[df["SelectedResponseType"].isin([a, b])]
    
    if len(subset) < 10:
        return None
    
    group_a = subset[subset["SelectedResponseType"] == a]
    group_b = subset[subset["SelectedResponseType"] == b]
    
    diffs = {}
    
    for f in FEATURES:
        diffs[f] = group_a[f].mean() - group_b[f].mean()
    
    return diffs

def interpret_diffs(pair, diffs):
    a, b = pair
    suggestions = []
    
    for f, d in diffs.items():
        if abs(d) < 0.02:
            continue
        
        if d > 0:
            suggestions.append(
                f"{a} tends to have higher {f} than {b} → increase weight of {f} for {a} or decrease for {b}"
            )
        else:
            suggestions.append(
                f"{b} tends to have higher {f} than {a} → increase weight of {f} for {b} or decrease for {a}"
            )
    
    return suggestions

def generate_suggestions(df):
    responses = df["SelectedResponseType"].unique()
    
    report = []
    
    report.append("# Parameter Tuning Suggestions\n")
    
    boundary_df = find_boundaries(df)
    
    report.append(f"## Boundary Cases Analysed: {len(boundary_df)}\n")
    
    for i, a in enumerate(responses):
        for b in responses[i+1:]:
            
            diffs = analyse_pair(boundary_df, a, b)
            if diffs is None:
                continue
            
            suggestions = interpret_diffs((a, b), diffs)
            
            if not suggestions:
                continue
            
            report.append(f"\n## {a} vs {b}\n")
            
            for f, d in diffs.items():
                report.append(f"- {f}: {d:.3f}")
            
            report.append("\n### Suggested Adjustments\n")
            
            for s in suggestions:
                report.append(f"- {s}")
    
    return "\n".join(report)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("csv_path")
    parser.add_argument("--output", default="tuning_suggestions.md")
    args = parser.parse_args()
    
    df = pd.read_csv(args.csv_path)
    
    report = generate_suggestions(df)
    
    with open(args.output, "w") as f:
        f.write(report)
    
    print(f"Saved suggestions → {args.output}")

if __name__ == "__main__":
    main()