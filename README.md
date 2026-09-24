# dotnet-ai-assistant

An ASP.NET Core 8 API that answers HR-policy questions in Arabic by routing each
question on intent, retrieving from a small knowledge store when the question is
factual, and calling a local LLM through Ollama — built as Clean Architecture +
CQRS so the model and the store sit behind ports and can be replaced without
touching the application layer.

## Problem

Most LLM demos live in notebooks. The question this project answers is narrower:
what does an LLM feature look like inside an ordinary .NET backend — with a
controller, a MediatR handler, DI, typed results — and how do you test it both
without the model (fast, in CI) and with it (slow, measured)?

## Architecture

```
POST /api/ask
    │
    ▼
AskController ── MediatR ──▶ AskHandler
                               ├─ 1. classify intent      → ILanguageModel
                               ├─ 2. if factual: retrieve → IKnowledgeStore (top 3)
                               └─ 3. answer with context  → ILanguageModel
                               ▼
                             AskResult { answer, intent, sourceChunks, elapsedMs }

Adapters (Infrastructure)
  OllamaClient            ILanguageModel   Ollama /v1/chat/completions, temperature 0
  InMemoryKnowledgeStore  IKnowledgeStore  TF-IDF cosine over five seeded policies
```

| Layer | Holds |
|---|---|
| `AiAssistant.Domain` | `QueryIntent` value object, `ChatSession` |
| `AiAssistant.Application` | ports, `Result<T>`, `AskCommand` / `AskHandler` |
| `AiAssistant.Infrastructure` | Ollama adapter, in-memory store, `AddInfrastructure()` |
| `AiAssistant.API` | controller, DI wiring |

## Run

```bash
# .NET 8 SDK or later; Ollama serving gemma3:4b on 127.0.0.1:11434
dotnet run --project src/AiAssistant.API
curl -X POST http://localhost:5000/api/ask -H "Content-Type: application/json" \
  -d '{"question": "كم يوم إجازة سنوية أستحق؟"}'
```

The model and host are read from `src/AiAssistant.API/appsettings.json`
(`Ollama:Model`, `Ollama:Host`).

## Eval

Two test projects, deliberately separate:

| Project | Needs a model | What it checks | Where it runs |
|---|---|---|---|
| `tests/AiAssistant.UnitTests` | no | intent parsing, `Result<T>`, TF-IDF ranking, the handler's routing and prompt assembly, the Ollama request/response contract against a fake HTTP handler, and the real HTTP pipeline in-process with the model replaced | CI, every push |
| `tests/AiAssistant.Eval` | yes (`gemma3:4b`) | five gold questions through the real pipeline and the real model: the classified intent, and that the answer contains the required fact | locally, via `scripts/run_eval.py` |

The eval compares answers after stripping Arabic tashkeel and tatweel from both
sides — and nothing else. The first run failed a correct answer that wrote
`بعد` where the gold text had `بُعد`; hamza and alef forms are left alone because
they can distinguish different words.

## Results

<!-- RESULTS -->

## Limitations

* **Five cases.** One case moves the pass rate by 20 points; this is a smoke
  test of the vertical slice, not a quality measurement of the model.
* **Substring assertions.** A pass means a required fact appears in the answer,
  not that the answer is complete or free of extra claims.
* **TF-IDF over five documents.** It demonstrates the retrieval port, not
  retrieval quality; the port is where a vector store would go.
* **One model, greedy decoding, one machine**, whose GPU is shared with the
  desktop, so wall-clock time is reported but is not a latency benchmark.

## What this project does not prove

* That the model answers HR questions well in general — five questions cannot.
* Anything about retrieval quality at realistic corpus sizes.
* Production readiness: there is no authentication, rate limiting, persistence
  or observability here; the point is the seams, not the deployment.

## Reproduction

```bash
dotnet build AiAssistant.sln
dotnet test tests/AiAssistant.UnitTests/AiAssistant.UnitTests.csproj   # no model needed

# with Ollama serving gemma3:4b, from a clean checkout:
python scripts/run_eval.py      # writes results/eval_<sha8>.json
```

`run_eval.py` refuses to run on a dirty tree (unless `--allow-dirty`, which is
recorded) and stores the commit it ran at, so every number above points at the
exact code that produced it.

Machine for the recorded run: Windows 11, 15.9 GB RAM, NVIDIA GTX 1050 Ti 4 GB;
Ollama offloads part of the model to the GPU.

## License

MIT. `gemma3:4b` is used under the Gemma terms of use and is not redistributed here.
