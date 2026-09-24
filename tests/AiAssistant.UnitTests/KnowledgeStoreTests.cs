using AiAssistant.Infrastructure.Rag;
using Xunit;

namespace AiAssistant.UnitTests;

public sealed class KnowledgeStoreTests
{
    private static InMemoryKnowledgeStore Seeded()
    {
        var store = new InMemoryKnowledgeStore();
        store.AddDocument("leave", "سياسة الإجازات: يحق للموظف 21 يوم إجازة سنوية بعد سنة خدمة.");
        store.AddDocument("remote", "العمل عن بعد: مسموح يومين أسبوعياً بعد إتمام فترة الاختبار.");
        store.AddDocument("expenses", "المصروفات: تقدم طلبات استرداد المصروفات بنهاية كل شهر.");
        return store;
    }

    [Fact]
    public async Task Search_RanksTheDocumentSharingTheQueryTermsFirst()
    {
        var hits = await Seeded().SearchAsync("كم يوم إجازة سنوية؟", topK: 3);

        Assert.Equal("leave", hits[0].Id);
        Assert.True(hits[0].Score > hits[1].Score);
    }

    [Fact]
    public async Task Search_ReturnsAtMostTopK()
    {
        var hits = await Seeded().SearchAsync("المصروفات", topK: 2);

        Assert.Equal(2, hits.Count);
        Assert.Equal("expenses", hits[0].Id);
    }

    [Fact]
    public async Task Search_ScoresAreOrderedDescending()
    {
        var hits = await Seeded().SearchAsync("العمل عن بعد يومين", topK: 3);

        Assert.Equal(hits.OrderByDescending(h => h.Score).Select(h => h.Id), hits.Select(h => h.Id));
    }

    [Fact]
    public async Task Search_WithNoSharedTermsScoresZero()
    {
        var hits = await Seeded().SearchAsync("kubernetes", topK: 3);

        Assert.All(hits, h => Assert.Equal(0f, h.Score));
    }

    [Fact]
    public async Task Search_OfAnEmptyQueryScoresZeroInsteadOfDividingByZero()
    {
        var hits = await Seeded().SearchAsync("   ", topK: 3);

        Assert.All(hits, h => Assert.Equal(0f, h.Score));
    }

    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        var store = new InMemoryKnowledgeStore();
        store.AddDocument("vpn", "VPN is mandatory for remote work");
        store.AddDocument("other", "meals are capped per day");

        var hits = await store.SearchAsync("vpn", topK: 1);

        Assert.Equal("vpn", hits[0].Id);
        Assert.True(hits[0].Score > 0f);
    }

    [Fact]
    public async Task Search_OnAnEmptyStoreReturnsNothing()
    {
        var hits = await new InMemoryKnowledgeStore().SearchAsync("anything");

        Assert.Empty(hits);
    }
}
