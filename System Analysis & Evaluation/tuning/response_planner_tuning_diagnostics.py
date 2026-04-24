from __future__ import annotations

import argparse
from pathlib import Path
import json
import math

import matplotlib.pyplot as plt
import pandas as pd

SCORE_COLS = [
    "Score_Mirror",
    "Score_Complement",
    "Score_Simplify",
    "Score_Intensify",
    "Score_Contrast",
    "Score_Fill",
]


def add_diagnostics(df: pd.DataFrame) -> pd.DataFrame:
    df = df.copy()

    def winning_types(row: pd.Series) -> list[str]:
        mx = row[SCORE_COLS].max()
        return [c.replace("Score_", "") for c in SCORE_COLS if row[c] == mx]

    def top_margin(row: pd.Series) -> float:
        vals = sorted([float(row[c]) for c in SCORE_COLS], reverse=True)
        return vals[0] - vals[1]

    df["WinningScore"] = df[SCORE_COLS].max(axis=1)
    df["WinningTypes"] = df.apply(winning_types, axis=1)
    df["SelectedMatchesWinner"] = df.apply(
        lambda r: r["SelectedResponseType"] in r["WinningTypes"], axis=1
    )
    df["TopMargin"] = df.apply(top_margin, axis=1)
    df["LowConfidence"] = df["TopMargin"] < 0.5
    return df


def build_pairwise(base_df: pd.DataFrame, variant_df: pd.DataFrame) -> pd.DataFrame:
    merged = base_df.merge(
        variant_df,
        left_on="CaseId",
        right_on="ParentCaseId",
        suffixes=("_base", "_var"),
    )
    for col in [
        "Density",
        "MeanEnergy",
        "PeakEnergy",
        "AnchorCount",
        "EndDensity",
        "EndEnergy",
        "EndAccent",
        "TopMargin",
    ]:
        merged[f"delta_{col}"] = merged[f"{col}_var"] - merged[f"{col}_base"]

    merged["ResponseChanged"] = (
        merged["SelectedResponseType_base"] != merged["SelectedResponseType_var"]
    )
    return merged


def heuristic_suggestions(df: pd.DataFrame, pair_df: pd.DataFrame) -> list[str]:
    suggestions: list[str] = []

    mismatch_rate = 1.0 - df["SelectedMatchesWinner"].mean()
    low_margin_rate = (df["TopMargin"] < 0.5).mean()
    change_rate = pair_df["ResponseChanged"].mean()

    if mismatch_rate > 0.0:
        suggestions.append(
            f"Selection mismatch detected in {mismatch_rate:.1%} of rows. "
            "First check planner/export logic before changing thresholds: some rows do not select the numerically highest-scoring response."
        )

    if low_margin_rate > 0.20:
        suggestions.append(
            f"{low_margin_rate:.1%} of rows have a top-score margin below 0.5. "
            "These are borderline decisions. Prefer tuning here first rather than retuning the whole planner."
        )

    if change_rate > 0.20:
        suggestions.append(
            f"Base→variant response changes occur in {change_rate:.1%} of pairs. "
            "Treat this as moderate sensitivity: good for responsiveness, but inspect whether changes happen near intended thresholds."
        )

    # Response-type-specific signals.
    response_means = (
        df.groupby("SelectedResponseType")[["Density", "MeanEnergy", "AnchorCount", "EndDensity", "EndEnergy"]]
        .mean()
        .sort_index()
    )

    if "Intensify" in response_means.index and "Fill" in response_means.index:
        intensify_density = response_means.loc["Intensify", "Density"]
        fill_density = response_means.loc["Fill", "Density"]
        if intensify_density <= fill_density:
            suggestions.append(
                "Intensify is being chosen on average for patterns at least as sparse as Fill. "
                "That may be correct if Intensify is driven by anchor/gesture reinforcement rather than raw density, "
                "but if your musical intent is 'denser reply to denser input', review the Intensify density contribution or Fill bonuses."
            )

    changed = pair_df[pair_df["ResponseChanged"]]
    unchanged = pair_df[~pair_df["ResponseChanged"]]
    if not changed.empty and not unchanged.empty:
        anchor_changed = changed["delta_AnchorCount"].abs().mean()
        anchor_unchanged = unchanged["delta_AnchorCount"].abs().mean()
        if anchor_changed > anchor_unchanged * 1.75:
            suggestions.append(
                "Response changes are much more associated with anchor-count changes than stable pairs. "
                "Anchor thresholds or anchor-weight terms are likely strong tuning levers."
            )

    return suggestions


def write_report(df: pd.DataFrame, pair_df: pd.DataFrame, output_dir: Path) -> Path:
    output_dir.mkdir(parents=True, exist_ok=True)

    transition_counts = (
        pair_df.groupby(["SelectedResponseType_base", "SelectedResponseType_var"])
        .size()
        .reset_index(name="count")
        .sort_values("count", ascending=False)
    )

    response_means = (
        df.groupby("SelectedResponseType")[["Density", "MeanEnergy", "AnchorCount", "EndDensity", "EndEnergy", "TopMargin"]]
        .mean()
        .round(3)
        .sort_index()
    )

    borderline = (
        df.sort_values(["TopMargin", "WinningScore"], ascending=[True, False])[
            ["CaseId", "SelectedResponseType", "WinningTypes", "TopMargin"] + SCORE_COLS
        ]
        .head(25)
    )

    summary = {
        "row_count": int(len(df)),
        "base_rows": int((df["VariantKind"] == "Base").sum()),
        "variant_rows": int((df["VariantKind"] == "Variant").sum()),
        "selected_matches_winner_rate": round(float(df["SelectedMatchesWinner"].mean()), 4),
        "mean_top_margin": round(float(df["TopMargin"].mean()), 4),
        "median_top_margin": round(float(df["TopMargin"].median()), 4),
        "pct_margin_lt_0_5": round(float((df["TopMargin"] < 0.5).mean()), 4),
        "pct_margin_lt_1_0": round(float((df["TopMargin"] < 1.0).mean()), 4),
        "variant_change_rate": round(float(pair_df["ResponseChanged"].mean()), 4),
        "response_distribution": df["SelectedResponseType"].value_counts().to_dict(),
    }

    report_lines = [
        "# ResponsePlanner tuning report",
        "",
        "## Summary",
        json.dumps(summary, indent=2),
        "",
        "## Heuristic suggestions",
    ]
    for s in heuristic_suggestions(df, pair_df):
        report_lines.append(f"- {s}")

    report_lines += [
        "",
        "## Mean feature profile by selected response",
        response_means.to_markdown(),
        "",
        "## Base → variant transitions",
        transition_counts.to_markdown(index=False),
        "",
        "## Most borderline cases",
        borderline.to_markdown(index=False),
        "",
    ]

    report_path = output_dir / "tuning_report.md"
    report_path.write_text("\n".join(report_lines), encoding="utf-8")
    return report_path


def save_plots(df: pd.DataFrame, pair_df: pd.DataFrame, output_dir: Path) -> None:
    # 1. Response distribution
    ax = df["SelectedResponseType"].value_counts().sort_values(ascending=False).plot(kind="bar")
    ax.set_title("Selected response distribution")
    ax.set_xlabel("Response type")
    ax.set_ylabel("Count")
    plt.tight_layout()
    plt.savefig(output_dir / "response_distribution.png", dpi=160)
    plt.close()

    # 2. Top margin histogram
    ax = df["TopMargin"].plot(kind="hist", bins=20)
    ax.set_title("Top-score margin distribution")
    ax.set_xlabel("Winning score margin")
    ax.set_ylabel("Count")
    plt.tight_layout()
    plt.savefig(output_dir / "top_margin_hist.png", dpi=160)
    plt.close()

    # 3. Density vs energy scatter
    for response_type, group in df.groupby("SelectedResponseType"):
        plt.scatter(group["Density"], group["MeanEnergy"], label=response_type, alpha=0.7)
    plt.title("Density vs mean energy by selected response")
    plt.xlabel("Density")
    plt.ylabel("MeanEnergy")
    plt.legend()
    plt.tight_layout()
    plt.savefig(output_dir / "density_energy_scatter.png", dpi=160)
    plt.close()

    # 4. Base->variant response change rates by base response
    change_rate_by_base = (
        pair_df.groupby("SelectedResponseType_base")["ResponseChanged"].mean().sort_values(ascending=False)
    )
    ax = change_rate_by_base.plot(kind="bar")
    ax.set_title("Base→variant change rate by base response")
    ax.set_xlabel("Base response type")
    ax.set_ylabel("Change rate")
    plt.tight_layout()
    plt.savefig(output_dir / "change_rate_by_base_response.png", dpi=160)
    plt.close()


def main() -> None:
    parser = argparse.ArgumentParser(description="Semi-automatic ResponsePlanner tuning diagnostics.")
    parser.add_argument("csv_path", type=Path, help="Path to planner analysis CSV")
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path("tuning_output"),
        help="Directory for markdown report and plots",
    )
    args = parser.parse_args()

    df = pd.read_csv(args.csv_path)
    df = add_diagnostics(df)

    base_df = df[df["VariantKind"] == "Base"].copy()
    variant_df = df[df["VariantKind"] == "Variant"].copy()
    pair_df = build_pairwise(base_df, variant_df)

    args.output_dir.mkdir(parents=True, exist_ok=True)
    report_path = write_report(df, pair_df, args.output_dir)
    save_plots(df, pair_df, args.output_dir)

    print(f"Wrote tuning report: {report_path}")
    print(f"Wrote plots to: {args.output_dir.resolve()}")


if __name__ == "__main__":
    main()
