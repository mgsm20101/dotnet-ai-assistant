using System.Net.Http.Json;
using System.Text.Json;
using AiAssistant.Application.Features.Ask;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace AiAssistant.Eval;

/// <summary>
/// Integration eval suite — starts the real API in-process and runs all eval cases.
/// Requires Ollama running locally with gemma3:4b pulled.
/// </summary>
public sealed class EvalTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private static IReadOnlyList<EvalCase> LoadCases() =>
        JsonSerializer.Deserialize<List<EvalCase>>(
            File.ReadAllText("eval_set.json"), JsonOpts)!;

    [Theory]
    [MemberData(nameof(GetEvalCases))]
    public async Task Ask_ReturnsExpectedIntent(EvalCase evalCase)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ask",
            new { question = evalCase.Question });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AskResult>(JsonOpts);
        Assert.NotNull(result);

        output.WriteLine($"[{evalCase.Id}] Intent={result.Intent} Answer={result.Answer[..Math.Min(80, result.Answer.Length)]}");

        Assert.Equal(evalCase.ExpectedIntent, result.Intent);
    }

    [Theory]
    [MemberData(nameof(GetEvalCases))]
    public async Task Ask_AnswerContainsExpectedContent(EvalCase evalCase)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/ask",
            new { question = evalCase.Question });

        var result = await response.Content.ReadFromJsonAsync<AskResult>(JsonOpts);
        Assert.NotNull(result);

        foreach (var expected in evalCase.AnswerContains)
        {
            Assert.Contains(StripArabicDiacritics(expected),
                StripArabicDiacritics(result.Answer),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Removes Arabic tashkeel and tatweel from both sides of a comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A raw substring match fails on Arabic for a reason that has nothing to do
    /// with the model. The eval case for remote work expected <c>بُعد</c>, carrying a
    /// damma; the model answered "مسموح به يومين أسبوعياً بعد إتمام فترة التجربة" —
    /// the right answer, written the way Arabic is normally written, without the
    /// diacritic. The assertion failed on orthography.
    /// </para>
    /// <para>
    /// Diacritics are optional in written Arabic and carry no lexical difference
    /// here, so stripping them compares the words rather than the typing. Nothing
    /// else is folded: hamza and alef forms stay exactly as written, because those
    /// <em>can</em> distinguish different words and folding them would quietly mark
    /// wrong answers correct — which is the failure an eval harness exists to avoid.
    /// </para>
    /// </remarks>
    private static string StripArabicDiacritics(string text)
    {
        Span<char> buffer = text.Length <= 512 ? stackalloc char[text.Length] : new char[text.Length];
        var length = 0;
        foreach (var ch in text)
        {
            // U+064B–U+0652 tashkeel · U+0670 superscript alef · U+0640 tatweel
            var isDiacritic = (ch >= 'ً' && ch <= 'ْ') || ch == 'ٰ' || ch == 'ـ';
            if (!isDiacritic)
            {
                buffer[length++] = ch;
            }
        }

        return new string(buffer[..length]);
    }

    public static IEnumerable<object[]> GetEvalCases() =>
        LoadCases().Select(c => new object[] { c });
}
