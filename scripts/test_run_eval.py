import subprocess
from pathlib import Path

import pytest

import run_eval

TRX = """<?xml version="1.0" encoding="utf-8"?>
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <Results>
    <UnitTestResult testName="Ask_ReturnsExpectedIntent(factual-1)" outcome="Passed" duration="00:00:12.5000000" />
    <UnitTestResult testName="Ask_AnswerContainsExpectedContent(factual-2)" outcome="Failed" duration="00:01:02.2500000" />
  </Results>
</TestRun>
"""


def test_parse_trx_reads_every_result_with_its_duration(tmp_path: Path):
    trx = tmp_path / "eval.trx"
    trx.write_text(TRX, encoding="utf-8")

    rows = run_eval.parse_trx(trx)

    assert rows == [
        {"test": "Ask_AnswerContainsExpectedContent(factual-2)", "outcome": "Failed", "seconds": 62.25},
        {"test": "Ask_ReturnsExpectedIntent(factual-1)", "outcome": "Passed", "seconds": 12.5},
    ]


def test_summarise_counts_anything_but_passed_as_failed():
    rows = [{"outcome": "Passed"}, {"outcome": "Failed"}, {"outcome": "NotExecuted"}]

    assert run_eval.summarise(rows) == {"total": 3, "passed": 1, "failed": 2}


def _fake_git(status: str):
    def run(cmd, **_):
        out = "a" * 40 + "\n" if cmd[1] == "rev-parse" else status
        return subprocess.CompletedProcess(cmd, 0, stdout=out, stderr="")
    return run


def test_dirty_tree_is_refused(monkeypatch):
    monkeypatch.setattr(run_eval.subprocess, "run", _fake_git(" M README.md\n"))

    with pytest.raises(SystemExit, match="dirty"):
        run_eval.check_provenance(allow_dirty=False)


def test_dirty_tree_is_recorded_when_allowed(monkeypatch):
    monkeypatch.setattr(run_eval.subprocess, "run", _fake_git(" M README.md\n"))

    assert run_eval.check_provenance(allow_dirty=True) == ("a" * 40, False)


def test_clean_tree_passes(monkeypatch):
    monkeypatch.setattr(run_eval.subprocess, "run", _fake_git(""))

    assert run_eval.check_provenance(allow_dirty=False) == ("a" * 40, True)


def test_outside_git_is_an_error(monkeypatch):
    def fail(cmd, **_):
        raise subprocess.CalledProcessError(128, cmd)
    monkeypatch.setattr(run_eval.subprocess, "run", fail)

    with pytest.raises(RuntimeError, match="not inside a git repository"):
        run_eval.check_provenance(allow_dirty=True)
