using Lunar;

namespace IChing.Lab.Core.Bazi;

/// <summary>
/// ponytail: wraps lunar-csharp for lab spikes. True solar time correction is a follow-up.
/// </summary>
public static class BaziEngine
{
    public static BaziChart Calculate(BaziInput input)
    {
        var solar = Solar.FromYmdHms(input.Year, input.Month, input.Day, input.Hour, input.Minute, input.Second);
        var lunar = solar.Lunar;
        var eight = lunar.EightChar;

        return new BaziChart(
            Engine: "lunar-csharp-1.6.8",
            Solar: solar.ToString(),
            Lunar: lunar.ToString(),
            YearPillar: eight.Year,
            MonthPillar: eight.Month,
            DayPillar: eight.Day,
            HourPillar: eight.Time,
            YearNaYin: eight.YearNaYin,
            MonthNaYin: eight.MonthNaYin,
            DayNaYin: eight.DayNaYin,
            TimeNaYin: eight.TimeNaYin
        );
    }
}

public record BaziInput(int Year, int Month, int Day, int Hour, int Minute = 0, int Second = 0);

public record BaziChart(
    string Engine,
    string Solar,
    string Lunar,
    string YearPillar,
    string MonthPillar,
    string DayPillar,
    string HourPillar,
    string YearNaYin,
    string MonthNaYin,
    string DayNaYin,
    string TimeNaYin
);
