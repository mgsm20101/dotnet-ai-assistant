namespace AiAssistant.Application.Features.Ask;

/// <summary>Body of <c>POST /api/ask</c>: <c>{ "question": "..." }</c>.</summary>
public sealed record AskRequest(string Question);
