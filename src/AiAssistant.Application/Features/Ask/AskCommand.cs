using MediatR;

namespace AiAssistant.Application.Features.Ask;

/// <summary>CQRS command: user asks a question, gets an answer + trace.</summary>
public sealed record AskCommand(string Question)
    : IRequest<AskResult>;

public sealed record AskResult(
    string Answer,
    string Intent,
    IReadOnlyList<string> SourceChunks,
    string StoppedBy,
    int Iterations,
    long ElapsedMs);
