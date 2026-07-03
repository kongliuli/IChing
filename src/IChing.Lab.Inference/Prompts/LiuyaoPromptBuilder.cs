using System.Text;
using System.Text.Json;

namespace IChing.Lab.Inference.Prompts;

[Obsolete("改用 IPromptBuilder（TemplatePromptBuilder）+ 外置 Scriban 模板 prompts/liuyao-tier1-default.txt")]
public static class LiuyaoPromptBuilder
{
    public static string BuildTier1(
        string question,
        string? focus,
        object ruleDigest,
        object chart,
        int wordMin = 200,
        int wordMax = 400)
    {
        var rules = FormatRuleDigest(ruleDigest);
        var chartJson = JsonSerializer.Serialize(chart, new JsonSerializerOptions { WriteIndented = true });

        const string system = """
            你是六爻解读助手。卦象与规则结论由系统计算，请勿修改卦名、爻位、六亲、干支数据。
            不要编造未提供的神煞或具体应期日期。
            """;

        var user = $"""
            问事：{question}
            关注：{focus ?? "综合"}

            【规则摘要】
            {rules}

            【卦象数据】
            {chartJson}

            请用简体中文写一段 {wordMin}～{wordMax} 字简析，先点用神与世应，再论动变。
            """;

        return QwenChatTemplate.Wrap(system, user);
    }

    public static string BuildTier2Section(
        string sectionKey,
        string question,
        object ruleDigest,
        object chart)
    {
        var (title, hint) = sectionKey switch
        {
            "yongShen" => ("用神", "只讨论用神旺衰、空破、与世应关系"),
            "dongBian" => ("动变", "只讨论动爻、变卦、回头生克"),
            "yingQi" => ("应期", "仅基于已给月建日辰旬空，不编造具体日期"),
            "advice" => ("建议", "趋势判断与行动建议"),
            _ => ("解读", "补充分析")
        };

        var user = $"""
            问事：{question}
            【规则摘要】
            {FormatRuleDigest(ruleDigest)}

            【卦象数据】
            {JsonSerializer.Serialize(chart)}

            请用简体中文写「{title}」一节，约 150～200 字。{hint}
            """;

        const string system = "你是六爻解读助手。请勿修改系统给出的卦象与规则数据。";
        return QwenChatTemplate.Wrap(system, user);
    }

    private static string FormatRuleDigest(object ruleDigest)
    {
        if (ruleDigest is JsonElement el)
        {
            return FormatJsonElement(el);
        }

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleDigest));
        return FormatJsonElement(doc.RootElement);
    }

    private static string FormatJsonElement(JsonElement el)
    {
        var sb = new StringBuilder();
        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.Value.EnumerateArray())
                {
                    sb.AppendLine($"- {prop.Name}: {item}");
                }
            }
            else
            {
                sb.AppendLine($"- {prop.Name}: {prop.Value}");
            }
        }
        return sb.ToString().TrimEnd();
    }
}
