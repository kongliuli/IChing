using System.Text;
using System.Text.Json;

namespace IChing.Lab.Inference.Prompts;

public static class BaziPromptBuilder
{
    public static string BuildTier1(object chart, object ruleDigest, string? focus, int wordMax = 400)
    {
        const string system = """
            你是八字解读助手。四柱与大运由系统计算，请勿修改干支。
            不要编造未提供的流年细节。
            """;

        var user = $"""
            关注：{focus ?? "综合"}
            规则摘要：
            {FormatRuleDigest(ruleDigest)}

            命盘：
            {JsonSerializer.Serialize(chart, new JsonSerializerOptions { WriteIndented = true })}

            请用简体中文写一段不超过 {wordMax} 字简析。
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
