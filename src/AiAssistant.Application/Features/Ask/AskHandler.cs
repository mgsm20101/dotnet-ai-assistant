using System.Diagnostics;
using System.Text;
using AiAssistant.Application.Contracts;
using AiAssistant.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AiAssistant.Application.Features.Ask;

/// <summary>
/// Orchestrates the RAG + Intent routing pipeline:
///   1. Classify intent (factual vs calculation)
///   2. Retrieve relevant chunks from knowledge store (for factual)
///   3. Call LLM with augmented context
///   4. Return answer + trace
/// </summary>
public sealed class AskHandler(
    ILanguageModel llm,
    IKnowledgeStore knowledge,
    ILogger<AskHandler> logger)
{
    public async Task<AskResult> Handle(AskRequest request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var intent = await ClassifyIntentAsync(request.Question, ct);
        logger.LogInformation("Intent={Intent} for Q={Question}", intent, request.Question);

        var chunks = intent == QueryIntent.Factual
            ? await knowledge.SearchAsync(request.Question, topK: 3, ct)
            : [];

        var answer = await GenerateAnswerAsync(request.Question, intent, chunks, ct);
        sw.Stop();

        return new AskResult(
            Answer:       answer,
            Intent:       intent.Value,
            SourceChunks: chunks.Select(c => c.Id).ToList(),
            StoppedBy:    "answer",
            Iterations:   1,
            ElapsedMs:    sw.ElapsedMilliseconds);
    }

    private async Task<QueryIntent> ClassifyIntentAsync(string question, CancellationToken ct)
    {
        var messages = new List<LlmMessage>
        {
            new("system",
                "Classify the user question as exactly one of: factual, calculation. " +
                "Reply with one word only, no punctuation."),
            new("user", question),
        };
        var response = await llm.CompleteAsync(messages, ct);
        return QueryIntent.From(response.Content.Trim().ToLowerInvariant());
    }

    private async Task<string> GenerateAnswerAsync(
        string question,
        QueryIntent intent,
        IReadOnlyList<KnowledgeChunk> chunks,
        CancellationToken ct)
    {
        var systemPrompt = new StringBuilder();
        systemPrompt.Append("أنت مساعد ذكي يُجيب بالعربية بشكل موجز ودقيق.");

        if (chunks.Count > 0)
        {
            systemPrompt.Append("\n\nالمعلومات المتاحة:\n");
            foreach (var chunk in chunks)
                systemPrompt.Append($"- {chunk.Text}\n");
            systemPrompt.Append("\nاستخدم هذه المعلومات فقط للإجابة.");
        }

        var messages = new List<LlmMessage>
        {
            new("system", systemPrompt.ToString()),
            new("user", question),
        };
        var response = await llm.CompleteAsync(messages, ct);
        return response.Content.Trim();
    }
}
