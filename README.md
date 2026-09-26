# dotnet-ai-assistant

An ASP.NET Core 8 API that answers HR-policy questions in Arabic by routing each
question on intent, retrieving from a small knowledge store when the question is
factual, and calling a local LLM through Ollama. The model and the store sit
behind interfaces (ports), so either can be replaced without touching the
application layer.

## Problem

Most LLM demos live in notebooks. The question this project answers is narrower:
what does an LLM feature look like inside an ordinary .NET backend — with a
controller, a handler, DI, typed results — and how do you test it both
without the model (fast, in CI) and with it (slow, measured)?

## Structure

### Entry points

| Command | Reads | Writes |
|---|---|---|
| `python scripts/run_eval.py` — the measured run | git HEAD and status, `src/AiAssistant.API/appsettings.json`, `tests/AiAssistant.Eval/eval_set.json`, a local Ollama serving `gemma3:4b` | `results/eval_<sha8>.json` |
| `dotnet run --project src/AiAssistant.API` — the API (entry: `src/AiAssistant.API/Program.cs`) | `src/AiAssistant.API/appsettings.json` (`Ollama:Host`, `Ollama:Model`), the Ollama server | `POST /api/ask` responses; nothing on disk |
| `dotnet test tests/AiAssistant.UnitTests/AiAssistant.UnitTests.csproj` — model-free tests, what CI runs | nothing external (the model is replaced by a scripted double) | build output only |
| `python -m pytest -q scripts` — tests for the eval runner, also in CI | `scripts/run_eval.py` | nothing |

### Request flow

```
POST /api/ask  { "question": "..." }
  → AskController.Ask                 src/AiAssistant.API/Controllers/AskController.cs
  → AskHandler.Handle                 src/AiAssistant.Application/Features/Ask/AskHandler.cs
      1. classify intent   ILanguageModel  → OllamaClient            src/AiAssistant.Infrastructure/Llm/OllamaClient.cs
      2. if factual: top 3 IKnowledgeStore → InMemoryKnowledgeStore  src/AiAssistant.Infrastructure/Rag/InMemoryKnowledgeStore.cs
      3. answer            ILanguageModel  → OllamaClient
  ← AskResult JSON                    src/AiAssistant.Application/Features/Ask/AskResult.cs
```

A blank `question` returns 400 before the model is called.

### Why four projects

The four-project layout is a deliberate demonstration of a team-scale Clean
Architecture layout, not something an app this size needs — one project would
run the same code. The dependency rule is enforced by project references rather
than convention: `AiAssistant.Domain` references nothing, `AiAssistant.Application`
references only Domain (never Infrastructure), and only `AiAssistant.Infrastructure`
knows the concrete Ollama and store types. Every type in every layer is used by
the request path above.

### Code map

| File | What it is |
|---|---|
| `src/AiAssistant.API/Program.cs` | Entry point: registers controllers, Swagger, `AskHandler`, `AddInfrastructure()` |
| `src/AiAssistant.API/Controllers/AskController.cs` | `POST /api/ask`: rejects a blank question, calls `AskHandler.Handle` |
| `src/AiAssistant.API/appsettings.json` | Ollama host, model and timeout; logging |
| `src/AiAssistant.API/AiAssistant.API.csproj` | Web project; references Infrastructure |
| `src/AiAssistant.Application/Features/Ask/AskHandler.cs` | The pipeline: classify, retrieve if factual, answer; both prompts live here |
| `src/AiAssistant.Application/Features/Ask/AskRequest.cs` | Request body `{ question }` |
| `src/AiAssistant.Application/Features/Ask/AskResult.cs` | Response body: answer, intent, sourceChunks, stoppedBy, iterations, elapsedMs |
| `src/AiAssistant.Application/Contracts/ILanguageModel.cs` | Port for the LLM, plus `LlmMessage` / `LlmResponse` |
| `src/AiAssistant.Application/Contracts/IKnowledgeStore.cs` | Port for retrieval, plus `KnowledgeChunk` |
| `src/AiAssistant.Application/AiAssistant.Application.csproj` | References Domain only |
| `src/AiAssistant.Domain/ValueObjects/QueryIntent.cs` | `factual` / `calculation` / `unknown`, parsed from the classifier's reply |
| `src/AiAssistant.Domain/AiAssistant.Domain.csproj` | No references |
| `src/AiAssistant.Infrastructure/DependencyInjection.cs` | `AddInfrastructure()`: the only place that names the concrete adapters |
| `src/AiAssistant.Infrastructure/Llm/OllamaClient.cs` | `ILanguageModel` over Ollama's `/v1/chat/completions`, temperature 0 |
| `src/AiAssistant.Infrastructure/Rag/InMemoryKnowledgeStore.cs` | `IKnowledgeStore`: TF-IDF cosine similarity in memory |
| `src/AiAssistant.Infrastructure/Rag/SeedPolicies.cs` | The five HR policies loaded at startup (`policy-leave`, ...) |
| `src/AiAssistant.Infrastructure/AiAssistant.Infrastructure.csproj` | References Application |
| `tests/AiAssistant.UnitTests/AskEndpointTests.cs` | Real HTTP pipeline in-process, `ILanguageModel` swapped for a double |
| `tests/AiAssistant.UnitTests/AskHandlerTests.cs` | Routing and prompt assembly of `AskHandler` |
| `tests/AiAssistant.UnitTests/KnowledgeStoreTests.cs` | TF-IDF ranking, top-k, zero-score edge cases |
| `tests/AiAssistant.UnitTests/OllamaClientTests.cs` | Ollama request/response contract against a fake HTTP handler |
| `tests/AiAssistant.UnitTests/QueryIntentTests.cs` | Intent parsing |
| `tests/AiAssistant.UnitTests/Fakes.cs` | Test doubles: scripted model, fixed store, capturing HTTP handler |
| `tests/AiAssistant.UnitTests/AiAssistant.UnitTests.csproj` | xUnit project; references the API |
| `tests/AiAssistant.Eval/EvalTests.cs` | Real-model eval: `Ask_ReturnsExpectedIntent`, `Ask_AnswerContainsExpectedContent` |
| `tests/AiAssistant.Eval/EvalCase.cs` | One eval case as loaded from JSON |
| `tests/AiAssistant.Eval/eval_set.json` | The five gold questions |
| `tests/AiAssistant.Eval/AiAssistant.Eval.csproj` | xUnit project; references the API |
| `scripts/run_eval.py` | Runs the eval and records commit, SDK, model and outcomes |
| `scripts/test_run_eval.py` | Tests for the runner (TRX parsing, dirty-tree refusal) |
| `results/eval_df41e7d7.json` | The recorded eval run |
| `docs/DESIGN.md` | Design decisions: ports, TF-IDF, OpenAI-compatible endpoint, eval strategy |
| `.github/workflows/tests.yml` | CI: model-free .NET tests and the runner tests |
| `AiAssistant.sln` | Solution containing the six projects |
| `README.md`, `LICENSE`, `.gitignore`, `.gitattributes` | Repository metadata |

### Read the code in this order

1. `src/AiAssistant.API/Program.cs`
2. `src/AiAssistant.API/Controllers/AskController.cs`
3. `src/AiAssistant.Application/Features/Ask/AskHandler.cs`
4. `src/AiAssistant.Application/Contracts/ILanguageModel.cs`, then `src/AiAssistant.Application/Contracts/IKnowledgeStore.cs`
5. `src/AiAssistant.Infrastructure/Llm/OllamaClient.cs`, then `src/AiAssistant.Infrastructure/Rag/InMemoryKnowledgeStore.cs`

## Architecture

```
POST /api/ask
    │
    ▼
AskController ──▶ AskHandler
                    ├─ 1. classify intent      → ILanguageModel
                    ├─ 2. if factual: retrieve → IKnowledgeStore (top 3)
                    └─ 3. answer with context  → ILanguageModel
                    ▼
                  AskResult { answer, intent, sourceChunks, stoppedBy, iterations, elapsedMs }

Adapters (Infrastructure)
  OllamaClient            ILanguageModel   Ollama /v1/chat/completions, temperature 0
  InMemoryKnowledgeStore  IKnowledgeStore  TF-IDF cosine over five seeded policies
```

`stoppedBy` is always `"answer"` and `iterations` always `1`: the pipeline is a
single pass, and the fields stay because they are part of the JSON contract the
eval reads.

| Layer | Holds |
|---|---|
| `AiAssistant.Domain` | `QueryIntent` value object |
| `AiAssistant.Application` | ports (`ILanguageModel`, `IKnowledgeStore`), `AskRequest` / `AskHandler` / `AskResult` |
| `AiAssistant.Infrastructure` | Ollama adapter, in-memory store, seed policies, `AddInfrastructure()` |
| `AiAssistant.API` | `Program.cs`, `AskController` |

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
| `tests/AiAssistant.UnitTests` | no | intent parsing, TF-IDF ranking, the handler's routing and prompt assembly, the Ollama request/response contract against a fake HTTP handler, and the real HTTP pipeline in-process with the model replaced | CI, every push |
| `tests/AiAssistant.Eval` | yes (`gemma3:4b`) | five gold questions through the real pipeline and the real model: the classified intent, and that the answer contains the required fact | locally, via `scripts/run_eval.py` |

The eval compares answers after stripping Arabic tashkeel and tatweel from both
sides — and nothing else. The first run failed a correct answer that wrote
`بعد` where the gold text had `بُعد`; hamza and alef forms are left alone because
they can distinguish different words.

## Results

Measured at commit `df41e7d7` on a clean tree with `gemma3:4b` through local Ollama —
raw file [`results/eval_df41e7d7.json`](results/eval_df41e7d7.json).

That run predates a cleanup that removed unused types and the mediator between
controller and handler, and moved the seed policies into their own file. The
cleanup did not change what the eval exercises: the route (`POST /api/ask`), the request and response JSON fields,
the prompts, the seed policies, classification and TF-IDF retrieval are the
same, and the model-free tests pass on the cleaned-up code. The number has not
been re-measured on the later commits.

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
dotnet test tests/AiAssistant.UnitTests/AiAssistant.UnitTests.csproj   # no model needed
python -m pytest -q scripts                                            # runner tests

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
