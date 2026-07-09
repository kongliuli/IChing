using System.Text;
using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings;

/// <summary>
/// Lab Scriban 路径的 JSON 输出契约后缀；Remote/App 路径由 SystemPrompt + Packet.OutputSchema 覆盖。
/// </summary>
public static class ReadingJsonOutputContract
{
    private static readonly HashSet<string> SkipTemplateIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "tarot-translate-to-zh"
    };

    public static string Append(string domain, string prompt, string? templateId = null)
    {
        if (templateId is not null && SkipTemplateIds.Contains(templateId))
        {
            return prompt;
        }

        if (prompt.Contains(ReadingSchemas.OutputV2, StringComparison.Ordinal))
        {
            return prompt;
        }

        var template = ReadingPromptTemplateManager.Get(domain, "initial");
        var sections = template.OutputSections
            .Select(s => $"    {{ \"key\": \"{s.Key}\", \"title\": \"{s.Title}\", \"body\": \"...\" }}")
            .ToList();

        var sb = new StringBuilder(prompt.TrimEnd());
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("请仅返回一个合法 JSON 对象，不要 markdown、不要代码围栏、不要额外说明。");
        sb.AppendLine($"输出 schema {ReadingSchemas.OutputV2}:");
        sb.AppendLine("{");
        sb.AppendLine($"  \"schema\": \"{ReadingSchemas.OutputV2}\",");
        sb.AppendLine("  \"summary\": \"一句总论\",");
        sb.AppendLine("  \"sections\": [");
        sb.AppendLine(string.Join(",\n", sections));
        sb.AppendLine("  ],");
        sb.AppendLine("  \"warnings\": []");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
