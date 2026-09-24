using AiAssistant.Application.Contracts;
using AiAssistant.Application.Features.Ask;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiAssistant.UnitTests;

public sealed class AskHandlerTests
{
    private static readonly KnowledgeChunk Leave = new("policy-leave", "21 يوم إجازة سنوية", 0.9f);
    private static readonly KnowledgeChunk Remote = new("policy-remote", "يومين أسبوعياً", 0.4f);

    private static AskHandler Handler(ILanguageModel llm, IKnowledgeStore store) =>
        new(llm, store, NullLogger<AskHandler>.Instance);

    [Fact]
    public async Task Factual_RetrievesAndPutsTheChunksInTheSystemPrompt()
    {
        var llm = new ScriptedLanguageModel("factual", "  21 يوماً  ");
        var store = new FixedKnowledgeStore(Leave, Remote);

        var result = await Handler(llm, store).Handle(new AskCommand("كم يوم إجازة؟"), default);

        Assert.Equal("factual", result.Intent);
        Assert.Equal("21 يوماً", result.Answer);
        Assert.Equal(["policy-leave", "policy-remote"], result.SourceChunks);
        Assert.Equal(["كم يوم إجازة؟"], store.Queries);

        var answerPrompt = llm.Calls[1][0];
        Assert.Equal("system", answerPrompt.Role);
        Assert.Contains("21 يوم إجازة سنوية", answerPrompt.Content);
        Assert.Contains("يومين أسبوعياً", answerPrompt.Content);
    }

    [Fact]
    public async Task Calculation_SkipsRetrievalAndSendsNoContext()
    {
        var llm = new ScriptedLanguageModel("calculation", "480");
        var store = new FixedKnowledgeStore(Leave);

        var result = await Handler(llm, store).Handle(new AskCommand("15% من 3200؟"), default);

        Assert.Equal("calculation", result.Intent);
        Assert.Equal("480", result.Answer);
        Assert.Empty(result.SourceChunks);
        Assert.Empty(store.Queries);
        Assert.DoesNotContain("21 يوم", llm.Calls[1][0].Content);
    }

    [Fact]
    public async Task UnparseableClassification_IsUnknownAndSkipsRetrieval()
    {
        var llm = new ScriptedLanguageModel("I think it is factual.", "answer");
        var store = new FixedKnowledgeStore(Leave);

        var result = await Handler(llm, store).Handle(new AskCommand("q"), default);

        Assert.Equal("unknown", result.Intent);
        Assert.Empty(store.Queries);
    }

    [Fact]
    public async Task Classification_ToleratesWhitespaceAndCase()
    {
        var llm = new ScriptedLanguageModel("  Factual\n", "a");

        var result = await Handler(llm, new FixedKnowledgeStore(Leave)).Handle(new AskCommand("q"), default);

        Assert.Equal("factual", result.Intent);
    }

    [Fact]
    public async Task Classifier_IsAskedForOneWordAndReceivesTheQuestion()
    {
        var llm = new ScriptedLanguageModel("calculation", "12");

        await Handler(llm, new FixedKnowledgeStore()).Handle(new AskCommand("144 / 12"), default);

        var classify = llm.Calls[0];
        Assert.Equal(2, llm.Calls.Count);
        Assert.Contains("one word", classify[0].Content);
        Assert.Equal(new LlmMessage("user", "144 / 12"), classify[1]);
    }

    [Fact]
    public async Task Result_ReportsASingleAnswerIteration()
    {
        var result = await Handler(new ScriptedLanguageModel("calculation", "1"), new FixedKnowledgeStore())
            .Handle(new AskCommand("q"), default);

        Assert.Equal("answer", result.StoppedBy);
        Assert.Equal(1, result.Iterations);
        Assert.True(result.ElapsedMs >= 0);
    }
}
