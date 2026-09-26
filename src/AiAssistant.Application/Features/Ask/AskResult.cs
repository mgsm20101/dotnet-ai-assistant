namespace AiAssistant.Application.Features.Ask;

/// <summary>
/// Response of <c>POST /api/ask</c>. The field names are the JSON contract the eval
/// in <c>tests/AiAssistant.Eval</c> reads; <see cref="StoppedBy"/> and
/// <see cref="Iterations"/> are always <c>"answer"</c> and <c>1</c> for this one-pass pipeline.
/// </summary>
public sealed record AskResult(
    string Answer,
    string Intent,
    IReadOnlyList<string> SourceChunks,
    string StoppedBy,
    int Iterations,
    long ElapsedMs);
