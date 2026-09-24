using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AiAssistant.Application.Contracts;
using AiAssistant.Application.Features.Ask;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiAssistant.UnitTests;

/// <summary>The real HTTP pipeline in-process, with the language model replaced so no Ollama is needed.</summary>
public sealed class AskEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private HttpClient ClientWith(ILanguageModel llm) =>
        factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ILanguageModel>();
            s.AddSingleton(llm);
        })).CreateClient();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankQuestion_IsRejectedWithoutCallingTheModel(string question)
    {
        var llm = new ScriptedLanguageModel();

        var response = await ClientWith(llm).PostAsJsonAsync("/api/ask", new { question });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(llm.Calls);
    }

    [Fact]
    public async Task FactualQuestion_IsAnsweredFromTheSeededPolicies()
    {
        var llm = new ScriptedLanguageModel("factual", "21 يوماً");

        var response = await ClientWith(llm).PostAsJsonAsync("/api/ask", new { question = "كم يوم إجازة سنوية أستحق؟" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AskResult>(Json);
        Assert.NotNull(result);
        Assert.Equal("factual", result.Intent);
        Assert.Equal("21 يوماً", result.Answer);
        Assert.Contains("policy-leave", result.SourceChunks);
        Assert.Contains("21 يوم إجازة سنوية", llm.Calls[1][0].Content);
    }

    [Fact]
    public async Task MissingBody_IsABadRequest()
    {
        var client = ClientWith(new ScriptedLanguageModel());

        var response = await client.PostAsync("/api/ask",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

internal static class ServiceCollectionTestExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList())
            services.Remove(d);
    }
}
