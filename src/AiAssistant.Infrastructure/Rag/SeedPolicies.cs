namespace AiAssistant.Infrastructure.Rag;

/// <summary>The five HR policies the knowledge store is seeded with at startup.</summary>
/// <remarks>The ids appear in <c>AskResult.SourceChunks</c> and in the tests; keep them stable.</remarks>
internal static class SeedPolicies
{
    public static readonly IReadOnlyList<(string Id, string Text)> All =
    [
        ("policy-leave",
            "سياسة الإجازات: يحق للموظف 21 يوم إجازة سنوية بعد سنة خدمة. " +
            "تزداد إلى 28 يوم بعد 5 سنوات."),

        ("policy-remote",
            "العمل عن بُعد: مسموح يومين أسبوعياً بعد إتمام فترة الاختبار (3 أشهر). " +
            "يتطلب موافقة المدير المباشر."),

        ("policy-expenses",
            "المصروفات: تُقدَّم طلبات استرداد المصروفات بنهاية كل شهر. " +
            "الحد الأقصى للوجبات 150 ريال يومياً في السفر."),

        ("policy-sick-leave",
            "الإجازة المرضية: 30 يوم مدفوعة الأجر بشكل كامل. " +
            "ما فوق 30 يوم يُحتسب نصف الراتب حتى 90 يوم."),

        ("policy-security",
            "أمن المعلومات: لا يُسمح بمشاركة بيانات العملاء خارج الشبكة الداخلية. " +
            "كلمات المرور تُغيَّر كل 90 يوم. VPN إلزامي للعمل عن بُعد."),
    ];
}
