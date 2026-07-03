using Lunar;

namespace IChing.Lab.Core.Calendar;

public static class HuangLiService
{
    public static HuangLiDay GetDay(int year, int month, int day, int sect = 1)
    {
        var solar = Solar.FromYmdHms(year, month, day, 0, 0, 0);
        var lunar = solar.Lunar;

        return new HuangLiDay(
            Solar: solar.ToString(),
            Lunar: lunar.ToString(),
            WeekDay: solar.WeekInChinese,
            JieQi: lunar.JieQi,
            Yi: lunar.GetDayYi(sect).ToList(),
            Ji: lunar.GetDayJi(sect).ToList(),
            JiShen: lunar.DayJiShen.ToList(),
            XiongSha: lunar.DayXiongSha.ToList(),
            Chong: lunar.DayChongDesc,
            Sha: lunar.DaySha,
            TaiShen: lunar.DayPositionTai
        );
    }
}

public record HuangLiDay(
    string Solar,
    string Lunar,
    string WeekDay,
    string JieQi,
    IReadOnlyList<string> Yi,
    IReadOnlyList<string> Ji,
    IReadOnlyList<string> JiShen,
    IReadOnlyList<string> XiongSha,
    string Chong,
    string Sha,
    string TaiShen);
