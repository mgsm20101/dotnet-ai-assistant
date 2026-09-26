# Design — dotnet-ai-assistant

## What this project shows

How an LLM feature fits inside an ordinary .NET backend: the model and the
knowledge store are ports, the pipeline is one handler class, and the whole
thing is testable at two speeds — without the model in CI, with it locally.

## Architecture decisions

### Clean Architecture layers

Domain → Application → Infrastructure → API.

- **Domain** has zero dependencies. QueryIntent is a value object (not an enum) because
  it needs parse/factory behaviour without switching on strings throughout the codebase.

- **Application** defines interfaces (ILanguageModel, IKnowledgeStore) — these are
  *ports* in hexagonal architecture terms. Infrastructure provides the *adapters*.
  The handler (AskHandler) depends only on interfaces, making it testable without Ollama.

- **Infrastructure** contains all I/O: HTTP calls to Ollama, the in-memory knowledge store.
  The DI extension method `AddInfrastructure()` is the only place that knows concrete types.

### One handler, called directly

There is one use case, so AskController receives AskHandler from DI and calls
`Handle(request, ct)`. A mediator or command bus would add indirection without
removing any coupling that matters at this size.

### Why in-memory TF-IDF, not a real vector store?

pgvector needs PostgreSQL. Qdrant/Chroma need Docker. Neither is running here, and
standing one up buys nothing this demo is trying to show.
TF-IDF cosine similarity demonstrates the RAG retrieval contract correctly for the demo,
and the IKnowledgeStore interface is the seam — swap in pgvector by implementing the
interface, zero changes to Application or API layers.

### OllamaClient — OpenAI-compatible

Ollama exposes `/v1/chat/completions` (OpenAI format). OllamaClient uses this directly
with System.Net.Http — no OpenAI SDK dependency needed. If you switch to Azure OpenAI,
implement ILanguageModel with the Azure SDK.

## Eval strategy

Two layers:

1. **Unit and in-process API tests** (`tests/AiAssistant.UnitTests`) replace the
   language model with a scripted double. They pin the handler's routing
   (factual → retrieve, anything else → no retrieval), the prompts it builds,
   the Ollama wire contract, and the HTTP behaviour, and they run in CI.
2. **The eval** (`tests/AiAssistant.Eval`) runs the real API in-process against
   the real model and checks intent and answer content. It needs Ollama, so it
   runs locally through `scripts/run_eval.py`, which records the commit.

The split exists because each layer catches what the other cannot: the unit
layer catches a broken prompt or contract in seconds; only the eval catches a
model that misclassifies or omits the fact.
