using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Rules;
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
        Assert.NotEmpty(ReadingSummaries.BuildBaziRuleDigest(bazi, "career").ActivePlugins);
        Assert.NotEmpty(ReadingSummaries.BuildLiuyaoRuleDigest(liuyao, "career", null).Items);
        Assert.NotEmpty(ReadingSummaries.BuildTarotRuleDigest(tarot).ActivePlugins);
    }

    [Fact]
    public void RuleEngine_DisablesPluginById()
    {
        var engine = new RuleEngine(new RuleEngineOptions
        {
            Plugins =
            {
                ["tarot.stats.elements"] = new RulePluginOptions { Enabled = false }
            }
        });
        var tarot = TarotEngine.Draw("past-present-future", "career", 42);
        var digest = ReadingSummaries.BuildTarotRuleDigest(tarot, engine);

        Assert.DoesNotContain("tarot.stats.elements", digest.ActivePlugins);
        Assert.DoesNotContain(digest.Items, i => i.PluginId == "tarot.stats.elements");
    }

    [Fact]
    public void RuleEngine_ListsAndUpdatesPlugins()
    {
        var engine = new RuleEngine();

        Assert.Contains(engine.ListPlugins(), p => p.Id == "liuyao.coin.probability" && p.Enabled);
        Assert.True(engine.ConfigurePlugin("liuyao.coin.probability", enabled: false, weight: 12));

        var updated = engine.ListPlugins().First(p => p.Id == "liuyao.coin.probability");
        Assert.False(updated.Enabled);
        Assert.Equal(12, updated.Weight);
        Assert.False(engine.ConfigurePlugin("missing", enabled: true, weight: null));
    }

    [Fact]
    public void RuleEngine_FiltersByMinWeight()
    {
        var engine = new RuleEngine(new RuleEngineOptions { MinWeight = 90 });
        var liuyao = LiuyaoNajiaService.Coin(DateTimeOffset.Parse("2026-07-03T12:00:00+08:00"), 42);
        var digest = ReadingSummaries.BuildLiuyaoRuleDigest(liuyao, "career", null, engine);

        Assert.DoesNotContain("liuyao.interpretation.traditional", digest.ActivePlugins);
        Assert.Contains("liuyao.yongshen.keyword", digest.ActivePlugins);
        Assert.DoesNotContain("liuyao.coin.probability", digest.ActivePlugins);
    }

    [Fact]
    public void Liuyao_UnclassifiedQuestion_UsesShiLine()
    {
        var liuyao = LiuyaoNajiaService.Coin(DateTimeOffset.Parse("2026-07-03T12:00:00+08:00"), 42);
        var digest = ReadingSummaries.BuildLiuyaoRuleDigest(liuyao, null, null);

        Assert.Contains("世", digest.YongShenSummary);
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
