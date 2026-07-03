namespace IChing.Lab.Core.Liuyao;

public static class LiuyaoComparisonBuilder
{
    public static ChangedComparison Build(
        IReadOnlyList<LiuyaoLineDetail> original,
        IReadOnlyList<LiuyaoLineDetail> changed)
    {
        var lines = new List<LineComparison>();
        for (var i = 0; i < 6; i++)
        {
            var o = original[i];
            var c = changed[i];
            lines.Add(new LineComparison(
                o.Index,
                o.Position,
                o.IsChanging,
                o.SixKin,
                c.SixKin,
                o.Role,
                c.Role,
                o.StemBranch,
                c.StemBranch,
                o.YinYang != c.YinYang,
                o.SixSpirit,
                c.SixSpirit));
        }

        return new ChangedComparison(
            DescribeShiYing(original),
            DescribeShiYing(changed),
            lines);
    }

    private static string DescribeShiYing(IReadOnlyList<LiuyaoLineDetail> lines)
    {
        var shi = lines.FirstOrDefault(l => l.Role is "世");
        var ying = lines.FirstOrDefault(l => l.Role is "应");
        if (shi is null && ying is null)
        {
            return "世应未定位";
        }

        return $"世爻第{shi?.Index ?? 0}爻，应爻第{ying?.Index ?? 0}爻";
    }
}

public record LineComparison(
    int Index,
    string Position,
    bool IsChanging,
    string? OriginalSixKin,
    string? ChangedSixKin,
    string? OriginalRole,
    string? ChangedRole,
    string? OriginalStemBranch,
    string? ChangedStemBranch,
    bool YinYangChanged,
    string? OriginalSixSpirit,
    string? ChangedSixSpirit);

public record ChangedComparison(
    string OriginalShiYing,
    string ChangedShiYing,
    IReadOnlyList<LineComparison> Lines);
