using IChing.Lab.Core.Bazi;
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
        Assert.NotEqual(without.HourPillar, withWest.HourPillar);
    }

    [Fact]
    public void Gender_ReturnsDaYun()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        Assert.NotNull(chart.DaYun);
        Assert.Equal(10, chart.DaYun!.Count);
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
}
