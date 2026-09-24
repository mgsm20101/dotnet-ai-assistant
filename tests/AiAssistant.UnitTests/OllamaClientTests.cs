using System.Net;
using System.Text.Json;
using AiAssistant.Application.Contracts;
using AiAssistant.Infrastructure.Llm;
using Microsoft.Extensions.Options;
using Xunit;

namespace AiAssistant.UnitTests;

public sealed class OllamaClientTests
{
    private const string Reply = """
        {"choices":[{"message":{"role":"assistant","content":"مرحبا"}}],
         "usage":{"prompt_tokens":17,"completion_tokens":3}}
        """;

    private static OllamaClient Client(HttpMessageHandler handler, string host = "http://ollama.test:11434") =>
        new(Options.Create(new OllamaOptions { Host = host, Model = "test-model" }), handler);

    [Fact]
    public async Task Complete_PostsAGreedyNonStreamingChatRequest()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, Reply);

        await Client(handler).CompleteAsync([new LlmMessage("system", "s"), new LlmMessage("user", "u")]);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("http://ollama.test:11434/v1/chat/completions", handler.Request.RequestUri!.ToString());

        using var body = JsonDocument.Parse(handler.RequestBody!);
        var root = body.RootElement;
        Assert.Equal("test-model", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        Assert.Equal(0, root.GetProperty("options").GetProperty("temperature").GetInt32());
        var messages = root.GetProperty("messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("u", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Complete_ParsesContentAndTokenUsage()
    {
        var response = await Client(new CapturingHandler(HttpStatusCode.OK, Reply))
            .CompleteAsync([new LlmMessage("user", "u")]);

        Assert.Equal(new LlmResponse("مرحبا", 17, 3), response);
    }

    [Fact]
    public async Task Complete_ToleratesATrailingSlashInTheHost()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, Reply);

        await Client(handler, "http://ollama.test:11434/").CompleteAsync([new LlmMessage("user", "u")]);

        Assert.Equal("/v1/chat/completions", handler.Request!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Complete_TreatsANullContentAsEmpty()
    {
        const string nullContent = """
            {"choices":[{"message":{"role":"assistant","content":null}}],
             "usage":{"prompt_tokens":1,"completion_tokens":0}}
            """;

        var response = await Client(new CapturingHandler(HttpStatusCode.OK, nullContent))
            .CompleteAsync([new LlmMessage("user", "u")]);

        Assert.Equal(string.Empty, response.Content);
    }

    [Fact]
    public async Task Complete_ThrowsOnAnErrorStatus()
    {
        var client = Client(new CapturingHandler(HttpStatusCode.NotFound, """{"error":"model not found"}"""));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.CompleteAsync([new LlmMessage("user", "u")]));
    }
}
