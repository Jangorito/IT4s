#!/usr/bin/env python3
"""
Analyse planner sensitivity across base/variant case pairs for Chapter 5.

Produces:
  - planner_variant_analysis.csv
  - planner_variant_summary.csv
  - planner_variant_examples.csv

The current planner CSV contains explicit Base/Variant rows and ParentCaseId
links. This script uses ParentCaseId when available, then falls back to the
Base_ / Variant_ case-id prefix convention.
"""

from __future__ import annotations

import argparse
import csv
import sys
from collections import Counter, defaultdict
from pathlib import Path
from statistics import median
from typing import Iterable


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SOURCE = REPO_ROOT / "ModelAnalysis" / "synthetic_turn_analysis_response_planner_base_plus_variants.csv"
DEFAULT_OUTPUT_DIR = REPO_ROOT / "Evaluation" / "chapter5_variant_outputs"

CASE_ID_CANDIDATES = ("CaseId", "CaseID", "SourceId", "SourceID", "Id", "ID")
SELECTED_RESPONSE_CANDIDATES = (
    "SelectedResponseType",
    "SelectedResponse",
    "PlannerSelectedResponseType",
    "SnapshotSelectedResponseType",
)
VARIANT_KIND_CANDIDATES = ("VariantKind", "CaseKind", "GenerationKind")
PARENT_CASE_CANDIDATES = ("ParentCaseId", "ParentCaseID", "ParentId", "BaseCaseId")
SCORE_PREFIXES = ("Score_", "score_", "PlannerScore_", "ResponseScore_")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Analyse base/variant planner response sensitivity."
    )
    parser.add_argument(
        "--source",
        type=Path,
        default=DEFAULT_SOURCE,
        help=f"Planner CSV to process. Default: {DEFAULT_SOURCE}",
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
        name for name in prefixed if any(is_float(row.get(name)) for row in rows)
    ]
    if len(numeric_prefixed) >= 2:
        return numeric_prefixed

    fallback = [
        name
        for name in fieldnames
        if "score" in name.lower() and any(is_float(row.get(name)) for row in rows)
    ]
    if len(fallback) >= 2:
        return fallback

    raise ValueError(
        "Could not detect at least two numeric response score columns. "
        "Expected fields such as Score_Mirror, Score_Complement, etc."
    )


def response_type_from_score_column(column: str) -> str:
    for prefix in SCORE_PREFIXES:
        if column.startswith(prefix):
            return column[len(prefix) :]
    return column


def top_score_and_margin(row: dict[str, str], score_columns: list[str]) -> tuple[float, float]:
    scored = []
    for column in score_columns:
        value = row.get(column)
        if not is_float(value):
            raise ValueError(
                f"Non-numeric or blank score for case {row!r}, column {column}: {value!r}"
            )
        scored.append((response_type_from_score_column(column), float(value)))

    ranked = sorted(scored, key=lambda item: item[1], reverse=True)
    if len(ranked) < 2:
        raise ValueError("Need at least two response scores to compute a margin.")

    return ranked[0][1], ranked[0][1] - ranked[1][1]


def pair_key_from_case_id(case_id: str) -> str | None:
    if case_id.startswith("Base_"):
        return case_id[len("Base_") :]
    if case_id.startswith("Variant_"):
        return case_id[len("Variant_") :]
    return None


def classify_rows(
    rows: list[dict[str, str]],
    case_column: str,
    variant_kind_column: str | None,
) -> tuple[dict[str, dict[str, str]], list[dict[str, str]]]:
    bases = {}
    variants = []

    for row in rows:
        case_id = (row.get(case_column) or "").strip()
        if not case_id:
            raise ValueError(f"Blank case id in column {case_column}")

        kind = (row.get(variant_kind_column) or "").strip().lower() if variant_kind_column else ""
        is_base = kind == "base" or case_id.startswith("Base_")
        is_variant = kind == "variant" or case_id.startswith("Variant_")

        if is_base and not is_variant:
            if case_id in bases:
                raise ValueError(f"Duplicate base case id: {case_id}")
            bases[case_id] = row
        elif is_variant and not is_base:
            variants.append(row)

    return bases, variants


def build_pairs(
    rows: list[dict[str, str]],
    case_column: str,
    selected_column: str,
    score_columns: list[str],
    variant_kind_column: str | None,
    parent_case_column: str | None,
) -> tuple[list[dict[str, str]], dict[str, int]]:
    bases, variants = classify_rows(rows, case_column, variant_kind_column)
    analysis_rows = []
    unpaired_variant_count = 0
    prefix_parent_mismatch_count = 0

    for variant in variants:
        variant_case_id = (variant.get(case_column) or "").strip()
        parent_case_id = (variant.get(parent_case_column) or "").strip() if parent_case_column else ""

        fallback_pair_key = pair_key_from_case_id(variant_case_id)
        fallback_base_id = f"Base_{fallback_pair_key}" if fallback_pair_key else ""
        base_case_id = parent_case_id or fallback_base_id

        if parent_case_id and fallback_base_id and parent_case_id != fallback_base_id:
            prefix_parent_mismatch_count += 1

        base = bases.get(base_case_id)
        if base is None:
            unpaired_variant_count += 1
            continue

        pair_key = pair_key_from_case_id(base_case_id) or fallback_pair_key or base_case_id
        base_selected = (base.get(selected_column) or "").strip()
        variant_selected = (variant.get(selected_column) or "").strip()
        if not base_selected or not variant_selected:
            raise ValueError(
                f"Blank selected response for pair {base_case_id} / {variant_case_id}"
            )

        base_top_score, base_margin = top_score_and_margin(base, score_columns)
        variant_top_score, variant_margin = top_score_and_margin(variant, score_columns)

        output = {
            "PairKey": pair_key,
            "BaseCaseID": base_case_id,
            "VariantCaseID": variant_case_id,
            "BaseSelectedResponseType": base_selected,
            "VariantSelectedResponseType": variant_selected,
            "ResponseTypeChanged": str(base_selected != variant_selected),
            "BaseTopScore": f"{base_top_score:.6g}",
            "VariantTopScore": f"{variant_top_score:.6g}",
            "BaseMargin": f"{base_margin:.6g}",
            "VariantMargin": f"{variant_margin:.6g}",
        }

        for column in ("StructureType", "EnergyProfile"):
            if column in base:
                output[f"Base{column}"] = (base.get(column) or "").strip()
            if column in variant:
                output[f"Variant{column}"] = (variant.get(column) or "").strip()

        analysis_rows.append(output)

    analysis_rows.sort(key=lambda row: row["PairKey"])
    diagnostics = {
        "base_rows": len(bases),
        "variant_rows": len(variants),
        "unpaired_variant_rows": unpaired_variant_count,
        "prefix_parent_mismatches": prefix_parent_mismatch_count,
    }
    return analysis_rows, diagnostics


def percentage(changed: int, total: int) -> str:
    return f"{((changed / total) * 100 if total else 0.0):.2f}"


def add_summary_row(
    rows: list[dict[str, str]],
    section: str,
    group_field: str,
    group_value: str,
    total: int,
    changed: int,
    base_response: str = "",
    variant_response: str = "",
) -> None:
    rows.append(
        {
            "Section": section,
            "GroupField": group_field,
            "GroupValue": group_value,
            "BaseSelectedResponseType": base_response,
            "VariantSelectedResponseType": variant_response,
            "PairCount": str(total),
            "ChangedCount": str(changed),
            "PercentageChanged": percentage(changed, total),
        }
    )


def build_summary(analysis_rows: list[dict[str, str]]) -> list[dict[str, str]]:
    summary_rows = []
    total = len(analysis_rows)
    changed = sum(row["ResponseTypeChanged"] == "True" for row in analysis_rows)

    add_summary_row(summary_rows, "Overall", "All", "All", total, changed)

    for group_field in ("BaseStructureType", "BaseEnergyProfile", "BaseSelectedResponseType"):
        if not analysis_rows or group_field not in analysis_rows[0]:
            continue
        groups: dict[str, list[dict[str, str]]] = defaultdict(list)
        for row in analysis_rows:
            groups[row.get(group_field, "")].append(row)
        for group_value in sorted(groups):
            group_rows = groups[group_value]
            group_changed = sum(row["ResponseTypeChanged"] == "True" for row in group_rows)
            add_summary_row(
                summary_rows,
                "Grouped",
                group_field,
                group_value,
                len(group_rows),
                group_changed,
            )

    matrix = Counter(
        (row["BaseSelectedResponseType"], row["VariantSelectedResponseType"])
        for row in analysis_rows
    )
    for (base_response, variant_response), count in sorted(matrix.items()):
        changed_count = 0 if base_response == variant_response else count
        add_summary_row(
            summary_rows,
            "ChangeMatrix",
            "BaseToVariantResponseType",
            f"{base_response}->{variant_response}",
            count,
            changed_count,
            base_response,
            variant_response,
        )

    return summary_rows


def select_examples(analysis_rows: list[dict[str, str]]) -> list[dict[str, str]]:
    changed_rows = [row for row in analysis_rows if row["ResponseTypeChanged"] == "True"]
    unchanged_rows = [row for row in analysis_rows if row["ResponseTypeChanged"] == "False"]

    def min_margin(row: dict[str, str]) -> float:
        return min(float(row["BaseMargin"]), float(row["VariantMargin"]))

    def combined_margin(row: dict[str, str]) -> float:
        return float(row["BaseMargin"]) + float(row["VariantMargin"])

    changed_sorted = sorted(changed_rows, key=lambda row: (min_margin(row), row["PairKey"]))
    unchanged_sorted = sorted(
        unchanged_rows,
        key=lambda row: (
            abs(combined_margin(row) - median([combined_margin(r) for r in unchanged_rows]))
            if unchanged_rows
            else 0.0,
            row["PairKey"],
        ),
    )

    examples = []
    examples.extend(take_diverse_changed_examples(changed_sorted, limit=5))
    examples.extend(take_diverse_unchanged_examples(unchanged_sorted, limit=5))
    return examples


def take_diverse_changed_examples(
    rows: list[dict[str, str]], limit: int
) -> list[dict[str, str]]:
    selected = []
    seen_transitions = set()

    for row in rows:
        transition = (row["BaseSelectedResponseType"], row["VariantSelectedResponseType"])
        if transition in seen_transitions:
            continue
        selected.append(example_row(row, "Changed"))
        seen_transitions.add(transition)
        if len(selected) == limit:
            return selected

    for row in rows:
        candidate = example_row(row, "Changed")
        if candidate not in selected:
            selected.append(candidate)
        if len(selected) == limit:
            break
    return selected


def take_diverse_unchanged_examples(
    rows: list[dict[str, str]], limit: int
) -> list[dict[str, str]]:
    selected = []
    seen_responses = set()

    for row in rows:
        response = row["BaseSelectedResponseType"]
        if response in seen_responses:
            continue
        selected.append(example_row(row, "Unchanged"))
        seen_responses.add(response)
        if len(selected) == limit:
            return selected

    for row in rows:
        candidate = example_row(row, "Unchanged")
        if candidate not in selected:
            selected.append(candidate)
        if len(selected) == limit:
            break
    return selected


def example_row(row: dict[str, str], label: str) -> dict[str, str]:
    return {
        "ExampleType": label,
        "PairKey": row["PairKey"],
        "BaseCaseID": row["BaseCaseID"],
        "VariantCaseID": row["VariantCaseID"],
        "BaseSelectedResponseType": row["BaseSelectedResponseType"],
        "VariantSelectedResponseType": row["VariantSelectedResponseType"],
        "BaseMargin": row["BaseMargin"],
        "VariantMargin": row["VariantMargin"],
    }


def write_csv(path: Path, fieldnames: list[str], rows: list[dict[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def main() -> int:
    args = parse_args()
    source = args.source.resolve()
    output_dir = args.output_dir.resolve()

    fieldnames, rows = read_rows(source)
    case_column = require_column(fieldnames, CASE_ID_CANDIDATES, "case id column")
    selected_column = require_column(
        fieldnames, SELECTED_RESPONSE_CANDIDATES, "selected-response column"
    )
    variant_kind_column = first_existing(fieldnames, VARIANT_KIND_CANDIDATES)
    parent_case_column = first_existing(fieldnames, PARENT_CASE_CANDIDATES)
    score_columns = detect_score_columns(fieldnames, rows)

    analysis_rows, diagnostics = build_pairs(
        rows,
        case_column,
        selected_column,
        score_columns,
        variant_kind_column,
        parent_case_column,
    )

    if not analysis_rows:
        raise ValueError("No valid base/variant pairs were detected.")

    summary_rows = build_summary(analysis_rows)
    example_rows = select_examples(analysis_rows)

    analysis_fieldnames = [
        "PairKey",
        "BaseCaseID",
        "VariantCaseID",
        "BaseSelectedResponseType",
        "VariantSelectedResponseType",
        "ResponseTypeChanged",
        "BaseTopScore",
        "VariantTopScore",
        "BaseMargin",
        "VariantMargin",
    ]
    for optional in (
        "BaseStructureType",
        "VariantStructureType",
        "BaseEnergyProfile",
        "VariantEnergyProfile",
    ):
        if optional in analysis_rows[0]:
            analysis_fieldnames.append(optional)

    summary_fieldnames = [
        "Section",
        "GroupField",
        "GroupValue",
        "BaseSelectedResponseType",
        "VariantSelectedResponseType",
        "PairCount",
        "ChangedCount",
        "PercentageChanged",
    ]
    example_fieldnames = [
        "ExampleType",
        "PairKey",
        "BaseCaseID",
        "VariantCaseID",
        "BaseSelectedResponseType",
        "VariantSelectedResponseType",
        "BaseMargin",
        "VariantMargin",
    ]

    analysis_path = output_dir / "planner_variant_analysis.csv"
    summary_path = output_dir / "planner_variant_summary.csv"
    examples_path = output_dir / "planner_variant_examples.csv"

    write_csv(analysis_path, analysis_fieldnames, analysis_rows)
    write_csv(summary_path, summary_fieldnames, summary_rows)
    write_csv(examples_path, example_fieldnames, example_rows)

    total_pairs = len(analysis_rows)
    changed_count = sum(row["ResponseTypeChanged"] == "True" for row in analysis_rows)
    pairing_logic = (
        "ParentCaseId -> CaseId"
        if parent_case_column
        else "Base_/Variant_ prefix suffix matching"
    )

    print("Planner variant analysis complete.")
    print(f"Source CSV: {source}")
    print(f"Rows read: {len(rows)}")
    print(f"Case id column: {case_column}")
    print(f"Selected-response column: {selected_column}")
    print(f"Variant kind column: {variant_kind_column or 'not available'}")
    print(f"Parent case column: {parent_case_column or 'not available'}")
    print(f"Pairing logic: {pairing_logic}")
    print(f"Base rows detected: {diagnostics['base_rows']}")
    print(f"Variant rows detected: {diagnostics['variant_rows']}")
    print(f"Valid pairs: {total_pairs}")
    print(f"Unpaired variant rows excluded: {diagnostics['unpaired_variant_rows']}")
    print(f"Parent/prefix mismatches observed: {diagnostics['prefix_parent_mismatches']}")
    print(f"Changed selections: {changed_count} ({percentage(changed_count, total_pairs)}%)")
    print(f"Score columns: {', '.join(score_columns)}")
    print(f"Analysis CSV: {analysis_path}")
    print(f"Summary CSV: {summary_path}")
    print(f"Examples CSV: {examples_path}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
