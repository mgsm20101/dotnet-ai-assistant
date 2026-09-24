namespace AiAssistant.Eval;

/// <summary>Single eval test case loaded from eval_set.json.</summary>
public sealed record EvalCase(
    string Id,
    string Question,
    string ExpectedIntent,
    string[] AnswerContains);
