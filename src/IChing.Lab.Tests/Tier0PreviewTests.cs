using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Tests;

public class Tier0PreviewTests
{
    [Fact]
    public void BaziTier0_ContainsDayMasterAndPattern()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        var preview = ReadingSummaries.BuildBaziTier0Preview(chart, "事业");

        Assert.Contains(chart.DayMaster, preview.OneLiner);
        Assert.Contains("事业", preview.OneLiner);
        Assert.Equal(ReadingSummaries.Tier0Disclaimer, preview.Disclaimer);
    }

    [Fact]
    public void LiuyaoTier0_ContainsHexagramName()
    {
        var chart = LiuyaoNajiaService.Coin(DateTimeOffset.Parse("2026-07-03T12:00:00+08:00"), 42);
        var preview = ReadingSummaries.BuildLiuyaoTier0Preview(chart, "工作", null);

        Assert.Contains(chart.OriginalHexagram, preview.OneLiner);
        Assert.Contains("世爻", preview.OneLiner);
    }

    [Fact]
    public void TarotTier0_ContainsPositionTitles()
    {
        var reading = TarotEngine.Draw("past-present-future", "career", 42);
        var preview = ReadingSummaries.BuildTarotTier0Preview(reading, "career");

        Assert.Contains("过去", preview.OneLiner);
        Assert.False(string.IsNullOrWhiteSpace(preview.Disclaimer));
    }
}

public class SpreadConfigTests
{
    [Fact]
    public void SpreadCatalog_LoadsFromEmbeddedJson()
    {
        var spreads = SpreadCatalog.List();
        Assert.True(spreads.Count >= 9);
        Assert.Contains(spreads, s => s.Id == "celtic-cross" && s.CardCount == 10);
    }
}
