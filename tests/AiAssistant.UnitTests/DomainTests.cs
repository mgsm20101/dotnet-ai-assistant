using AiAssistant.Application.Common;
using AiAssistant.Domain.Entities;
using AiAssistant.Domain.ValueObjects;
using Xunit;

namespace AiAssistant.UnitTests;

public sealed class QueryIntentTests
{
    [Theory]
    [InlineData("factual")]
    [InlineData("FACTUAL")]
    [InlineData("Factual")]
    public void From_ParsesFactualCaseInsensitively(string raw) =>
        Assert.Same(QueryIntent.Factual, QueryIntent.From(raw));

    [Fact]
    public void From_ParsesCalculation() =>
        Assert.Same(QueryIntent.Calculation, QueryIntent.From("calculation"));

    [Theory]
    [InlineData("")]
    [InlineData("factual.")]
    [InlineData("the question is factual")]
    [InlineData("حقيقة")]
    public void From_FallsBackToUnknownForAnythingElse(string raw) =>
        Assert.Same(QueryIntent.Unknown, QueryIntent.From(raw));

    [Fact]
    public void ToString_ReturnsTheWireValue() =>
        Assert.Equal("calculation", QueryIntent.Calculation.ToString());
}

public sealed class ResultTests
{
    [Fact]
    public void Ok_IsSuccessAndCarriesTheValue()
    {
        var result = Result<int>.Ok(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Fail_IsNotSuccessAndCarriesTheError()
    {
        var result = Result<int>.Fail("not found");

        Assert.False(result.IsSuccess);
        Assert.Equal("not found", result.Error);
    }

    [Fact]
    public void Map_TransformsASuccess()
    {
        var mapped = Result<int>.Ok(20).Map(v => v * 2);

        Assert.True(mapped.IsSuccess);
        Assert.Equal(40, mapped.Value);
    }

    [Fact]
    public void Map_PassesAFailureThroughWithoutCallingTheFunction()
    {
        var called = false;

        var mapped = Result<int>.Fail("boom").Map(v => { called = true; return v.ToString(); });

        Assert.False(mapped.IsSuccess);
        Assert.Equal("boom", mapped.Error);
        Assert.False(called);
    }
}

public sealed class ChatSessionTests
{
    [Fact]
    public void AddTurn_AppendsInOrder()
    {
        var session = new ChatSession();

        session.AddTurn("user", "q");
        session.AddTurn("assistant", "a");

        Assert.Collection(session.Turns,
            t => Assert.Equal(("user", "q"), (t.Role, t.Content)),
            t => Assert.Equal(("assistant", "a"), (t.Role, t.Content)));
    }

    [Fact]
    public void Sessions_GetDistinctIds() =>
        Assert.NotEqual(new ChatSession().Id, new ChatSession().Id);
}
