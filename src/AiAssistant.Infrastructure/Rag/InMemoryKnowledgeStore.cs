using AiAssistant.Application.Contracts;

namespace AiAssistant.Infrastructure.Rag;

/// <summary>
/// TF-IDF cosine similarity knowledge store with no external dependencies.
/// Replace with a real vector store (pgvector, Chroma, Qdrant) for production.
/// </summary>
public sealed class InMemoryKnowledgeStore : IKnowledgeStore
{
    private readonly List<(string Id, string Text, Dictionary<string, float> Tfidf)> _docs = [];

    public void AddDocument(string id, string text)
    {
        var tf = ComputeTf(Tokenize(text));
        _docs.Add((id, text, tf));
    }

    public Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(
        string query, int topK = 3, CancellationToken ct = default)
    {
        var queryTf = ComputeTf(Tokenize(query));

        var scored = _docs
            .Select(doc => new KnowledgeChunk(
                doc.Id,
                doc.Text,
                CosineSimilarity(queryTf, doc.Tfidf)))
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<KnowledgeChunk>>(scored);
    }

    private static string[] Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '\t', '\n', '.', '،', '؟', '!', ':', '(', ')'], StringSplitOptions.RemoveEmptyEntries);

    private static Dictionary<string, float> ComputeTf(string[] tokens)
    {
        var counts = new Dictionary<string, int>();
        foreach (var t in tokens)
            counts[t] = counts.GetValueOrDefault(t) + 1;

        return counts.ToDictionary(kv => kv.Key, kv => (float)kv.Value / tokens.Length);
    }

    private static float CosineSimilarity(Dictionary<string, float> a, Dictionary<string, float> b)
    {
        var dot     = a.Sum(kv => kv.Value * b.GetValueOrDefault(kv.Key));
        var normA   = MathF.Sqrt(a.Values.Sum(v => v * v));
        var normB   = MathF.Sqrt(b.Values.Sum(v => v * v));
        return normA == 0 || normB == 0 ? 0f : dot / (normA * normB);
    }
}
