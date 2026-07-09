namespace IChing.Lab.Core.Rules;

using IChing.Lab.Core.Readings;

public sealed record PromptExtension(
    IReadOnlyList<string> SystemDirectives,
    IReadOnlyList<OutputSectionSpec> OutputSections,
    IReadOnlyList<string> Constraints);

/// <summary>
/// RuleEngine 插件启用时追加的 AI Prompt 扩展；首期由静态表驱动，开关仍走 RuleEngineOptions。
/// </summary>
public static class RulePromptExtensionRegistry
{
    private static readonly IReadOnlyDictionary<string, PromptExtension> ByPluginId =
        new Dictionary<string, PromptExtension>(StringComparer.OrdinalIgnoreCase)
        {
            ["bazi.yongshen.current"] = new(
                ["Interpret yongshen only from plugin facts; do not recompute stems or branches."],
                [new("yongshen", "用神分析")],
                ["Do not contradict serialized yongshen facts."]),
            ["bazi.wuxing.balance"] = new(
                ["Treat element counts as hints; combine with month command and yongshen."],
                [new("flow", "五行平衡")],
                []),
            ["bazi.flow.current"] = new(
                ["Mention flow year or da-yun only when plugin facts include them."],
                [new("flow", "运势流转")],
                ["Do not invent dates or years not present in facts."]),
            ["bazi.school.ziping.geju"] = new(
                ["Use ziping geju framing when this plugin is active."],
                [new("geju", "格局分析")],
                []),
            ["liuyao.interpretation.traditional"] = new(
                ["Follow traditional liuyao line and six-kin interpretation."],
                [new("changing", "动变分析"), new("shi_ying", "世应")],
                []),
            ["liuyao.shensha.markers"] = new(
                ["Reference shensha markers only when present in plugin facts."],
                [new("overview", "神煞提示")],
                [])
        };

    public static PromptExtension? Get(string pluginId) =>
        ByPluginId.TryGetValue(pluginId, out var ext) ? ext : null;

    public static PromptExtension Merge(IEnumerable<string> activePluginIds)
    {
        var directives = new List<string>();
        var sections = new List<OutputSectionSpec>();
        var constraints = new List<string>();
        foreach (var id in activePluginIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Get(id) is not { } ext)
            {
                continue;
            }

            directives.AddRange(ext.SystemDirectives);
            sections.AddRange(ext.OutputSections);
            constraints.AddRange(ext.Constraints);
        }

        return new PromptExtension(directives, sections, constraints);
    }
}
