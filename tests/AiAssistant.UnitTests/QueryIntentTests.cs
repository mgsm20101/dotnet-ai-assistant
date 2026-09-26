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
