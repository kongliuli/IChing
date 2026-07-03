using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Calendar;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Tests;

public class BaziTrueSolarTimeTests
{
    [Fact]
    public void Longitude_ShiftsHourPillar()
    {
        var without = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 12, Gender: 1));
        var withWest = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 12, Longitude: 87.6, Gender: 1));
        Assert.NotNull(withWest.TrueSolarTime);
        Assert.NotEqual(without.HourPillar.GanZhi, withWest.HourPillar.GanZhi);
    }

    [Fact]
    public void Gender_ReturnsDaYun()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        Assert.NotNull(chart.DaYun);
        Assert.Equal(10, chart.DaYun!.Count);
    }

    [Fact]
    public void City_ResolvesLongitude()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 12, City: "上海", Gender: 1));
        Assert.Equal(121.47, chart.Longitude);
        Assert.NotNull(chart.TrueSolarTime);
    }

    [Fact]
    public void HideGan_And_ShiShen_Present()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        Assert.NotEmpty(chart.YearPillar.HideGan);
        Assert.NotEmpty(chart.YearPillar.ShiShenGan);
        Assert.Equal(chart.DayPillar.Gan, chart.DayMaster);
    }

    [Fact]
    public void FlowYear_ReturnsLiuNian()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1, FlowYear: 2026));
        Assert.NotNull(chart.FlowYear);
        Assert.Equal(2026, chart.FlowYear!.Year);
        Assert.NotEmpty(chart.FlowYear.GanZhi);
    }
}

public class HePanTests
{
    [Fact]
    public void Compare_ReturnsScore()
    {
        var a = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        var b = BaziEngine.Calculate(new BaziInput(1992, 8, 15, 14, Gender: 0));
        var result = HePanService.Compare(a, b);
        Assert.InRange(result.Score, 0, 100);
        Assert.NotEmpty(result.Summary);
    }
}

public class LiuyaoNajiaTests
{
    [Fact]
    public void Coin_IncludesNajiaFields()
    {
        var result = LiuyaoNajiaService.Coin(DateTimeOffset.Now, 42);
        Assert.Equal("IChingLibrary.SixLines", result.Engine);
        Assert.Equal(6, result.Lines.Count);
        Assert.Contains(result.Lines, l => l.StemBranch is not null);
        Assert.Contains(result.Lines, l => l.SixKin is not null);
    }

    [Fact]
    public void Coin_IncludesSymbolicStars()
    {
        var result = LiuyaoNajiaService.Coin(DateTimeOffset.Now, 99);
        Assert.NotEmpty(result.SymbolicStars);
    }
}

public class TarotDeckTests
{
    [Fact]
    public void Deck_Has78Cards()
    {
        Assert.Equal(78, TarotDeck.All.Count);
    }

    [Fact]
    public void CelticCross_Draws10Cards()
    {
        var reading = TarotEngine.Draw("celtic-cross", "career", 7);
        Assert.Equal(10, reading.Positions.Count);
    }

    [Fact]
    public void Horseshoe_Draws7Cards()
    {
        var reading = TarotEngine.Draw("horseshoe", "love", 3);
        Assert.Equal(7, reading.Positions.Count);
    }

    [Fact]
    public void Narrative_BuildsSummary()
    {
        var reading = TarotEngine.Draw("past-present-future", "test", 1);
        var narrative = TarotNarrative.Build(reading);
        Assert.NotEmpty(narrative.Summary);
        Assert.Equal(3, narrative.Sections.Count);
    }
}

public class HuangLiTests
{
    [Fact]
    public void Day_ReturnsYiJi()
    {
        var day = HuangLiService.GetDay(2026, 7, 3);
        Assert.NotEmpty(day.Yi);
        Assert.NotEmpty(day.Ji);
        Assert.NotEmpty(day.Solar);
    }
}
