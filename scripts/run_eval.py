"""Run the Ollama-backed eval suite and record the outcome with its provenance.

    python scripts/run_eval.py            # refuses on a dirty tree
    python scripts/run_eval.py --allow-dirty

Writes results/eval_<sha8>.json: one row per test case, the totals, the model
and host from appsettings.json, the SDK version, and the commit it ran at.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
EVAL_PROJECT = ROOT / "tests" / "AiAssistant.Eval" / "AiAssistant.Eval.csproj"
SETTINGS = ROOT / "src" / "AiAssistant.API" / "appsettings.json"
TRX_NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
# Test names embed the Arabic question, which the test host can mis-encode on
# Windows; the case id and the method name are ASCII and identify the row.
CASE_ID = re.compile(r"Id = ([\w.-]+)")


def git_state(root: Path = ROOT) -> tuple[str, bool]:
    """Return (HEAD sha, worktree clean). Raises RuntimeError outside a git repo."""
    try:
        sha = subprocess.run(["git", "rev-parse", "HEAD"], cwd=root, capture_output=True,
                             text=True, check=True).stdout.strip()
        status = subprocess.run(["git", "status", "--porcelain"], cwd=root, capture_output=True,
                                text=True, check=True).stdout
    except (subprocess.CalledProcessError, FileNotFoundError) as exc:
        raise RuntimeError("not inside a git repository") from exc
    return sha, status.strip() == ""


def check_provenance(allow_dirty: bool, root: Path = ROOT) -> tuple[str, bool]:
    sha, clean = git_state(root)
    if not clean and not allow_dirty:
        raise SystemExit("worktree is dirty: commit first, or pass --allow-dirty "
                         "(the result will record worktree_clean=false)")
    return sha, clean


def parse_trx(path: Path) -> list[dict]:
    """One row per executed test: case id, check, outcome, duration in seconds."""
    rows = []
    for r in ET.parse(path).getroot().iterfind(".//t:UnitTestResult", TRX_NS):
        h, m, s = r.get("duration", "0:0:0").split(":")
        name = r.get("testName", "")
        case = CASE_ID.search(name)
        rows.append({
            "case": case.group(1) if case else None,
            "check": name.split("(", 1)[0].rsplit(".", 1)[-1],
            "outcome": r.get("outcome"),
            "seconds": round(int(h) * 3600 + int(m) * 60 + float(s), 3),
        })
    return sorted(rows, key=lambda row: (row["case"] or "", row["check"]))


def summarise(rows: list[dict]) -> dict:
    passed = sum(r["outcome"] == "Passed" for r in rows)
    return {"total": len(rows), "passed": passed, "failed": len(rows) - passed}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--allow-dirty", action="store_true")
    args = ap.parse_args(argv)

    sha, clean = check_provenance(args.allow_dirty)
    settings = json.loads(SETTINGS.read_text(encoding="utf-8"))["Ollama"]
    sdk = subprocess.run(["dotnet", "--version"], capture_output=True, text=True, check=True).stdout.strip()

    with tempfile.TemporaryDirectory() as tmp:
        started = dt.datetime.now(dt.timezone.utc)
        proc = subprocess.run(
            ["dotnet", "test", str(EVAL_PROJECT), "--logger", "trx;LogFileName=eval.trx",
             "--results-directory", tmp],
            cwd=ROOT, capture_output=True, text=True, encoding="utf-8", errors="replace")
        wall = (dt.datetime.now(dt.timezone.utc) - started).total_seconds()
        trx = Path(tmp) / "eval.trx"
        if not trx.exists():
            sys.stdout.write(proc.stdout[-4000:])
            raise SystemExit("dotnet test produced no results file")
        rows = parse_trx(trx)

    result = {
        "source_commit_sha": sha,
        "worktree_clean": clean,
        "timestamp_utc": started.isoformat(timespec="seconds"),
        "model": settings["Model"],
        "ollama_host": settings["Host"],
        "dotnet_sdk": sdk,
        "hardware": "Windows 11, 15.9 GB RAM, NVIDIA GTX 1050 Ti 4 GB (Ollama offloads part of the model)",
        "wall_clock_seconds": round(wall, 1),
        "summary": summarise(rows),
        "rows": rows,
    }
    out = ROOT / "results" / f"eval_{sha[:8]}.json"
    out.parent.mkdir(exist_ok=True)
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"{result['summary']['passed']}/{result['summary']['total']} passed -> {out.relative_to(ROOT)}")
    return 0 if proc.returncode == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
