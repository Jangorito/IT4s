from pathlib import Path

import matplotlib
import pandas as pd

matplotlib.use("Agg")
import matplotlib.pyplot as plt


FINDINGS_DIR = Path(__file__).resolve().parent
SKELETON_STEP_COUNT = 96


def csv_path(*parts):
    path = FINDINGS_DIR.joinpath(*parts)
    if not path.exists():
        raise FileNotFoundError(f"Missing input CSV: {path}")
    return path


def save_current_figure(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    plt.tight_layout()
    plt.savefig(path, dpi=300)
    plt.close()
    print(f"Saved {path}")


def load_skeleton_case_metrics():
    df = pd.read_csv(csv_path("f2", "skeleton_case_metrics.csv"))
    df["Density"] = df["SelectedStepCount"] / SKELETON_STEP_COUNT
    return df


def plot_response_type_distribution():
    data_dir = FINDINGS_DIR / "f1"
    df = pd.read_csv(csv_path("f1", "planner_distribution.csv"))

    plt.figure()
    plt.bar(df["ResponseType"], df["Count"])
    plt.xlabel("Response Type")
    plt.ylabel("Count")
    plt.title("Planner Response Type Distribution")
    plt.xticks(rotation=30, ha="right")
    save_current_figure(data_dir / "fig_planner_distribution.png")


def plot_margin_distribution():
    data_dir = FINDINGS_DIR / "f1"
    df = pd.read_csv(csv_path("f1", "planner_margins.csv"))

    plt.figure()
    plt.hist(df["Margin"], bins=20)
    plt.xlabel("Decision Margin")
    plt.ylabel("Frequency")
    plt.title("Planner Decision Margin Distribution")
    save_current_figure(data_dir / "fig_planner_margins.png")


def plot_skeleton_divergence():
    data_dir = FINDINGS_DIR / "f2"
    df = pd.read_csv(csv_path("f2", "skeleton_pairwise_divergence.csv"))

    plt.figure()
    plt.hist(df["JaccardDistance"], bins=20)
    plt.xlabel("Structural Divergence")
    plt.ylabel("Frequency")
    plt.title("Distribution of Structural Divergence Between Responses")
    save_current_figure(data_dir / "fig_skeleton_divergence.png")


def plot_density_by_response():
    data_dir = FINDINGS_DIR / "f2"
    df = load_skeleton_case_metrics()
    summary = (
        df.groupby("ResponseType", as_index=False)["Density"]
        .mean()
        .rename(columns={"Density": "MeanDensity"})
    )

    plt.figure()
    plt.bar(summary["ResponseType"], summary["MeanDensity"])
    plt.xlabel("Response Type")
    plt.ylabel("Mean Density")
    plt.title("Mean Density by Response Type")
    plt.xticks(rotation=30, ha="right")
    save_current_figure(data_dir / "fig_density_by_response.png")


def plot_divergence_by_response_pair():
    data_dir = FINDINGS_DIR / "f2"
    df = pd.read_csv(csv_path("f2", "skeleton_pairwise_divergence.csv"))
    df["ResponsePair"] = df["ResponseTypeA"] + " vs " + df["ResponseTypeB"]
    summary = (
        df.groupby("ResponsePair", as_index=False)["JaccardDistance"]
        .mean()
        .rename(columns={"JaccardDistance": "MeanDivergence"})
    )

    plt.figure(figsize=(10, 5))
    plt.bar(summary["ResponsePair"], summary["MeanDivergence"])
    plt.xlabel("Response Type Pair")
    plt.ylabel("Mean Structural Divergence")
    plt.title("Mean Structural Divergence by Response Type Pair")
    plt.xticks(rotation=40, ha="right")
    save_current_figure(data_dir / "fig_divergence_by_response_pair.png")


def save_skeleton_metrics_table():
    data_dir = FINDINGS_DIR / "f2"
    df = load_skeleton_case_metrics()
    summary = (
        df.groupby("ResponseType")
        .agg(
            MeanDensity=("Density", "mean"),
            StdDensity=("Density", "std"),
            MeanActiveSteps=("SelectedStepCount", "mean"),
            AnchorPreservation=("AnchorPreservation", "mean"),
            MeanOverlap=("Overlap", "mean"),
            MeanGapFill=("GapFill", "mean"),
            MeanDivergence=("MeanPairwiseJaccardDistance", "mean"),
            CaseCount=("CaseID", "count"),
        )
        .reset_index()
    )

    summary.to_csv(data_dir / "skeleton_metrics_summary_table.csv", index=False)

    display = summary.rename(
        columns={
            "ResponseType": "Response",
            "MeanDensity": "Density",
            "StdDensity": "Std Density",
            "MeanActiveSteps": "Active Steps",
            "AnchorPreservation": "Anchors",
            "MeanOverlap": "Overlap",
            "MeanGapFill": "Gap-Fill",
            "MeanDivergence": "Divergence",
            "CaseCount": "Cases",
        }
    )
    numeric_columns = [column for column in display.columns if column != "Response"]
    display[numeric_columns] = display[numeric_columns].round(3)
    display["Cases"] = display["Cases"].astype(int)

    fig, ax = plt.subplots(figsize=(12, 3.4))
    ax.axis("off")
    table = ax.table(
        cellText=display.values,
        colLabels=display.columns,
        loc="center",
        cellLoc="center",
    )
    table.auto_set_font_size(False)
    table.set_fontsize(9)
    table.scale(1, 1.6)

    for (row, column), cell in table.get_celld().items():
        cell.set_linewidth(0.4)
        if row == 0:
            cell.set_facecolor("#e8eef7")
            cell.set_text_props(weight="bold")
        elif row % 2 == 0:
            cell.set_facecolor("#f7f7f7")

    output_path = data_dir / "fig_skeleton_metrics_summary_table.png"
    plt.savefig(output_path, dpi=600, bbox_inches="tight", pad_inches=0.05)
    plt.close()
    print(f"Saved {output_path}")


def plot_variant_sensitivity():
    data_dir = FINDINGS_DIR / "f3"
    df = pd.read_csv(csv_path("f3", "planner_variant_summary.csv"))
    df = df[df["Section"] == "Grouped"].copy()

    group_order = [
        ("BaseStructureType", "Structure variants"),
        ("BaseEnergyProfile", "Energy variants"),
        ("BaseSelectedResponseType", "Response-type variants"),
    ]

    frames = []
    group_centres = []
    separator_positions = []
    start = 0
    for field, label in group_order:
        group = df[df["GroupField"] == field].copy()
        if group.empty:
            continue
        group = group.sort_values("GroupValue")
        group["GroupLabel"] = label
        frames.append(group)
        end = start + len(group) - 1
        group_centres.append(((start + end) / 2, label))
        separator_positions.append(end + 0.5)
        start = end + 1

    df = pd.concat(frames, ignore_index=True)
    x_positions = range(len(df))

    fig, ax = plt.subplots(figsize=(9, 5))
    ax.bar(x_positions, df["PercentageChanged"])
    plt.ylabel("Changed Pairs (%)")
    plt.title("Planner Sensitivity to Input Variation")
    ax.set_xticks(list(x_positions))
    ax.set_xticklabels(df["GroupValue"], rotation=35, ha="right")

    for position in separator_positions[:-1]:
        ax.axvline(position, color="0.75", linewidth=0.8)

    for centre, label in group_centres:
        ax.text(
            centre,
            -0.28,
            label,
            ha="center",
            va="top",
            transform=ax.get_xaxis_transform(),
            fontsize=9,
        )

    fig.subplots_adjust(bottom=0.32)
    save_current_figure(data_dir / "fig_variant_sensitivity.png")


def plot_plan_output_drift():
    data_dir = FINDINGS_DIR / "f4"
    df = pd.read_csv(csv_path("f4", "plan_vs_output_summary.csv"))
    df = df[df["Section"] == "ResponseType"].copy()

    plt.figure()
    plt.bar(df["GroupValue"], df["MeanDensityError"])
    plt.xlabel("Response Type")
    plt.ylabel("Mean Density Error")
    plt.title("Plan vs Output Density Error")
    plt.xticks(rotation=30, ha="right")
    save_current_figure(data_dir / "fig_plan_output_drift.png")

    


def main():
    plot_response_type_distribution()
    plot_margin_distribution()
    plot_skeleton_divergence()
    plot_density_by_response()
    plot_divergence_by_response_pair()
    save_skeleton_metrics_table()
    plot_variant_sensitivity()
    plot_plan_output_drift()


if __name__ == "__main__":
    main()
