using System.Globalization;

namespace IChing.Lab.Core.Tarot;

public static class ZodiacCalculator
{
    private static readonly (int Month, int Day, string Name)[] Boundaries =
    [
        (1, 20, "水瓶座"),
        (2, 19, "双鱼座"),
        (3, 21, "白羊座"),
        (4, 20, "金牛座"),
        (5, 21, "双子座"),
        (6, 22, "巨蟹座"),
        (7, 23, "狮子座"),
        (8, 23, "处女座"),
        (9, 23, "天秤座"),
        (10, 24, "天蝎座"),
        (11, 23, "射手座"),
        (12, 22, "摩羯座"),
    ];

    public static string? FromBirthday(string? birthday)
    {
        if (birthday is null)
            return null;

        if (!DateTime.TryParseExact(birthday, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            return null;

        var key = date.Month * 100 + date.Day;
        for (var i = 0; i < Boundaries.Length; i++)
        {
            var b = Boundaries[i];
            if (key < b.Month * 100 + b.Day)
                return i == 0 ? Boundaries[^1].Name : Boundaries[i - 1].Name;
        }
        return Boundaries[^1].Name;
    }
}
