namespace AiAssistant.Application.Contracts;

public record LlmMessage(string Role, string Content);

public record LlmResponse(string Content, int InputTokens, int OutputTokens);

/// <summary>Port for any LLM backend (Ollama, OpenAI, Azure).</summary>
public interface ILanguageModel
{
    Task<LlmResponse> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken ct = default);
}
