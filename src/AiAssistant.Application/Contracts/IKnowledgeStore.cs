namespace AiAssistant.Application.Contracts;

public record KnowledgeChunk(string Id, string Text, float Score);

/// <summary>Port for RAG retrieval — any vector store behind this interface.</summary>
public interface IKnowledgeStore
{
    Task<IReadOnlyList<KnowledgeChunk>> SearchAsync(
        string query,
        int topK = 3,
        CancellationToken ct = default);
}
