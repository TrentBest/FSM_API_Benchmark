import importlib.util
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "build_executive_report.py"
SPEC = importlib.util.spec_from_file_location("executive_report", SCRIPT)
REPORT = importlib.util.module_from_spec(SPEC)
assert SPEC and SPEC.loader
SPEC.loader.exec_module(REPORT)


class ExecutiveReportParserTests(unittest.TestCase):
    def write_csv(self, folder: Path, rows: list[str]) -> None:
        path = folder / "matched-report.csv"
        path.write_text(
            "Method,Mean,Error,StdDev,Median,Allocated\n" + "\n".join(rows) + "\n",
            encoding="utf-8",
        )

    def test_uses_runtime_recorded_by_benchmarkdotnet(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            (folder / "run-report-github.md").write_text(
                "BenchmarkDotNet v0.15.2\n  DefaultJob : .NET 10.0.15, X64 RyuJIT\n",
                encoding="utf-8",
            )
            runtime, _ = REPORT.dotnet_runtime_details(folder)
            self.assertEqual(runtime, ".NET 10.0.15")

    def test_rejects_inconsistent_benchmarkdotnet_runtimes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            (folder / "a-report-github.md").write_text("  DefaultJob : .NET 10.0.15\n", encoding="utf-8")
            (folder / "b-report-github.md").write_text("  DefaultJob : .NET 10.0.16\n", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "exactly one consistent"):
                REPORT.dotnet_runtime_details(folder)

    def test_converts_time_and_allocation_units(self) -> None:
        self.assertEqual(REPORT.parse_quantity("1.5 us", REPORT.TIME_UNITS_TO_NS), 1500.0)
        self.assertEqual(REPORT.parse_quantity("2,387.4 μs", REPORT.TIME_UNITS_TO_NS), 2387400.0)
        self.assertEqual(REPORT.parse_quantity("2 KB", REPORT.BYTE_UNITS), 2048.0)

    def test_requires_all_five_matched_lanes(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            self.write_csv(folder, ["ApplicationLogicBaseline,5 ns,0.2 ns,1 ns,5 ns,0 B"])
            with self.assertRaisesRegex(ValueError, "Missing matched benchmark lanes"):
                REPORT.read_measurements(folder)

    def test_rejects_duplicate_lane_instead_of_selecting_a_favorable_row(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            self.write_csv(folder, [
                "ApplicationLogicBaseline,5 ns,0.2 ns,1 ns,5 ns,0 B",
                "ApplicationLogicBaseline,4 ns,0.1 ns,0.5 ns,4 ns,0 B",
                "IntegerHandleUpdate,20 ns,1 ns,2 ns,20 ns,0 B",
                "StringHandleUpdate,30 ns,1 ns,3 ns,30 ns,0 B",
                "IntegerScheduledTick,25 ns,1 ns,2 ns,25 ns,0 B",
                "StringScheduledTick,45 ns,2 ns,4 ns,45 ns,0 B",
            ])
            with self.assertRaisesRegex(ValueError, "Duplicate benchmark result"):
                REPORT.read_measurements(folder)

    def test_rejects_subnanosecond_application_baseline(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            self.write_csv(folder, [
                "ApplicationLogicBaseline,0.5 ns,0.01 ns,0.02 ns,0.5 ns,0 B",
                "IntegerHandleUpdate,20 ns,1 ns,2 ns,20 ns,0 B",
                "StringHandleUpdate,30 ns,1 ns,3 ns,30 ns,8 B",
                "IntegerScheduledTick,25 ns,1 ns,2 ns,25 ns,0 B",
                "StringScheduledTick,45 ns,2 ns,4 ns,45 ns,8 B",
            ])
            with self.assertRaisesRegex(ValueError, "below 1 ns"):
                REPORT.read_measurements(folder)

    def test_accepts_complete_matched_run_and_preserves_each_lane(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            self.write_csv(folder, [
                "ApplicationLogicBaseline,5 ns,0.2 ns,1 ns,5 ns,0 B",
                "IntegerHandleUpdate,20 ns,1 ns,2 ns,20 ns,0 B",
                "StringHandleUpdate,30 ns,1 ns,3 ns,30 ns,8 B",
                "IntegerScheduledTick,25 ns,1 ns,2 ns,25 ns,0 B",
                "StringScheduledTick,45 ns,2 ns,4 ns,45 ns,8 B",
            ])
            values = REPORT.read_measurements(folder)
            self.assertEqual(values["integerHandle"]["meanNs"], 20.0)
            self.assertEqual(values["stringScheduled"]["allocatedBytesPerOperation"], 8.0)


if __name__ == "__main__":
    unittest.main()
