using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Tests;

public class ReadingSummaryTests
{
    [Fact]
    public void ThreeDomains_BuildNonEmptyRuleSummaries()
    {
        var bazi = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        var liuyao = LiuyaoNajiaService.Coin(DateTimeOffset.Parse("2026-07-03T12:00:00+08:00"), 42);
        var tarot = TarotEngine.Draw("past-present-future", "career", 42);

        Assert.False(string.IsNullOrWhiteSpace(ReadingSummaries.BuildBaziPreview(bazi, "career").OneLiner));
        Assert.False(string.IsNullOrWhiteSpace(ReadingSummaries.BuildLiuyaoRuleDigest(liuyao, "career", null).YongShenSummary));
        Assert.True(ReadingSummaries.BuildTarotRuleDigest(tarot).Total > 0);
    }

    [Fact]
    public void Prompt_KeepsComputedTarotFacts()
    {
        var tarot = TarotEngine.Draw("past-present-future", "career", 42);
        var digest = ReadingSummaries.BuildTarotRuleDigest(tarot);
        var prompt = ReadingSummaries.BuildChatPrompt("tarot", tarot.Question, null, tarot, digest);
        var first = tarot.Positions[0];

        Assert.Contains(first.CardName, prompt);
        Assert.Contains(first.PositionTitle, prompt);
        Assert.Contains(first.Reversed.ToString().ToLowerInvariant(), prompt.ToLowerInvariant());
    }

    [Fact]
    public void SettingsLikeJson_DoesNotNeedPlainApiKey()
    {
        var json = JsonSerializer.Serialize(new { baseUrl = "https://api.openai.com/v1", model = "test", apiKeyProtected = "cipher" });

        Assert.DoesNotContain("sk-test-secret", json);
    }
}
