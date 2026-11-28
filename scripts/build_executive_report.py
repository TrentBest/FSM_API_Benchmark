#!/usr/bin/env python3
"""Convert one matched BenchmarkDotNet CSV run into a fail-closed executive summary."""
from __future__ import annotations

import argparse
import csv
import json
import math
import os
import platform
import re
from datetime import datetime, timezone
from pathlib import Path

EXPECTED_METHODS = {
    "ApplicationLogicBaseline": "applicationLogic",
    "IntegerHandleUpdate": "integerHandle",
    "StringHandleUpdate": "stringHandle",
    "IntegerScheduledTick": "integerScheduled",
    "StringScheduledTick": "stringScheduled",
}
TIME_UNITS_TO_NS = {
    "ns": 1.0,
    "us": 1_000.0,
    "µs": 1_000.0,
    "μs": 1_000.0,
    "ms": 1_000_000.0,
    "s": 1_000_000_000.0,
}
BYTE_UNITS = {"B": 1.0, "KB": 1024.0, "MB": 1024.0**2, "GB": 1024.0**3}


def parse_quantity(value: str | None, units: dict[str, float]) -> float | None:
    if value is None or not value.strip() or value.strip().lower() in {"na", "n/a", "-"}:
        return None
    match = re.match(r"^\s*([+-]?(?:\d[\d,]*(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)\s*([A-Za-zµμ]+)?\s*$", value)
    if not match:
        raise ValueError(f"Unrecognized BenchmarkDotNet quantity: {value!r}")
    numeric_text = match.group(1)
    if "," in numeric_text and not re.fullmatch(r"[+-]?\d{1,3}(?:,\d{3})+(?:\.\d+)?(?:[eE][+-]?\d+)?", numeric_text):
        raise ValueError(f"Invalid thousands grouping in BenchmarkDotNet quantity: {value!r}")
    number = float(numeric_text.replace(",", ""))
    unit = match.group(2) or next(iter(units))
    if not math.isfinite(number) or unit not in units:
        raise ValueError(f"Unsupported or non-finite quantity: {value!r}")
    return number * units[unit]


def read_measurements(results_dir: Path) -> dict[str, dict[str, float | None]]:
    rows: dict[str, dict[str, float | None]] = {}
    csv_files = sorted(results_dir.glob("*report.csv"))
    if not csv_files:
        raise ValueError(f"No BenchmarkDotNet *report.csv files found in {results_dir}")

    for csv_path in csv_files:
        with csv_path.open(newline="", encoding="utf-8-sig") as stream:
            reader = csv.DictReader(stream)
            if not reader.fieldnames or "Method" not in reader.fieldnames or "Mean" not in reader.fieldnames:
                continue
            for row in reader:
                method = (row.get("Method") or "").strip()
                if method not in EXPECTED_METHODS:
                    continue
                lane = EXPECTED_METHODS[method]
                if lane in rows:
                    raise ValueError(f"Duplicate benchmark result for {method}; refusing to choose one.")
                rows[lane] = {
                    "meanNs": parse_quantity(row.get("Mean"), TIME_UNITS_TO_NS),
                    "errorNs": parse_quantity(row.get("Error"), TIME_UNITS_TO_NS),
                    "standardDeviationNs": parse_quantity(row.get("StdDev"), TIME_UNITS_TO_NS),
                    "medianNs": parse_quantity(row.get("Median"), TIME_UNITS_TO_NS),
                    "allocatedBytesPerOperation": parse_quantity(row.get("Allocated"), BYTE_UNITS),
                }

    missing = sorted(set(EXPECTED_METHODS.values()) - set(rows))
    if missing:
        raise ValueError("Missing matched benchmark lanes: " + ", ".join(missing))
    for lane, metrics in rows.items():
        if metrics["meanNs"] is None or metrics["meanNs"] <= 0:
            raise ValueError(f"{lane} has no valid positive mean measurement.")
    if rows["applicationLogic"]["meanNs"] < 1.0:
        raise ValueError("Application-logic baseline is below 1 ns; this workload includes observable delegate calls and state mutation, so a sub-nanosecond result indicates likely dead-code elimination.")
    return rows


def percentage(delta: float, baseline: float) -> float | None:
    return (delta / baseline) * 100.0 if baseline > 0 else None


def derive(integer: float, string: float, baseline: float) -> dict[str, float | None]:
    integer_overhead = integer - baseline
    string_overhead = string - baseline
    string_penalty = string - integer
    return {
        "integerRuntimeOverheadNs": integer_overhead,
        "integerRuntimeOverheadPercent": percentage(integer_overhead, baseline),
        "stringRuntimeOverheadNs": string_overhead,
        "stringRuntimeOverheadPercent": percentage(string_overhead, baseline),
        "stringPenaltyOverIntegerNs": string_penalty,
        "stringPenaltyOverIntegerPercent": percentage(string_penalty, integer),
        "stringToIntegerRatio": string / integer,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--results-dir", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--source-sha", required=True)
    parser.add_argument("--harness-sha", required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--run-attempt", required=True, type=int)
    args = parser.parse_args()

    for label, sha in (("source", args.source_sha), ("harness", args.harness_sha)):
        if not re.fullmatch(r"[0-9a-fA-F]{40}", sha):
            raise SystemExit(f"{label} SHA must be a full 40-character hexadecimal commit.")
    if not args.run_id.isdigit() or int(args.run_id) <= 0 or args.run_attempt < 1:
        raise SystemExit("Run ID and attempt must be positive integers.")

    metrics = read_measurements(args.results_dir)
    baseline = metrics["applicationLogic"]["meanNs"]
    integer_handle = metrics["integerHandle"]["meanNs"]
    string_handle = metrics["stringHandle"]["meanNs"]
    integer_scheduled = metrics["integerScheduled"]["meanNs"]
    string_scheduled = metrics["stringScheduled"]["meanNs"]
    assert all(value is not None for value in (baseline, integer_handle, string_handle, integer_scheduled, string_scheduled))
    baseline = float(baseline)
    integer_handle = float(integer_handle)
    string_handle = float(string_handle)
    integer_scheduled = float(integer_scheduled)
    string_scheduled = float(string_scheduled)

    report = {
        "schema": "fsm-benchmark.executive-report.v1",
        "status": "measured",
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "source": {
            "repository": "TrentBest/FSM_API",
            "commitSha": args.source_sha.lower(),
            "harnessRepository": "TrentBest/FSM_API_Benchmark",
            "harnessCommitSha": args.harness_sha.lower(),
            "workflowRunId": int(args.run_id),
            "workflowAttempt": args.run_attempt,
        },
        "environment": {
            "runtime": ".NET 10",
            "operatingSystem": platform.platform(),
            "architecture": platform.machine(),
            "processor": platform.processor() or os.environ.get("PROCESSOR_IDENTIFIER", "not reported by runner"),
        },
        "workloads": {
            "instanceUpdate": {
                "identity": "direct handle update",
                "description": "The same two-state machine update invoked directly on the instance handle; excludes processing-group scheduling and group lookup.",
            },
            "scheduledGroupUpdate": {
                "identity": "scheduled processing-group update",
                "description": "The same two-state machine advanced through the public processing-group update API; includes scheduler/group-level runtime costs.",
            },
            "baseline": {
                "identity": "application logic only",
                "description": "The same counter update and parity decision executed directly through delegates without an FSM scheduler, definition lookup, state lookup, or handle lifecycle.",
            },
        },
        "measurements": metrics,
        "derived": {
            "instanceUpdate": derive(integer_handle, string_handle, baseline),
            "scheduledGroupUpdate": derive(integer_scheduled, string_scheduled, baseline),
        },
        "interpretation": {
            "status": "measured",
            "summary": (
                f"The application-logic-only baseline averaged {baseline:.3f} ns. For a direct instance update, "
                f"integer-backed execution averaged {integer_handle:.3f} ns and string-backed execution "
                f"averaged {string_handle:.3f} ns (string difference: {string_handle - integer_handle:+.3f} ns). "
                f"Through the scheduled group API, integer-backed execution averaged {integer_scheduled:.3f} ns "
                f"and string-backed execution averaged {string_scheduled:.3f} ns "
                f"(string difference: {string_scheduled - integer_scheduled:+.3f} ns)."
            ),
            "caveat": "These are measurements of two specific public execution paths on this runner, not universal processor constants or a guarantee for every application. The scheduled path includes each API's scheduler and diagnostics implementation; the direct-handle path is the closer comparison of per-instance execution.",
        },
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote measured executive report: {args.output}")


if __name__ == "__main__":
    main()
