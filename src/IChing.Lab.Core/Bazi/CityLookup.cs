namespace IChing.Lab.Core.Bazi;

/// <summary>ponytail: 静态城市表，覆盖常见出生地；未命中时由调用方传 longitude。</summary>
public static class CityLookup
{
    private static readonly Dictionary<string, double> Cities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["beijing"] = 116.40,
        ["北京"] = 116.40,
        ["shanghai"] = 121.47,
        ["上海"] = 121.47,
        ["guangzhou"] = 113.26,
        ["广州"] = 113.26,
        ["shenzhen"] = 114.05,
        ["深圳"] = 114.05,
        ["chengdu"] = 104.06,
        ["成都"] = 104.06,
        ["chongqing"] = 106.55,
        ["重庆"] = 106.55,
        ["wuhan"] = 114.30,
        ["武汉"] = 114.30,
        ["xian"] = 108.93,
        ["西安"] = 108.93,
        ["urumqi"] = 87.62,
        ["乌鲁木齐"] = 87.62,
        ["lhasa"] = 91.11,
        ["拉萨"] = 91.11,
        ["harbin"] = 126.63,
        ["哈尔滨"] = 126.63,
        ["taipei"] = 121.56,
        ["台北"] = 121.56,
        ["hongkong"] = 114.17,
        ["香港"] = 114.17
    };

    public static bool TryGetLongitude(string city, out double longitude) =>
        Cities.TryGetValue(city.Trim(), out longitude);

    public static IReadOnlyList<string> List() => Cities.Keys.Distinct().OrderBy(k => k).ToList();
}
