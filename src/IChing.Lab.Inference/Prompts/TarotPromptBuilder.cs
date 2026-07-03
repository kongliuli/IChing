using System.Text;
using System.Text.Json;

namespace IChing.Lab.Inference.Prompts;

[Obsolete("改用 IPromptBuilder（TemplatePromptBuilder）+ 外置 Scriban 模板 prompts/tarot-tier1-en.txt 与 prompts/tarot-translate-to-zh.txt")]
public static class TarotPromptBuilder
{
    public static string BuildEnglishTier1(
        string question,
        string spreadTitle,
        object ruleDigest,
        IReadOnlyList<TarotPositionPrompt> positions,
        int wordLimit = 280)
    {
        const string system = """
            You are a tarot reading assistant. The spread below was drawn by the system.
            Do NOT change card names, positions, or upright/reversed states.
            Do NOT invent cards that are not listed.
            Mention each card name only in its position heading; in prose refer to "this card" or "this position".
            Write in clear English only.
            """;

        var rules = FormatRuleDigest(ruleDigest);
        var posBlock = new StringBuilder();
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            var orient = p.Reversed ? "reversed" : "upright";
            posBlock.AppendLine(
                $"{i + 1}. [{p.PositionTitle} / {p.PositionContext}] {p.CardName} ({orient}) — {p.MeaningEn}");
        }

        var user = $"""
            Question: {question}
            Spread: {spreadTitle}
            Rule summary:
            {rules}

            Positions:
            {posBlock}

            Write a {wordLimit}-word reading that follows the spread's narrative arc.
            Use one section per listed position and keep card names only in section headings.
            """;

        return QwenChatTemplate.Wrap(system, user);
    }

    public static string BuildTranslateToChinese(string englishText, IEnumerable<string>? cardNames = null)
    {
        const string system = """
            Translate the following English divination reading into natural Simplified Chinese.
            Keep every tarot card name exactly in English, including capitalization.
            Do NOT translate, replace, or substitute card names.
            Do NOT add, remove, or change the divination conclusions.
            Do NOT add labels such as "中文：".
            """;
        var glossary = FormatCardGlossary(cardNames);

        var user = $"""
            Protected card names, keep exactly as written:
            {glossary}

            English:
            {englishText.Trim()}
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

    private static string FormatCardGlossary(IEnumerable<string>? cardNames)
    {
        var names = cardNames?.Distinct().ToList();
        if (names is null || names.Count == 0)
        {
            return "- The Tower\n- The Star\n- Eight of Pentacles";
        }

        return string.Join('\n', names.Select(n => $"- {n}"));
    }
}

public record TarotPositionPrompt(
    string PositionTitle,
    string PositionContext,
    string CardName,
    bool Reversed,
    string MeaningEn);
