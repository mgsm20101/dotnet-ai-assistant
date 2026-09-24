# Evaluation results

Measured at commit `df41e7d7` on a clean tree with `gemma3:4b` through local Ollama —
raw file [`results/eval_df41e7d7.json`](results/eval_df41e7d7.json).

**10 of 10 checks passed** (5 cases × intent + content), 53 s wall clock
including model load and build (.NET SDK 10.0.302).

| case | intent | answer content |
|---|---|---|
| `calculation-1` | Passed | Passed |
| `calculation-2` | Passed | Passed |
| `factual-1` | Passed | Passed |
| `factual-2` | Passed | Passed |
| `factual-3` | Passed | Passed |

Five cases is a smoke test of the whole slice with a real model, not a quality
measurement — one case moves the pass rate by 20 points (see Limitations).
