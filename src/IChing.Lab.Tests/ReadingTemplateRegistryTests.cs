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
}
