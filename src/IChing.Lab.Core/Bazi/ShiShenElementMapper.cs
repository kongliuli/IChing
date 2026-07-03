namespace IChing.Lab.Core.Bazi;

internal static class ShiShenElementMapper
{
    private static readonly Dictionary<string, string> GanWuXing = new()
    {
        ["甲"] = "木", ["乙"] = "木", ["丙"] = "火", ["丁"] = "火",
        ["戊"] = "土", ["己"] = "土", ["庚"] = "金", ["辛"] = "金",
        ["壬"] = "水", ["癸"] = "水"
    };

    private static readonly Dictionary<string, string> Generates = new()
    {
        ["木"] = "火", ["火"] = "土", ["土"] = "金", ["金"] = "水", ["水"] = "木"
    };

    private static readonly Dictionary<string, string> Overcomes = new()
    {
        ["木"] = "土", ["土"] = "水", ["水"] = "火", ["火"] = "金", ["金"] = "木"
    };

    public static IReadOnlyList<string> MapCategories(string dayMaster, IReadOnlyList<string> categories)
    {
        if (!GanWuXing.TryGetValue(dayMaster, out var dmWx))
        {
            return [];
        }

        var elements = new List<string>();
        foreach (var cat in categories)
        {
            elements.Add(cat switch
            {
                "比劫" => dmWx,
                "印星" => ElementThatGenerates(dmWx),
                "食伤" => Generates.GetValueOrDefault(dmWx, dmWx),
                "财星" => Overcomes.GetValueOrDefault(dmWx, dmWx),
                "官杀" => ElementThatOvercomes(dmWx),
                _ => dmWx
            });
        }

        return elements.Distinct().ToList();
    }

    private static string ElementThatGenerates(string target) =>
        Generates.FirstOrDefault(kv => kv.Value == target).Key ?? target;

    private static string ElementThatOvercomes(string target) =>
        Overcomes.FirstOrDefault(kv => kv.Value == target).Key ?? target;
}
