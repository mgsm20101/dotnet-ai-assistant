using AiAssistant.Application.Contracts;
using AiAssistant.Infrastructure.Llm;
using AiAssistant.Infrastructure.Rag;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<OllamaOptions>(config.GetSection("Ollama"));
        services.AddSingleton<ILanguageModel, OllamaClient>();

        // Knowledge store seeded with the policies in Rag/SeedPolicies.cs
        var store = new InMemoryKnowledgeStore();
        foreach (var (id, text) in SeedPolicies.All)
            store.AddDocument(id, text);
        services.AddSingleton<IKnowledgeStore>(store);

        return services;
    }
}
