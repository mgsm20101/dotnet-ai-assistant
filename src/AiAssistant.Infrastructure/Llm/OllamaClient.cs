using System.Net.Http.Json;
using System.Text.Json;
using AiAssistant.Application.Contracts;
using Microsoft.Extensions.Options;

namespace AiAssistant.Infrastructure.Llm;

public sealed class OllamaOptions
{
    public string Host  { get; init; } = "http://127.0.0.1:11434";
    public string Model { get; init; } = "gemma3:4b";
    public int    TimeoutSeconds { get; init; } = 300;
}

/// <summary>ILanguageModel adapter for Ollama's OpenAI-compatible /v1/chat/completions.</summary>
public sealed class OllamaClient : ILanguageModel, IDisposable
{
    private readonly HttpClient _http;
    private readonly OllamaOptions _opts;

    public OllamaClient(IOptions<OllamaOptions> opts)
        : this(opts, new HttpClientHandler())
    {
    }

    internal OllamaClient(IOptions<OllamaOptions> opts, HttpMessageHandler handler)
    {
        _opts = opts.Value;
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(_opts.Host.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(_opts.TimeoutSeconds),
        };
    }

    public async Task<LlmResponse> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        CancellationToken ct = default)
    {
        var payload = new
        {
            model = _opts.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            stream = false,
            options = new { temperature = 0 },
        };

        var response = await _http.PostAsJsonAsync("v1/chat/completions", payload, ct);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        var root   = doc.RootElement;
        var choice = root.GetProperty("choices")[0].GetProperty("message");
        var content = choice.GetProperty("content").GetString() ?? string.Empty;
        var usage  = root.GetProperty("usage");

        return new LlmResponse(
            Content:      content,
            InputTokens:  usage.GetProperty("prompt_tokens").GetInt32(),
            OutputTokens: usage.GetProperty("completion_tokens").GetInt32());
    }

    public void Dispose() => _http.Dispose();
}
