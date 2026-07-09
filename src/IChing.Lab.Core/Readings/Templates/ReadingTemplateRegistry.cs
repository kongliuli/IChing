using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings.Templates;

/// <summary>
/// Core 模板注册表：统一 initial/followup 模板元数据与塔罗牌阵解析。
/// Scriban 文本仍由 Inference <see cref="IChing.Lab.Inference.Prompts.PromptTemplateRegistry"/> 加载。
/// </summary>
public static class ReadingTemplateRegistry
{
    private static readonly IReadOnlyDictionary<string, ReadingTemplateDescriptor> ById =
        new Dictionary<string, ReadingTemplateDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            ["bazi-tier1-default"] = new("bazi-tier1-default", "bazi", "initial", 1, ReadingSchemas.OutputV2, "core.bazi"),
            ["liuyao-tier1-default"] = new("liuyao-tier1-default", "liuyao", "initial", 1, ReadingSchemas.OutputV2, "core.liuyao"),
            ["tarot-tier1-en"] = new("tarot-tier1-en", "tarot", "initial", 1, ReadingSchemas.OutputV2, "core.tarot", NeedsTranslationPass: true),
            ["tarot-tier1-deckaura-default"] = new("tarot-tier1-deckaura-default", "tarot", "initial", 1, ReadingSchemas.OutputV2, "core.tarot"),
            ["tarot-tier2-celtic-cross"] = new("tarot-tier2-celtic-cross", "tarot", "initial", 2, ReadingSchemas.OutputV2, "core.tarot"),
            ["tarot-translate-to-zh"] = new("tarot-translate-to-zh", "tarot", "translate", 1, ReadingSchemas.OutputV2, "core.tarot"),
            ["core-followup-json"] = new("core-followup-json", "*", "followup", 1, ReadingSchemas.OutputV2, "core.followup"),
        };

    public static ReadingTemplateDescriptor ResolveInitial(string domain, int tier = 1) =>
        TryGet($"{domain}-tier{tier}-default", out var found)
            ? found
            : new($"{domain}-tier{tier}-default", domain, "initial", tier, ReadingSchemas.OutputV2, $"core.{domain}");

    public static TarotTemplateResolution ResolveTarot(string engineId, int tier, string spreadId)
    {
        if (tier == 2 && string.Equals(spreadId, "celtic-cross", StringComparison.OrdinalIgnoreCase))
        {
            return new(Template("tarot-tier2-celtic-cross"), 900, 1200);
        }

        if (engineId.StartsWith("tarot-deckaura", StringComparison.OrdinalIgnoreCase)
            || string.Equals(engineId, "iching-tarot-built-in", StringComparison.OrdinalIgnoreCase))
        {
            var wordLimit = tier == 2 ? 700 : 400;
            return new(Template("tarot-tier1-deckaura-default"), wordLimit, tier == 2 ? 900 : 512);
        }

        var limit = tier == 2 ? 800 : (spreadId == "celtic-cross" ? 500 : 280);
        return new(Template("tarot-tier1-en"), limit, tier == 2 ? 1024 : 512);
    }

    public static bool TryGet(string templateId, out ReadingTemplateDescriptor descriptor) =>
        ById.TryGetValue(templateId, out descriptor!);

    public static ReadingTemplateDescriptor Template(string templateId) =>
        TryGet(templateId, out var d) ? d : new(templateId, "unknown", "initial", 1, ReadingSchemas.OutputV2, "core.unknown");
}
