#!/usr/bin/env python3
"""
Extract structural skeleton metrics for Chapter 5 evaluation.

Produces:
  - skeleton_case_metrics.csv
  - skeleton_pairwise_divergence.csv
  - skeleton_metrics_summary.csv

The current skeleton batch CSV represents rhythmic structures as pipe-delimited
step-index sets, for example:
  SourceHitIndices:   0|14|27|41|54|68|81|95
  SelectedHitIndices: 0|12|27|41|42|54|66|68|81|93

Ratios already present in the batch output are used where available; otherwise
the script computes them from the step sets.
"""

from __future__ import annotations

import argparse
import csv
import itertools
import math
import sys
from collections import defaultdict
from pathlib import Path
from statistics import mean
from typing import Iterable


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT_DIR = REPO_ROOT / "Evaluation" / "chapter5_skeleton_outputs"

CASE_ID_CANDIDATES = ("SourceId", "SourceID", "CaseId", "CaseID", "Id", "ID")
RESPONSE_TYPE_CANDIDATES = ("ResponseType", "SelectedResponseType", "Response")
SOURCE_STEPS_CANDIDATES = ("SourceHitIndices", "SourceSteps", "SourcePattern")
SELECTED_STEPS_CANDIDATES = (
    "SelectedHitIndices",
    "SelectedSkeletonSteps",
    "SelectedSteps",
    "SkeletonHitIndices",
)
ANCHOR_STEPS_CANDIDATES = ("SourceAnchorIndices", "AnchorIndices", "SourceAnchors")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Extract Chapter 5 structural metrics from skeleton batch output."
    )
    parser.add_argument(
        "--source",
        type=Path,
        help=(
            "Skeleton CSV to process. If omitted, the latest/current skeleton "
            "batch output is auto-selected from the workspace."
        ),
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
        raise FileNotFoundError(f"Source CSV does not exist: {path}")

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


def optional_ratio(row: dict[str, str], column: str) -> float | None:
    value = (row.get(column) or "").strip()
    if value == "":
        return None
    try:
        number = float(value)
    except ValueError:
        return None
    if math.isnan(number):
        return None
    return number


def parse_step_set(value: str | None) -> set[int]:
    text = (value or "").strip()
    if not text:
        return set()

    steps: set[int] = set()
    for part in text.replace(",", "|").split("|"):
        part = part.strip()
        if not part:
            continue
        try:
            steps.add(int(part))
        except ValueError as exc:
            raise ValueError(f"Invalid step index {part!r} in {text!r}") from exc
    return steps


def ratio(numerator: int, denominator: int) -> float:
    return 0.0 if denominator == 0 else numerator / denominator


def jaccard_distance(a: set[int], b: set[int]) -> float:
    union = a | b
    if not union:
        return 0.0
    return 1.0 - (len(a & b) / len(union))


def response_rank(response_type: str) -> tuple[int, str]:
    preferred = ["Mirror", "Complement", "Simplify", "Intensify", "Contrast", "Fill"]
    try:
        return (preferred.index(response_type), response_type)
    except ValueError:
        return (len(preferred), response_type)


def candidate_csvs(root: Path) -> list[Path]:
    return [
        path
        for path in root.rglob("*.csv")
        if "skeleton" in path.name.lower()
        and "response_planner" in path.name.lower()
        and "skeleton_metrics" not in path.name.lower()
        and "skeleton_case_metrics" not in path.name.lower()
        and "skeleton_pairwise_divergence" not in path.name.lower()
    ]


def candidate_rank(path: Path, rows: list[dict[str, str]]) -> tuple:
    name = path.name.lower()
    path_text = str(path).lower()
    builder_versions = {row.get("BuilderVersion", "") for row in rows}

    has_v2_builder = any("postarchitecturerevamp_v2" in v.lower() for v in builder_versions)
    has_post_architecture = "postarchitecturerevamp" in name or any(
        "postarchitecturerevamp" in v.lower() for v in builder_versions
    )
    diagnostics_current = "diagnosticsoutput" in path_text
    historical = "prephase1" in path_text or "phase 1 tuning" in path_text

    return (
        has_v2_builder,
        has_post_architecture,
        diagnostics_current,
        not historical,
        path.stat().st_mtime,
        len(rows),
    )


def auto_select_source(root: Path) -> Path:
    viable = []
    errors = []

    for path in candidate_csvs(root):
        try:
            fieldnames, rows = read_rows(path)
            require_column(fieldnames, CASE_ID_CANDIDATES, "case/source id column")
            require_column(fieldnames, RESPONSE_TYPE_CANDIDATES, "response type column")
            require_column(fieldnames, SOURCE_STEPS_CANDIDATES, "source step set column")
            require_column(fieldnames, SELECTED_STEPS_CANDIDATES, "selected step set column")
            viable.append((candidate_rank(path, rows), path))
        except Exception as exc:
            errors.append(f"{path}: {exc}")

    if not viable:
        detail = "\n".join(errors) if errors else "No skeleton CSV candidates found."
        raise ValueError(f"No viable skeleton CSV found under {root}.\n{detail}")

    viable.sort(key=lambda item: item[0], reverse=True)
    return viable[0][1]


def build_case_metrics(
    rows: list[dict[str, str]],
    case_column: str,
    response_column: str,
    source_steps_column: str,
    selected_steps_column: str,
    anchor_steps_column: str | None,
) -> tuple[list[dict[str, str]], dict[tuple[str, str], set[int]], bool]:
    case_metrics = []
    selected_by_case_response: dict[tuple[str, str], set[int]] = {}
    anchor_available = anchor_steps_column is not None

    for index, row in enumerate(rows, start=1):
        case_id = (row.get(case_column) or "").strip()
        response_type = (row.get(response_column) or "").strip()
        if not case_id:
            raise ValueError(f"Blank case id in row {index}, column {case_column}")
        if not response_type:
            raise ValueError(f"Blank response type in row {index}, column {response_column}")

        source_steps = parse_step_set(row.get(source_steps_column))
        selected_steps = parse_step_set(row.get(selected_steps_column))
        anchor_steps = parse_step_set(row.get(anchor_steps_column)) if anchor_steps_column else set()

        overlap = optional_ratio(row, "OverlapRatio")
        if overlap is None:
            overlap = ratio(len(selected_steps & source_steps), len(selected_steps))

        gap_fill = optional_ratio(row, "GapFillRatio")
        if gap_fill is None:
            gap_fill = ratio(len(selected_steps - source_steps), len(selected_steps))

        anchor_preservation = None
        if anchor_steps_column:
            anchor_preservation = optional_ratio(row, "AnchorPreservationRatio")
            if anchor_preservation is None:
                anchor_preservation = ratio(len(anchor_steps & selected_steps), len(anchor_steps))

        key = (case_id, response_type)
        if key in selected_by_case_response:
            raise ValueError(
                f"Duplicate case/response combination: CaseID={case_id}, "
                f"ResponseType={response_type}"
            )
        selected_by_case_response[key] = selected_steps

        output_row = {
            "CaseID": case_id,
            "ResponseType": response_type,
            "Overlap": f"{overlap:.6g}",
            "GapFill": f"{gap_fill:.6g}",
            "SelectedStepCount": str(len(selected_steps)),
            "SourceStepCount": str(len(source_steps)),
        }
        if anchor_steps_column:
            output_row["AnchorPreservation"] = f"{anchor_preservation:.6g}"
        case_metrics.append(output_row)

    case_metrics.sort(key=lambda row: (row["CaseID"], response_rank(row["ResponseType"])))
    return case_metrics, selected_by_case_response, anchor_available


def build_pairwise_divergence(
    selected_by_case_response: dict[tuple[str, str], set[int]]
) -> list[dict[str, str]]:
    by_case: dict[str, dict[str, set[int]]] = defaultdict(dict)
    for (case_id, response_type), selected_steps in selected_by_case_response.items():
        by_case[case_id][response_type] = selected_steps

    pairwise_rows = []
    for case_id in sorted(by_case):
        responses = sorted(by_case[case_id], key=response_rank)
        for response_a, response_b in itertools.combinations(responses, 2):
            distance = jaccard_distance(by_case[case_id][response_a], by_case[case_id][response_b])
            pairwise_rows.append(
                {
                    "CaseID": case_id,
                    "ResponseTypeA": response_a,
                    "ResponseTypeB": response_b,
                    "JaccardDistance": f"{distance:.6g}",
                }
            )

    return pairwise_rows


def add_case_divergence(
    case_metrics: list[dict[str, str]], pairwise_rows: list[dict[str, str]]
) -> None:
    distances: dict[tuple[str, str], list[float]] = defaultdict(list)
    for row in pairwise_rows:
        case_id = row["CaseID"]
        distance = float(row["JaccardDistance"])
        distances[(case_id, row["ResponseTypeA"])].append(distance)
        distances[(case_id, row["ResponseTypeB"])].append(distance)

    for row in case_metrics:
        values = distances.get((row["CaseID"], row["ResponseType"]), [])
        row["MeanPairwiseJaccardDistance"] = f"{mean(values):.6g}" if values else "0"


def build_summary(
    case_metrics: list[dict[str, str]],
    anchor_available: bool,
) -> tuple[list[str], list[dict[str, str]]]:
    by_response: dict[str, list[dict[str, str]]] = defaultdict(list)
    for row in case_metrics:
        by_response[row["ResponseType"]].append(row)

    fieldnames = ["ResponseType", "MeanOverlap", "MeanGapFill"]
    if anchor_available:
        fieldnames.append("MeanAnchorPreservation")
    fieldnames.extend(["MeanDivergence", "CaseCount"])

    summary_rows = []
    for response_type in sorted(by_response, key=response_rank):
        rows = by_response[response_type]
        summary = {
            "ResponseType": response_type,
            "MeanOverlap": f"{mean(float(row['Overlap']) for row in rows):.6g}",
            "MeanGapFill": f"{mean(float(row['GapFill']) for row in rows):.6g}",
            "MeanDivergence": f"{mean(float(row['MeanPairwiseJaccardDistance']) for row in rows):.6g}",
            "CaseCount": str(len(rows)),
        }
        if anchor_available:
            summary["MeanAnchorPreservation"] = f"{mean(float(row['AnchorPreservation']) for row in rows):.6g}"
        summary_rows.append(summary)

    return fieldnames, summary_rows


def write_csv(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    args = parse_args()
    source = args.source.resolve() if args.source else auto_select_source(REPO_ROOT)
    output_dir = args.output_dir.resolve()

    fieldnames, rows = read_rows(source)
    case_column = require_column(fieldnames, CASE_ID_CANDIDATES, "case/source id column")
    response_column = require_column(fieldnames, RESPONSE_TYPE_CANDIDATES, "response type column")
    source_steps_column = require_column(fieldnames, SOURCE_STEPS_CANDIDATES, "source step set column")
    selected_steps_column = require_column(
        fieldnames, SELECTED_STEPS_CANDIDATES, "selected step set column"
    )
    anchor_steps_column = first_existing(fieldnames, ANCHOR_STEPS_CANDIDATES)

    case_metrics, selected_by_case_response, anchor_available = build_case_metrics(
        rows,
        case_column,
        response_column,
        source_steps_column,
        selected_steps_column,
        anchor_steps_column,
    )
    pairwise_rows = build_pairwise_divergence(selected_by_case_response)
    add_case_divergence(case_metrics, pairwise_rows)
    summary_fieldnames, summary_rows = build_summary(case_metrics, anchor_available)

    case_fieldnames = [
        "CaseID",
        "ResponseType",
        "Overlap",
        "GapFill",
    ]
    if anchor_available:
        case_fieldnames.append("AnchorPreservation")
    case_fieldnames.extend(
        ["SelectedStepCount", "SourceStepCount", "MeanPairwiseJaccardDistance"]
    )

    case_path = output_dir / "skeleton_case_metrics.csv"
    pairwise_path = output_dir / "skeleton_pairwise_divergence.csv"
    summary_path = output_dir / "skeleton_metrics_summary.csv"

    write_csv(case_path, case_fieldnames, case_metrics)
    write_csv(
        pairwise_path,
        ["CaseID", "ResponseTypeA", "ResponseTypeB", "JaccardDistance"],
        pairwise_rows,
    )
    write_csv(summary_path, summary_fieldnames, summary_rows)

    response_types = sorted({row["ResponseType"] for row in case_metrics}, key=response_rank)
    case_count = len({row["CaseID"] for row in case_metrics})

    print("Skeleton metrics extraction complete.")
    print(f"Source CSV: {source}")
    print(f"Rows processed: {len(rows)}")
    print(f"Unique source cases: {case_count}")
    print(f"Response types: {', '.join(response_types)}")
    print(f"Case/source id column: {case_column}")
    print(f"Response type column: {response_column}")
    print(f"Source step set column: {source_steps_column}")
    print(f"Selected step set column: {selected_steps_column}")
    print(f"Anchor step set column: {anchor_steps_column or 'not available'}")
    print("Overlap/GapFill: used batch ratios when present, otherwise computed from step sets.")
    print("Divergence: computed as pairwise Jaccard distance between selected step sets.")
    print(f"Case metrics CSV: {case_path}")
    print(f"Pairwise divergence CSV: {pairwise_path}")
    print(f"Summary CSV: {summary_path}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
