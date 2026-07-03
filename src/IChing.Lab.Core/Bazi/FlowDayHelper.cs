using Lunar;

namespace IChing.Lab.Core.Bazi;

public static class FlowDayHelper
{
    public static IReadOnlyList<FlowDayInfo> ListDaysInMonth(int year, int month)
    {
        var days = new List<FlowDayInfo>();
        for (var day = 1; day <= 31; day++)
        {
            if (!TryGetDay(year, month, day, out var info))
            {
                break;
            }

            days.Add(info);
        }

        return days;
    }

    public static FlowDayInfo? GetDay(int year, int month, int day) =>
        TryGetDay(year, month, day, out var info) ? info : null;

    private static bool TryGetDay(int year, int month, int day, out FlowDayInfo info)
    {
        info = null!;
        try
        {
            var solar = Solar.FromYmdHms(year, month, day, 0, 0, 0);
            var lunar = solar.Lunar;
            info = new FlowDayInfo(
                day,
                solar.ToString(),
                lunar.DayInGanZhi,
                lunar.DayXun,
                lunar.DayXunKong);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public record FlowDayInfo(
    int Day,
    string Solar,
    string GanZhi,
    string Xun,
    string XunKong);
