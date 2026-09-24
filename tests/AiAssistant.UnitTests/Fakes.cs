using System.Net;
using AiAssistant.Application.Contracts;

namespace AiAssistant.UnitTests;

/// <summary>Language model double that replays scripted replies and records every call.</summary>
internal sealed class ScriptedLanguageModel(params string[] replies) : ILanguageModel
{
    private readonly Queue<string> _replies = new(replies);

    public List<IReadOnlyList<LlmMessage>> Calls { get; } = [];

    public Task<LlmResponse> CompleteAsync(IReadOnlyList<LlmMessage> messages, CancellationToken ct = default)
    {
        Calls.Add(messages);
        var reply = _replies.Count > 0 ? _replies.Dequeue() : string.Empty;
        return Task.FromResult(new LlmResponse(reply, InputTokens: 0, OutputTokens: 0));
    }
}

/// <summary>Knowledge store double that returns fixed chunks and records queries.</summary>
internal sealed class FixedKnowledgeStore(params KnowledgeChunk[] chunks) : IKnowledgeStore
{
    public List<string> Queries { get; } = [];

    public Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(string query, int topK = 3, CancellationToken ct = default)
    {
        Queries.Add(query);
        return Task.FromResult<IReadOnlyList<KnowledgeChunk>>(chunks.Take(topK).ToList());
    }
}

/// <summary>HTTP handler double that captures the request and answers with a canned response.</summary>
internal sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }
    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Request = request;
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        return new HttpResponseMessage(status) { Content = new StringContent(body) };
    }
}
