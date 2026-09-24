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

        // Bootstrap knowledge store with seed documents
        var store = new InMemoryKnowledgeStore();
        SeedKnowledge(store);
        services.AddSingleton<IKnowledgeStore>(store);

        return services;
    }

    private static void SeedKnowledge(InMemoryKnowledgeStore store)
    {
        store.AddDocument("policy-leave",
            "سياسة الإجازات: يحق للموظف 21 يوم إجازة سنوية بعد سنة خدمة. " +
            "تزداد إلى 28 يوم بعد 5 سنوات.");

        store.AddDocument("policy-remote",
            "العمل عن بُعد: مسموح يومين أسبوعياً بعد إتمام فترة الاختبار (3 أشهر). " +
            "يتطلب موافقة المدير المباشر.");

        store.AddDocument("policy-expenses",
            "المصروفات: تُقدَّم طلبات استرداد المصروفات بنهاية كل شهر. " +
            "الحد الأقصى للوجبات 150 ريال يومياً في السفر.");

        store.AddDocument("policy-sick-leave",
            "الإجازة المرضية: 30 يوم مدفوعة الأجر بشكل كامل. " +
            "ما فوق 30 يوم يُحتسب نصف الراتب حتى 90 يوم.");

        store.AddDocument("policy-security",
            "أمن المعلومات: لا يُسمح بمشاركة بيانات العملاء خارج الشبكة الداخلية. " +
            "كلمات المرور تُغيَّر كل 90 يوم. VPN إلزامي للعمل عن بُعد.");
    }
}
