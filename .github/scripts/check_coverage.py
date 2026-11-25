"""Fails when the coverage in a ReportGenerator JSON summary is below MIN_LINE_COVERAGE or MIN_BRANCH_COVERAGE (percent)."""
import json
import os
import sys

summary = json.load(open(sys.argv[1], encoding="utf-8"))["summary"]
checks = [
    ("Line", summary["linecoverage"], float(os.environ["MIN_LINE_COVERAGE"])),
    ("Branch", summary["branchcoverage"], float(os.environ["MIN_BRANCH_COVERAGE"])),
]

failed = False
for name, actual, minimum in checks:
    ok = actual >= minimum
    failed |= not ok
    print(f"{name} coverage {actual}% (minimum {minimum}%): {'ok' if ok else 'TOO LOW'}")

sys.exit(1 if failed else 0)
