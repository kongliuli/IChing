using IChing.Lab.Core.Liuyao;

namespace IChing.Lab.Core.Rules.Plugins;

public static class LiuyaoRulePlugins
{
    public static readonly IReadOnlyList<RulePlugin> All =
    [
        new("liuyao.base.hexagram", "liuyao", "卦象基础", "本卦、变卦、动爻、世应基础。", 100, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var moving = chart.Lines.Where(l => l.IsChanging).Select(l => $"第{l.Index}爻").ToList();
            var changed = chart.ChangedHexagram is null ? "无变卦" : $"变卦：{chart.ChangedHexagram}";
            var text = $"本卦：{chart.OriginalHexagram}；{changed}；动爻：{(moving.Count == 0 ? "无" : string.Join("、", moving))}";
            return [new RuleDigestItem("liuyao.base.hexagram", "卦象", text, 100)];
        }),
        new("liuyao.base.najia", "liuyao", "纳甲基础", "六亲、六神、干支、伏神摘要。", 100, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var parts = chart.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.SixKin) || !string.IsNullOrWhiteSpace(l.SixSpirit))
                .Select(l => $"第{l.Index}爻：{l.SixKin} {l.StemBranch} {l.SixSpirit}".Trim())
                .Take(6);
            return [new RuleDigestItem("liuyao.base.najia", "纳甲", string.Join("；", parts), 100)];
        }),
        new("liuyao.yongshen.keyword", "liuyao", "关键词取用神", "按问题关键词匹配六亲；未分类默认世爻。", 100, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var kind = ResolveQuestionKind(ctx.Focus, ctx.Question);
            var target = kind.SixKin is null
                ? chart.Lines.FirstOrDefault(IsShi)
                : chart.Lines.FirstOrDefault(l => SameSixKin(l.SixKin, kind.SixKin));
            var text = target is null
                ? $"未找到{kind.Label}对应爻，默认以世爻为用神。"
                : $"{kind.Label}：取第{target.Index}爻为用神，{target.SixKin} {target.StemBranch}";
            return [new RuleDigestItem("liuyao.yongshen.keyword", "用神", text, 100)];
        }),
        new("liuyao.coin.probability", "liuyao", "铜钱概率", "三枚铜钱起卦的 6/7/8/9 分布。", 70, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            return chart.Method == "coin"
                ? [new RuleDigestItem("liuyao.coin.probability", "铜钱法", "老阴 6、老阳 9 为动爻，各占 1/8；少阳 7、少阴 8 为静爻，各占 3/8。", 70)]
                : [];
        }),
        new("liuyao.interpretation.traditional", "liuyao", "传统提示", "静卦重世应用神，动卦重动变关系。", 50, true, ctx =>
        {
            var chart = (LiuyaoNajiaResult)ctx.Chart;
            var changing = chart.Lines.Count(l => l.IsChanging);
            var text = changing == 0
                ? "静卦：重点看世应、用神旺衰、卦身与神煞。"
                : $"动卦：共有 {changing} 个动爻，重点看动爻、变卦及生克回头关系。";
            return [new RuleDigestItem("liuyao.interpretation.traditional", "解读提示", text, 50)];
        })
    ];

    private static bool IsShi(LiuyaoLineDetail line) =>
        ContainsAny(line.Role ?? "", "Shi", "Worldly", "世");

    private static bool SameSixKin(string? actual, string expected) =>
        !string.IsNullOrWhiteSpace(actual) && ContainsAny(actual, expected, SixKinFallback(expected));

    private static string SixKinFallback(string expected) => expected switch
    {
        "妻财" => "WifeWealth",
        "官鬼" => "OfficerGhost",
        "父母" => "Parent",
        "子孙" => "ChildGrandchild",
        "兄弟" => "Brother",
        _ => expected
    };

    private static (string Label, string? SixKin) ResolveQuestionKind(string? focus, string? question)
    {
        var text = $"{focus} {question}";
        if (ContainsAny(text, "财", "钱", "收入", "生意", "投资", "wealth", "money", "income", "business", "finance", "invest")) return ("财运", "妻财");
        if (ContainsAny(text, "事业", "工作", "考试", "诉讼", "官", "career", "job", "exam", "lawsuit", "work")) return ("事业", "官鬼");
        if (ContainsAny(text, "感情", "婚姻", "恋爱", "关系", "love", "marriage", "relationship")) return ("感情", "妻财");
        if (ContainsAny(text, "健康", "疾病", "身体", "health", "illness", "body")) return ("健康", "官鬼");
        if (ContainsAny(text, "文书", "合同", "房", "父母", "消息", "document", "contract", "house", "message")) return ("文书", "父母");
        if (ContainsAny(text, "子女", "结果", "下属", "宠物", "child", "result", "subordinate", "pet")) return ("结果", "子孙");
        if (ContainsAny(text, "朋友", "竞争", "伙伴", "friend", "competition", "partner")) return ("同辈", "兄弟");
        return ("未分类问题", null);
    }

    private static bool ContainsAny(string text, params string[] words) =>
        words.Any(w => text.Contains(w, StringComparison.OrdinalIgnoreCase));
}
