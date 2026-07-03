using Lunar;

namespace IChing.Lab.Core.Bazi;

public static class BaziEngine
{
    public static BaziChart Calculate(BaziInput input)
    {
        var wallClock = new DateTime(input.Year, input.Month, input.Day, input.Hour, input.Minute, input.Second);
        var corrected = input.Longitude is null
            ? wallClock
            : TrueSolarTime.Apply(wallClock, input.Longitude.Value);

        var solar = Solar.FromYmdHms(
            corrected.Year, corrected.Month, corrected.Day,
            corrected.Hour, corrected.Minute, corrected.Second);
        var lunar = solar.Lunar;
        var eight = lunar.EightChar;

        IReadOnlyList<DaYunPeriod>? daYun = null;
        YunInfo? yunInfo = null;
        if (input.Gender is not null)
        {
            var yun = eight.GetYun(input.Gender.Value, input.Sect);
            yunInfo = new YunInfo(
                yun.StartYear, yun.StartMonth, yun.StartDay, yun.StartHour,
                yun.Forward, yun.StartSolar.ToString());

            var periods = yun.GetDaYun(10);
            daYun = periods
                .Select(d => new DaYunPeriod(
                    d.Index, d.GanZhi, d.StartYear, d.EndYear, d.StartAge, d.EndAge))
                .ToList();
        }

        return new BaziChart(
            Engine: "lunar-csharp-1.6.8",
            WallClock: wallClock.ToString("yyyy-MM-dd HH:mm:ss"),
            TrueSolarTime: input.Longitude is null ? null : corrected.ToString("yyyy-MM-dd HH:mm:ss"),
            Longitude: input.Longitude,
            Solar: solar.ToString(),
            Lunar: lunar.ToString(),
            YearPillar: eight.Year,
            MonthPillar: eight.Month,
            DayPillar: eight.Day,
            HourPillar: eight.Time,
            YearNaYin: eight.YearNaYin,
            MonthNaYin: eight.MonthNaYin,
            DayNaYin: eight.DayNaYin,
            TimeNaYin: eight.TimeNaYin,
            Yun: yunInfo,
            DaYun: daYun
        );
    }
}

public record BaziInput(
    int Year, int Month, int Day, int Hour,
    int Minute = 0, int Second = 0,
    double? Longitude = null,
    int? Gender = null,
    int Sect = 1);

public record YunInfo(
    int StartYear, int StartMonth, int StartDay, int StartHour,
    bool Forward, string StartSolar);

public record DaYunPeriod(
    int Index, string GanZhi, int StartYear, int EndYear, int StartAge, int EndAge);

public record BaziChart(
    string Engine,
    string WallClock,
    string? TrueSolarTime,
    double? Longitude,
    string Solar,
    string Lunar,
    string YearPillar,
    string MonthPillar,
    string DayPillar,
    string HourPillar,
    string YearNaYin,
    string MonthNaYin,
    string DayNaYin,
    string TimeNaYin,
    YunInfo? Yun,
    IReadOnlyList<DaYunPeriod>? DaYun
);
