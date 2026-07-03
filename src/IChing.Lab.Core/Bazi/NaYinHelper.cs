namespace IChing.Lab.Core.Bazi;

public static class NaYinHelper
{
    private static readonly Dictionary<string, string> Generates = new()
    {
        ["木"] = "火", ["火"] = "土", ["土"] = "金", ["金"] = "水", ["水"] = "木"
    };

    private static readonly Dictionary<string, string> Overcomes = new()
    {
        ["木"] = "土", ["土"] = "水", ["水"] = "火", ["火"] = "金", ["金"] = "木"
    };

    public static string ElementOf(string naYin) =>
        naYin.Length > 0 ? naYin[^1].ToString() : "";

    public static string DescribeRelation(string naYinA, string naYinB)
    {
        var wxA = ElementOf(naYinA);
        var wxB = ElementOf(naYinB);
        if (wxA.Length == 0 || wxB.Length == 0)
        {
            return "未知";
        }

        if (wxA == wxB)
        {
            return $"纳音同属{wxA}";
        }

        if (Generates.TryGetValue(wxA, out var gen) && gen == wxB)
        {
            return $"纳音相生（{wxA}生{wxB}）";
        }

        if (Generates.TryGetValue(wxB, out var genB) && genB == wxA)
        {
            return $"纳音相生（{wxB}生{wxA}）";
        }

        if (Overcomes.TryGetValue(wxA, out var ov) && ov == wxB)
        {
            return $"纳音相克（{wxA}克{wxB}）";
        }

        if (Overcomes.TryGetValue(wxB, out var ovB) && ovB == wxA)
        {
            return $"纳音相克（{wxB}克{wxA}）";
        }

        return "纳音中性";
    }
}
