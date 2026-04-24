#!/usr/bin/env python3
"""
Align planner intent with generated skeleton metrics for Chapter 5.

Produces:
  - plan_vs_output_analysis.csv
  - plan_vs_output_summary.csv

The current planner CSV contains one selected response per case. The skeleton
metrics CSV contains one row per case per response type. Alignment is therefore
performed on:
  planner.CaseId == skeleton.CaseID
  planner.SelectedResponseType == skeleton.ResponseType
"""

from __future__ import annotations

import argparse
import csv
import sys
from collections import defaultdict
from pathlib import Path
from statistics import mean
from typing import Iterable


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PLANNER_SOURCE = (
    REPO_ROOT / "ModelAnalysis" / "synthetic_turn_analysis_response_planner_base_plus_variants.csv"
)
DEFAULT_SKELETON_SOURCE = (
    REPO_ROOT / "Evaluation" / "chapter5_skeleton_outputs" / "skeleton_case_metrics.csv"
)
DEFAULT_OUTPUT_DIR = REPO_ROOT / "Evaluation" / "chapter5_plan_vs_output_outputs"

PLANNER_CASE_ID_CANDIDATES = ("CaseId", "CaseID", "SourceId", "SourceID", "Id", "ID")
SELECTED_RESPONSE_CANDIDATES = (
    "SelectedResponseType",
    "SelectedResponse",
    "PlannerSelectedResponseType",
    "SnapshotSelectedResponseType",
)
SKELETON_CASE_ID_CANDIDATES = ("CaseID", "CaseId", "SourceId", "SourceID", "Id", "ID")
SKELETON_RESPONSE_CANDIDATES = ("ResponseType", "SelectedResponseType", "Response")


RESPONSE_INTENT_FALLBACKS = {
    # These are only used if explicit planner columns are unavailable.
    "Mirror": {"density_multiplier": 1.0, "preserve_anchors": True, "gap_bias": 0.25},
    "Complement": {"density_multiplier": 1.2, "preserve_anchors": False, "gap_bias": 0.85},
    "Simplify": {"density_multiplier": 0.6, "preserve_anchors": True, "gap_bias": 0.25},
    "Intensify": {"density_multiplier": 1.6, "preserve_anchors": True, "gap_bias": 0.5},
    "Contrast": {"density_multiplier": 1.2, "preserve_anchors": False, "gap_bias": 0.9},
    "Fill": {"density_multiplier": 1.8, "preserve_anchors": True, "gap_bias": 0.75},
}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Analyse whether generated skeleton outputs reflect planner intent."
    )
    parser.add_argument(
        "--planner",
        type=Path,
        default=DEFAULT_PLANNER_SOURCE,
        help=f"Planner CSV. Default: {DEFAULT_PLANNER_SOURCE}",
    )
    parser.add_argument(
        "--skeleton-metrics",
        type=Path,
        default=DEFAULT_SKELETON_SOURCE,
        help=f"Skeleton case metrics CSV. Default: {DEFAULT_SKELETON_SOURCE}",
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help=f"Directory for generated CSVs. Default: {DEFAULT_OUTPUT_DIR}",
    )
    return parser.parse_args()


def read_rows(path: Path) -> tuple[list[str], list[dict[str, str]]]:
    if not path.exists():
        raise FileNotFoundError(f"CSV does not exist: {path}")

    with path.open("r", newline="", encoding="utf-8-sig") as handle:
        reader = csv.DictReader(handle)
        if not reader.fieldnames:
            raise ValueError(f"CSV has no header row: {path}")
        rows = list(reader)

    if not rows:
        raise ValueError(f"CSV has no data rows: {path}")

    return list(reader.fieldnames), rows


def first_existing(fieldnames: Iterable[str], candidates: Iterable[str]) -> str | None:
    fields = set(fieldnames)
    for candidate in candidates:
        if candidate in fields:
            return candidate
    return None


def require_column(
    fieldnames: list[str], candidates: Iterable[str], description: str
) -> str:
    column = first_existing(fieldnames, candidates)
    if column:
        return column
    raise ValueError(
        f"Could not detect {description}. Looked for: {', '.join(candidates)}"
    )


def parse_float(row: dict[str, str], column: str, context: str) -> float:
    value = (row.get(column) or "").strip()
    try:
        return float(value)
    except ValueError as exc:
        raise ValueError(f"Expected numeric {column} for {context}, got {value!r}") from exc


def parse_bool(value: str | None) -> bool | None:
    text = (value or "").strip().lower()
    if text in {"true", "1", "yes", "y"}:
        return True
    if text in {"false", "0", "no", "n"}:
        return False
    return None


def infer_target_density(planner_row: dict[str, str], response_type: str) -> float:
    if "TargetDensity" in planner_row and (planner_row.get("TargetDensity") or "").strip():
        return parse_float(planner_row, "TargetDensity", planner_row.get("CaseId", "case"))

    source_density = None
    for column in ("PlannerSourceDensity", "Density"):
        if column in planner_row and (planner_row.get(column) or "").strip():
            source_density = parse_float(planner_row, column, planner_row.get("CaseId", "case"))
            break

    fallback = RESPONSE_INTENT_FALLBACKS.get(response_type)
    if fallback and source_density is not None:
        return source_density * float(fallback["density_multiplier"])

    raise ValueError(
        f"Cannot infer target density for response type {response_type!r}; "
        "TargetDensity and usable source density are unavailable."
    )


def infer_preserve_anchors(planner_row: dict[str, str], response_type: str) -> bool:
    if "PreserveAnchors" in planner_row:
        parsed = parse_bool(planner_row.get("PreserveAnchors"))
        if parsed is not None:
            return parsed

    fallback = RESPONSE_INTENT_FALLBACKS.get(response_type)
    if fallback:
        return bool(fallback["preserve_anchors"])

    raise ValueError(
        f"Cannot infer anchor-preservation intent for response type {response_type!r}."
    )


def infer_complementarity_bias(planner_row: dict[str, str], response_type: str) -> float:
    if "ComplementarityBias" in planner_row and (
        planner_row.get("ComplementarityBias") or ""
    ).strip():
        return parse_float(
            planner_row, "ComplementarityBias", planner_row.get("CaseId", "case")
        )

    fallback = RESPONSE_INTENT_FALLBACKS.get(response_type)
    if fallback:
        return float(fallback["gap_bias"])

    raise ValueError(
        f"Cannot infer complementarity/gap intent for response type {response_type!r}."
    )


def build_skeleton_index(
    skeleton_rows: list[dict[str, str]],
    case_column: str,
    response_column: str,
) -> dict[tuple[str, str], dict[str, str]]:
    index = {}
    for row in skeleton_rows:
        case_id = (row.get(case_column) or "").strip()
        response_type = (row.get(response_column) or "").strip()
        if not case_id or not response_type:
            raise ValueError("Skeleton metrics contain blank case id or response type.")

        key = (case_id, response_type)
        if key in index:
            raise ValueError(
                f"Duplicate skeleton metric row for CaseID={case_id}, ResponseType={response_type}"
            )
        index[key] = row
    return index


def actual_density(
    planner_row: dict[str, str],
    skeleton_row: dict[str, str],
    case_id: str,
) -> tuple[float, str]:
    selected_count = parse_float(skeleton_row, "SelectedStepCount", case_id)

    if "StepCount" in planner_row and (planner_row.get("StepCount") or "").strip():
        step_count = parse_float(planner_row, "StepCount", case_id)
        return selected_count / step_count if step_count else 0.0, "SelectedStepCount/StepCount"

    source_count = parse_float(skeleton_row, "SourceStepCount", case_id)
    return (
        selected_count / source_count if source_count else 0.0,
        "SelectedStepCount/SourceStepCount",
    )


def build_analysis(
    planner_rows: list[dict[str, str]],
    skeleton_index: dict[tuple[str, str], dict[str, str]],
    planner_case_column: str,
    selected_response_column: str,
) -> tuple[list[dict[str, str]], dict[str, int], str]:
    analysis_rows = []
    missing_alignment_count = 0
    density_methods = set()

    for planner_row in planner_rows:
        case_id = (planner_row.get(planner_case_column) or "").strip()
        response_type = (planner_row.get(selected_response_column) or "").strip()
        if not case_id or not response_type:
            raise ValueError("Planner CSV contains blank case id or selected response type.")

        skeleton_row = skeleton_index.get((case_id, response_type))
        if skeleton_row is None:
            missing_alignment_count += 1
            continue

        target_density = infer_target_density(planner_row, response_type)
        actual_density_value, density_method = actual_density(planner_row, skeleton_row, case_id)
        density_methods.add(density_method)
        density_error = abs(actual_density_value - target_density)

        preserve_anchors = infer_preserve_anchors(planner_row, response_type)
        target_anchor_preservation = 1.0 if preserve_anchors else 0.0
        actual_anchor_preservation = parse_float(
            skeleton_row, "AnchorPreservation", case_id
        )
        anchor_mismatch = abs(actual_anchor_preservation - target_anchor_preservation)

        complementarity_bias = infer_complementarity_bias(planner_row, response_type)
        actual_gap_fill = parse_float(skeleton_row, "GapFill", case_id)
        gap_mismatch = abs(actual_gap_fill - complementarity_bias)

        output = {
            "CaseID": case_id,
            "ResponseType": response_type,
            "TargetDensity": f"{target_density:.6g}",
            "ActualDensity": f"{actual_density_value:.6g}",
            "DensityError": f"{density_error:.6g}",
            "PreserveAnchors": str(preserve_anchors),
            "ActualAnchorPreservation": f"{actual_anchor_preservation:.6g}",
            "AnchorMismatch": f"{anchor_mismatch:.6g}",
            "ComplementarityBias": f"{complementarity_bias:.6g}",
            "ActualGapFill": f"{actual_gap_fill:.6g}",
            "GapMismatch": f"{gap_mismatch:.6g}",
        }

        for optional in ("StructureType", "EnergyProfile", "VariantKind"):
            if optional in planner_row:
                output[optional] = (planner_row.get(optional) or "").strip()

        analysis_rows.append(output)

    analysis_rows.sort(key=lambda row: row["CaseID"])
    diagnostics = {
        "planner_rows": len(planner_rows),
        "aligned_rows": len(analysis_rows),
        "missing_alignment_rows": missing_alignment_count,
    }
    return analysis_rows, diagnostics, ", ".join(sorted(density_methods))


def build_summary(analysis_rows: list[dict[str, str]]) -> list[dict[str, str]]:
    summary_rows = []
    add_summary_row(summary_rows, "Overall", "All", "All", analysis_rows)

    grouped: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in analysis_rows:
        grouped[row["ResponseType"]].append(row)

    for response_type in sorted(grouped):
        add_summary_row(
            summary_rows,
            "ResponseType",
            "ResponseType",
            response_type,
            grouped[response_type],
        )

    return summary_rows


def add_summary_row(
    output_rows: list[dict[str, str]],
    section: str,
    group_field: str,
    group_value: str,
    rows: list[dict[str, str]],
) -> None:
    output_rows.append(
        {
            "Section": section,
            "GroupField": group_field,
            "GroupValue": group_value,
            "CaseCount": str(len(rows)),
            "MeanDensityError": f"{mean(float(row['DensityError']) for row in rows):.6g}",
            "MeanAnchorMismatch": f"{mean(float(row['AnchorMismatch']) for row in rows):.6g}",
            "MeanGapMismatch": f"{mean(float(row['GapMismatch']) for row in rows):.6g}",
        }
    )


def write_csv(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    args = parse_args()
    planner_path = args.planner.resolve()
    skeleton_path = args.skeleton_metrics.resolve()
    output_dir = args.output_dir.resolve()

    planner_fieldnames, planner_rows = read_rows(planner_path)
    skeleton_fieldnames, skeleton_rows = read_rows(skeleton_path)

    planner_case_column = require_column(
        planner_fieldnames, PLANNER_CASE_ID_CANDIDATES, "planner case id column"
    )
    selected_response_column = require_column(
        planner_fieldnames, SELECTED_RESPONSE_CANDIDATES, "planner selected response column"
    )
    skeleton_case_column = require_column(
        skeleton_fieldnames, SKELETON_CASE_ID_CANDIDATES, "skeleton case id column"
    )
    skeleton_response_column = require_column(
        skeleton_fieldnames, SKELETON_RESPONSE_CANDIDATES, "skeleton response type column"
    )

    for required in ("SelectedStepCount", "SourceStepCount", "AnchorPreservation", "GapFill"):
        if required not in skeleton_fieldnames:
            raise ValueError(f"Skeleton metrics missing required column: {required}")

    skeleton_index = build_skeleton_index(
        skeleton_rows, skeleton_case_column, skeleton_response_column
    )
    analysis_rows, diagnostics, density_method = build_analysis(
        planner_rows,
        skeleton_index,
        planner_case_column,
        selected_response_column,
    )

    if not analysis_rows:
        raise ValueError("No planner rows could be aligned to skeleton metrics.")

    summary_rows = build_summary(analysis_rows)

    analysis_fieldnames = [
        "CaseID",
        "ResponseType",
        "TargetDensity",
        "ActualDensity",
        "DensityError",
        "PreserveAnchors",
        "ActualAnchorPreservation",
        "AnchorMismatch",
        "ComplementarityBias",
        "ActualGapFill",
        "GapMismatch",
    ]
    for optional in ("StructureType", "EnergyProfile", "VariantKind"):
        if optional in analysis_rows[0]:
            analysis_fieldnames.append(optional)

    summary_fieldnames = [
        "Section",
        "GroupField",
        "GroupValue",
        "CaseCount",
        "MeanDensityError",
        "MeanAnchorMismatch",
        "MeanGapMismatch",
    ]

    analysis_path = output_dir / "plan_vs_output_analysis.csv"
    summary_path = output_dir / "plan_vs_output_summary.csv"

    write_csv(analysis_path, analysis_fieldnames, analysis_rows)
    write_csv(summary_path, summary_fieldnames, summary_rows)

    overall = summary_rows[0]

    print("Plan vs output analysis complete.")
    print(f"Planner CSV: {planner_path}")
    print(f"Skeleton metrics CSV: {skeleton_path}")
    print(f"Planner alignment columns: {planner_case_column} + {selected_response_column}")
    print(f"Skeleton alignment columns: {skeleton_case_column} + {skeleton_response_column}")
    print(f"Planner rows read: {diagnostics['planner_rows']}")
    print(f"Aligned rows written: {diagnostics['aligned_rows']}")
    print(f"Missing alignments excluded: {diagnostics['missing_alignment_rows']}")
    print(f"Actual density method: {density_method}")
    print(f"Mean density error: {overall['MeanDensityError']}")
    print(f"Mean anchor mismatch: {overall['MeanAnchorMismatch']}")
    print(f"Mean gap mismatch: {overall['MeanGapMismatch']}")
    print(f"Analysis CSV: {analysis_path}")
    print(f"Summary CSV: {summary_path}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
