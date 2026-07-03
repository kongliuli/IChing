using System.Text;
using System.Text.Json;

namespace IChing.Lab.Inference.Prompts;

[Obsolete("改用 IPromptBuilder（TemplatePromptBuilder）+ 外置 Scriban 模板 prompts/bazi-tier1-default.txt")]
public static class BaziPromptBuilder
{
    public static string BuildTier1(object chart, object ruleDigest, string? focus, int wordMax = 400)
    {
        const string system = """
            你是八字解读助手。四柱与大运由系统计算，请勿修改干支。
            不要编造未提供的流年、年份、应期或具体日期。
            """;

        var user = $"""
            关注：{focus ?? "综合"}
            规则摘要：
            {FormatRuleDigest(ruleDigest)}

            命盘：
            {JsonSerializer.Serialize(chart, new JsonSerializerOptions { WriteIndented = true })}

            请用简体中文写一段不超过 {wordMax} 字简析。若命盘未提供流年字段，不要提及任何具体年份。
            """;

        return QwenChatTemplate.Wrap(system, user);
    }

    private static string FormatRuleDigest(object ruleDigest)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleDigest));
        var sb = new StringBuilder();
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            sb.AppendLine($"- {prop.Name}: {prop.Value}");
        }
        return sb.ToString().TrimEnd();
    }
}
