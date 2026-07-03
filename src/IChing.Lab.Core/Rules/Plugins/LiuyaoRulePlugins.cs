using IChing.Lab.Core.Liuyao;

namespace IChing.Lab.Core.Rules.Plugins;

public static class LiuyaoRulePlugins
{
    public static readonly IReadOnlyList<RulePlugin> All =
    [
        new("liuyao.base.hexagram", "liuyao", "Hexagram basics", "Original, changed hexagram, moving lines, Shi/Ying basics.", 100, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var moving = chart.Lines.Where(l => l.IsChanging).Select(l => $"line {l.Index}").ToList();
            var changed = chart.ChangedHexagram is null ? "no changed hexagram" : $"changed to {chart.ChangedHexagram}";
            var text = $"{chart.OriginalHexagram}; {changed}; moving: {(moving.Count == 0 ? "none" : string.Join(", ", moving))}";
            return [new RuleDigestItem("liuyao.base.hexagram", "Hexagram", text, 100)];
        }),
        new("liuyao.base.najia", "liuyao", "Najia basics", "Six kin, six spirit, stem-branch, hidden deity summary.", 100, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var parts = chart.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.SixKin) || !string.IsNullOrWhiteSpace(l.SixSpirit))
                .Select(l => $"line {l.Index}: {l.SixKin} {l.StemBranch} {l.SixSpirit}".Trim())
                .Take(6);
            return [new RuleDigestItem("liuyao.base.najia", "Najia", string.Join("; ", parts), 100)];
        }),
        new("liuyao.yongshen.keyword", "liuyao", "Keyword yongshen", "Maps question keywords to six-kin; unclassified questions use Shi line.", 100, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var kind = ResolveQuestionKind(ctx.Focus, ctx.Question);
            var target = kind.SixKin is null
                ? chart.Lines.FirstOrDefault(IsShi)
                : chart.Lines.FirstOrDefault(l => SameSixKin(l.SixKin, kind.SixKin));
            var text = target is null
                ? "No matching line found; use Shi/世 line as subject."
                : $"{kind.Label}: use {kind.SixKin ?? "Shi/世"} at line {target.Index}, {target.SixKin} {target.StemBranch}";
            return [new RuleDigestItem("liuyao.yongshen.keyword", "Yongshen", text, 100)];
        }),
        new("liuyao.coin.probability", "liuyao", "Coin probability", "Documents the 6/7/8/9 distribution for three-coin casting.", 70, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            return chart.Method == "coin"
                ? [new RuleDigestItem("liuyao.coin.probability", "Coin method", "6/9 are moving lines at 1/8 each; 7/8 are static lines at 3/8 each.", 70)]
                : [];
        }),
        new("liuyao.interpretation.traditional", "liuyao", "Traditional reading hint", "Static charts focus Shi/Ying and yongshen; moving charts focus moving-to-changed relations.", 50, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var changing = chart.Lines.Count(l => l.IsChanging);
            var text = changing == 0
                ? "Static chart: read Shi/Ying, yongshen strength, body hexagram, and symbolic stars."
                : $"Moving chart: {changing} moving line(s); read movement, changed hexagram, and returning generation/restriction.";
            return [new RuleDigestItem("liuyao.interpretation.traditional", "Reading hint", text, 50)];
        })
    ];

    private static bool IsShi(LiuyaoLineDetail line) =>
        ContainsAny(line.Role ?? "", "世", "Worldly");

    private static bool SameSixKin(string? actual, string expected) =>
        !string.IsNullOrWhiteSpace(actual) && ContainsAny(actual, expected, SixKinFallback(expected));

    private static string SixKinFallback(string expected) => expected switch
    {
        "妻财" => "Wealth",
        "官鬼" => "Officer",
        "父母" => "Parent",
        "子孙" => "Offspring",
        "兄弟" => "Sibling",
        _ => expected
    };

    private static (string Label, string? SixKin) ResolveQuestionKind(string? focus, string? question)
    {
        var text = $"{focus} {question}";
        if (ContainsAny(text, "wealth", "money", "income", "business", "finance", "财", "钱", "投资", "生意")) return ("wealth", "妻财");
        if (ContainsAny(text, "career", "job", "exam", "lawsuit", "rule", "work", "事业", "工作", "考试", "官司")) return ("career", "官鬼");
        if (ContainsAny(text, "love", "marriage", "relationship", "感情", "婚", "恋爱")) return ("relationship", "妻财");
        if (ContainsAny(text, "health", "illness", "body", "病", "健康", "身体")) return ("health", "官鬼");
        if (ContainsAny(text, "document", "contract", "house", "parent", "message", "文书", "合同", "房", "父母", "消息")) return ("document", "父母");
        if (ContainsAny(text, "child", "result", "subordinate", "pet", "孩子", "成果", "下属", "宠物")) return ("result", "子孙");
        if (ContainsAny(text, "friend", "competition", "partner", "朋友", "竞争", "兄弟", "合伙")) return ("peer", "兄弟");
        return ("unclassified", null);
    }

    private static bool ContainsAny(string text, params string[] words) =>
        words.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
}
