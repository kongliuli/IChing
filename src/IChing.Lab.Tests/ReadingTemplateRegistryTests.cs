using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Readings.Templates;

namespace IChing.Lab.Tests;

public class ReadingTemplateRegistryTests
{
    [Fact]
    public void ResolveInitial_Bazi_ReturnsDefaultTemplate()
    {
        var d = ReadingTemplateRegistry.ResolveInitial("bazi", 1);
        Assert.Equal("bazi-tier1-default", d.TemplateId);
        Assert.Equal(ReadingSchemas.OutputV2, d.OutputSchema);
    }

    [Fact]
    public void ResolveTarot_CelticCrossTier2_UsesDedicatedTemplate()
    {
        var r = ReadingTemplateRegistry.ResolveTarot("tarot-deckaura", 2, "celtic-cross");
        Assert.Equal("tarot-tier2-celtic-cross", r.Descriptor.TemplateId);
        Assert.False(r.Descriptor.NeedsTranslationPass);
    }

    [Fact]
    public void ResolveTarot_DeckAura_SkipsTranslatePass()
    {
        var r = ReadingTemplateRegistry.ResolveTarot("iching-tarot-built-in", 1, "past-present-future");
        Assert.Equal("tarot-tier1-deckaura-default", r.Descriptor.TemplateId);
        Assert.False(r.Descriptor.NeedsTranslationPass);
    }

    [Fact]
    public void ResolveTarot_Default_UsesTier1Default_NoTranslatePass()
    {
        var r = ReadingTemplateRegistry.ResolveTarot("some-engine", 1, "past-present-future");
        Assert.Equal("tarot-tier1-default", r.Descriptor.TemplateId);
        Assert.False(r.Descriptor.NeedsTranslationPass);
        Assert.Equal(280, r.WordLimit);
        Assert.Equal(512, r.MaxTokens);
        Assert.NotEqual("tarot-tier1-en", r.Descriptor.TemplateId);
    }

    [Fact]
    public void ResolveTarot_Default_WordLimits_ByTierAndSpread()
    {
        var tier1Celtic = ReadingTemplateRegistry.ResolveTarot("x", 1, "celtic-cross");
        Assert.Equal("tarot-tier1-default", tier1Celtic.Descriptor.TemplateId);
        Assert.Equal(500, tier1Celtic.WordLimit);

        var tier2 = ReadingTemplateRegistry.ResolveTarot("x", 2, "past-present-future");
        Assert.Equal("tarot-tier1-default", tier2.Descriptor.TemplateId);
        Assert.Equal(800, tier2.WordLimit);
        Assert.Equal(1024, tier2.MaxTokens);
    }

    [Fact]
    public void TryGet_TarotTier1Default_IsRegistered()
    {
        Assert.True(ReadingTemplateRegistry.TryGet("tarot-tier1-default", out var d));
        Assert.Equal("tarot", d.Domain);
        Assert.False(d.NeedsTranslationPass);
        // Compatibility path retained
        Assert.True(ReadingTemplateRegistry.TryGet("tarot-tier1-en", out _));
    }
}

public class ReadingJsonOutputContractTests
{
    [Fact]
    public void Append_AddsV2SchemaBlock()
    {
        var prompt = "base prompt";
        var result = ReadingJsonOutputContract.Append("bazi", prompt, "bazi-tier1-default");
        Assert.Contains(ReadingSchemas.OutputV2, result);
        Assert.Contains("\"sections\"", result);
    }

    [Fact]
    public void Append_SkipsTranslateTemplate()
    {
        const string prompt = "translate only";
        var result = ReadingJsonOutputContract.Append("tarot", prompt, "tarot-translate-to-zh");
        Assert.Equal(prompt, result);
    }

    [Fact]
    public void Append_TarotTier1Default_IsNotSkipped()
    {
        var result = ReadingJsonOutputContract.Append("tarot", "test", "tarot-tier1-default");
        Assert.Contains("请仅返回一个合法 JSON 对象", result);
        Assert.Contains(ReadingSchemas.OutputV2, result);
        Assert.Contains("\"key\": \"overview\"", result);
        Assert.Contains("\"key\": \"advice\"", result);
    }
}
