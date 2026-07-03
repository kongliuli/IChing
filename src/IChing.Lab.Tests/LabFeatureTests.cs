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
    public void FlowYear_ReturnsLiuNianAndMonths()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1, FlowYear: 2026));
        Assert.NotNull(chart.FlowYear);
        Assert.Equal(2026, chart.FlowYear!.Year);
        Assert.Equal(12, chart.FlowYear.Months.Count);
    }

    [Fact]
    public void FlowMonth_SelectsMonth()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1, FlowYear: 2026, FlowMonth: 7));
        Assert.NotNull(chart.FlowYear?.SelectedMonth);
        Assert.NotEmpty(chart.FlowYear!.SelectedMonth!.GanZhi);
    }

    [Fact]
    public void FlowMonth_HasJieQiBoundaries()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1, FlowYear: 2026));
        var month = chart.FlowYear!.Months[0];
        Assert.Equal("立春", month.JieQiStart);
        Assert.NotNull(month.StartSolar);
        Assert.NotNull(month.EndSolar);
    }

    [Fact]
    public void FlowMonth_WithIndex_IncludesTermDays()
    {
        var chart = BaziEngine.Calculate(new BaziInput(
            1990, 5, 20, 10, Gender: 1, FlowYear: 2026, FlowMonth: 1));
        var selected = chart.FlowYear!.SelectedMonth!;
        Assert.NotNull(selected.FlowDays);
        Assert.True(selected.FlowDays!.Count >= 28);
        Assert.Equal(selected.StartSolar, selected.FlowDays[0].Solar[..10]);
    }

    [Fact]
    public void YongShen_HasGeJuBreak()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        Assert.NotNull(chart.YongShen.GeJu.Break);
        Assert.NotEmpty(chart.YongShen.GeJu.Break!.Summary);
        Assert.NotEmpty(chart.YongShen.GeJu.Pattern);
    }

    [Fact]
    public void FlowYear_IncludesXiaoYun()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1, FlowYear: 2026));
        Assert.NotNull(chart.FlowYear?.XiaoYun);
        Assert.Equal(2026, chart.FlowYear!.XiaoYun!.Year);
    }

    [Fact]
    public void FlowCalendarMonth_ListsFlowDays()
    {
        var chart = BaziEngine.Calculate(new BaziInput(
            1990, 5, 20, 10, Gender: 1,
            FlowYear: 2026, FlowCalendarMonth: 7, FlowDay: 3));
        Assert.NotNull(chart.FlowYear?.FlowDays);
        Assert.True(chart.FlowYear!.FlowDays!.Count >= 28);
        Assert.NotNull(chart.FlowYear.SelectedDay);
        Assert.Equal(3, chart.FlowYear.SelectedDay!.Day);
    }
}

public class HePanTests
{
    [Fact]
    public void Compare_ReturnsScoreWithNaYinAndYongShen()
    {
        var a = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        var b = BaziEngine.Calculate(new BaziInput(1992, 8, 15, 14, Gender: 0));
        var result = HePanService.Compare(a, b);
        Assert.InRange(result.Score, 0, 100);
        Assert.NotEmpty(result.DayNaYinRelation);
        Assert.NotEmpty(result.YongShenComplement);
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
    public void Coin_WithChangingLines_HasFullChangedDetail()
    {
        LiuyaoNajiaResult? found = null;
        for (var seed = 0; seed < 200 && found?.Changed is null; seed++)
        {
            var result = LiuyaoNajiaService.Coin(DateTimeOffset.Now, seed);
            if (result.Changed is not null)
            {
                found = result;
            }
        }

        Assert.NotNull(found?.Changed);
        Assert.NotEmpty(found.Changed.SymbolicStars);
        Assert.Equal(6, found.Changed.Lines.Count);
        Assert.NotNull(found.Comparison);
        Assert.Equal(6, found.Comparison!.Lines.Count);
        Assert.NotEmpty(found.Comparison.OriginalShiYing);
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
