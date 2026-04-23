#!/usr/bin/env python3
"""
Process TurnAnalysisResponsePlanner CSV output for Chapter 5 evaluation.

Produces:
  - planner_distribution.csv
  - planner_margins.csv

The script is intentionally conservative: it inspects the input schema, detects
the selected-response, case-id, and score columns, then fails with a clear error
if the CSV does not contain enough information to compute the requested outputs.
"""

from __future__ import annotations

import argparse
import csv
import sys
from collections import Counter
from pathlib import Path
from typing import Iterable


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT_DIR = REPO_ROOT / "Evaluation" / "chapter5_planner_outputs"

SELECTED_RESPONSE_CANDIDATES = (
    "SelectedResponseType",
    "SelectedResponse",
    "PlannerSelectedResponseType",
    "SnapshotSelectedResponseType",
    "ResponseType",
)

CASE_ID_CANDIDATES = (
    "CaseID",
    "CaseId",
    "SourceId",
    "SourceID",
    "Id",
    "ID",
)

SCORE_PREFIXES = (
    "Score_",
    "score_",
    "PlannerScore_",
    "ResponseScore_",
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate Chapter 5 planner distribution and margin CSVs."
    )
    parser.add_argument(
        "--source",
        type=Path,
        help=(
            "Planner CSV to process. If omitted, the script searches for the "
            "latest planner CSV with a selected-response column and score columns."
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


def detect_selected_column(fieldnames: list[str]) -> str:
    column = first_existing(fieldnames, SELECTED_RESPONSE_CANDIDATES)
    if column:
        return column

    lowered = {name.lower(): name for name in fieldnames}
    for key, original in lowered.items():
        if "selected" in key and "response" in key:
            return original

    raise ValueError(
        "Could not detect selected-response column. Looked for: "
        + ", ".join(SELECTED_RESPONSE_CANDIDATES)
    )


def detect_case_id_column(fieldnames: list[str]) -> str | None:
    column = first_existing(fieldnames, CASE_ID_CANDIDATES)
    if column:
        return column

    for name in fieldnames:
        lowered = name.lower()
        if lowered.endswith("id") and ("case" in lowered or "source" in lowered):
            return name

    return None


def is_float(value: str | None) -> bool:
    if value is None or value == "":
        return False
    try:
        float(value)
    except ValueError:
        return False
    return True


def detect_score_columns(fieldnames: list[str], rows: list[dict[str, str]]) -> list[str]:
    prefixed = [
        name
        for name in fieldnames
        if any(name.startswith(prefix) for prefix in SCORE_PREFIXES)
    ]

    numeric_prefixed = [
        name
        for name in prefixed
        if any(is_float(row.get(name)) for row in rows)
    ]

    if len(numeric_prefixed) >= 2:
        return numeric_prefixed

    # Fallback for small schema variations, while avoiding feature columns such
    # as Density or Energy that are numeric but not response scores.
    score_named = [
        name
        for name in fieldnames
        if "score" in name.lower() and any(is_float(row.get(name)) for row in rows)
    ]

    if len(score_named) >= 2:
        return score_named

    raise ValueError(
        "Could not detect at least two numeric response score columns. "
        "Expected columns such as Score_Mirror, Score_Complement, etc."
    )


def response_type_from_score_column(column: str) -> str:
    for prefix in SCORE_PREFIXES:
        if column.startswith(prefix):
            return column[len(prefix) :]
    return column


def build_distribution(
    rows: list[dict[str, str]], selected_column: str
) -> list[dict[str, str]]:
    total = len(rows)
    counts = Counter((row.get(selected_column) or "").strip() for row in rows)

    if "" in counts:
        raise ValueError(f"Selected-response column has blank values: {selected_column}")

    distribution = []
    for response_type, count in sorted(counts.items()):
        percentage = (count / total) * 100
        distribution.append(
            {
                "ResponseType": response_type,
                "Count": str(count),
                "Percentage": f"{percentage:.2f}",
            }
        )

    return distribution


def build_margins(
    rows: list[dict[str, str]],
    case_id_column: str | None,
    selected_column: str,
    score_columns: list[str],
) -> list[dict[str, str]]:
    margins = []

    for index, row in enumerate(rows, start=1):
        scored_responses = []
        for column in score_columns:
            value = row.get(column)
            if not is_float(value):
                raise ValueError(
                    f"Non-numeric or blank score in row {index}, column {column}: {value!r}"
                )
            scored_responses.append((response_type_from_score_column(column), float(value)))

        ranked = sorted(scored_responses, key=lambda item: item[1], reverse=True)
        if len(ranked) < 2:
            raise ValueError(f"Row {index} has fewer than two response scores.")

        top_score = ranked[0][1]
        second_score = ranked[1][1]
        case_id = row.get(case_id_column, "").strip() if case_id_column else ""
        if not case_id:
            case_id = f"Row_{index:04d}"

        margins.append(
            {
                "CaseID": case_id,
                "SelectedResponseType": (row.get(selected_column) or "").strip(),
                "TopScore": f"{top_score:.6g}",
                "SecondScore": f"{second_score:.6g}",
                "Margin": f"{(top_score - second_score):.6g}",
            }
        )

    return margins


def write_csv(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def candidate_csvs(root: Path) -> list[Path]:
    return [
        path
        for path in root.rglob("*.csv")
        if "planner" in path.name.lower()
        and "response_planner" in path.name.lower()
        and "planner_distribution" not in path.name.lower()
        and "planner_margins" not in path.name.lower()
    ]


def candidate_rank(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> tuple:
    selected_ok = first_existing(fieldnames, SELECTED_RESPONSE_CANDIDATES) is not None
    score_count = len(
        [
            name
            for name in fieldnames
            if any(name.startswith(prefix) for prefix in SCORE_PREFIXES)
        ]
    )
    one_row_per_case_hint = "skeleton" not in path.name.lower()
    base_plus_variants_hint = "base_plus_variants" in path.name.lower()

    return (
        selected_ok,
        score_count >= 2,
        one_row_per_case_hint,
        base_plus_variants_hint,
        path.stat().st_mtime,
        len(rows),
    )


def auto_select_source(root: Path) -> Path:
    viable = []
    errors = []

    for path in candidate_csvs(root):
        try:
            fieldnames, rows = read_rows(path)
            selected_column = detect_selected_column(fieldnames)
            score_columns = detect_score_columns(fieldnames, rows)
            if selected_column and len(score_columns) >= 2:
                viable.append((candidate_rank(path, fieldnames, rows), path))
        except Exception as exc:  # Keep searching and report if nothing works.
            errors.append(f"{path}: {exc}")

    if not viable:
        detail = "\n".join(errors) if errors else "No planner CSV candidates found."
        raise ValueError(f"No viable planner CSV found under {root}.\n{detail}")

    viable.sort(key=lambda item: item[0], reverse=True)
    return viable[0][1]


def main() -> int:
    args = parse_args()
    source = args.source.resolve() if args.source else auto_select_source(REPO_ROOT)
    output_dir = args.output_dir.resolve()

    fieldnames, rows = read_rows(source)
    selected_column = detect_selected_column(fieldnames)
    case_id_column = detect_case_id_column(fieldnames)
    score_columns = detect_score_columns(fieldnames, rows)

    distribution = build_distribution(rows, selected_column)
    margins = build_margins(rows, case_id_column, selected_column, score_columns)

    distribution_path = output_dir / "planner_distribution.csv"
    margins_path = output_dir / "planner_margins.csv"

    write_csv(
        distribution_path,
        ["ResponseType", "Count", "Percentage"],
        distribution,
    )
    write_csv(
        margins_path,
        ["CaseID", "SelectedResponseType", "TopScore", "SecondScore", "Margin"],
        margins,
    )

    print("Planner CSV processing complete.")
    print(f"Source CSV: {source}")
    print(f"Selected-response column: {selected_column}")
    print(f"Case-id column: {case_id_column or 'generated row ids'}")
    print(f"Score columns: {', '.join(score_columns)}")
    print(f"Rows processed: {len(rows)}")
    print(f"Distribution CSV: {distribution_path}")
    print(f"Margins CSV: {margins_path}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
