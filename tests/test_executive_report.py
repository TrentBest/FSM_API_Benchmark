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

    def test_converts_time_and_allocation_units(self) -> None:
        self.assertEqual(REPORT.parse_quantity("1.5 us", REPORT.TIME_UNITS_TO_NS), 1500.0)
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
