using System.Text;
using System.Text.Json;
using IChing.Lab.Abstractions.Readings;

namespace IChing.Lab.Core.Readings;

/// <summary>
/// 追问上下文压缩：L1 facts 原文 + L2 initial structured（超预算规则截断）+ L3 rolling history。
/// </summary>
public static class ExchangeContextCompactor
{
    public const int DefaultTokenBudget = 6000;
    public const double TruncateThreshold = 0.70;
    public const int MaxRollingTurns = 2;
    public const int MaxSectionsWhenTruncated = 3;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string BuildFollowUpContext(
        ExchangeInput input,
        ReadingStructuredOutput? initialStructured,
        IReadOnlyList<DialogueTurn> rollingHistory,
        string? pendingQuestion = null,
        int tokenBudget = DefaultTokenBudget)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["computedFacts"] = input.ComputedFacts,
            ["ruleDigest"] = input.RuleDigest,
            ["pluginContext"] = input.PluginContext,
            ["question"] = input.Question,
            ["focus"] = input.Focus,
            ["initialStructured"] = SelectInitialStructured(initialStructured, tokenBudget),
            ["history"] = TrimRollingHistory(rollingHistory),
            ["userQuestion"] = pendingQuestion
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static ReadingStructuredOutput? SelectInitialStructured(
        ReadingStructuredOutput? structured,
        int tokenBudget = DefaultTokenBudget)
    {
        if (structured is null)
        {
            return null;
        }

        var estimated = EstimateTokens(structured);
        if (estimated <= tokenBudget * TruncateThreshold)
        {
            return structured;
        }

        var sections = structured.Sections.Take(MaxSectionsWhenTruncated).ToList();
        return structured with { Sections = sections };
    }

    public static IReadOnlyList<DialogueTurn> TrimRollingHistory(IReadOnlyList<DialogueTurn> history) =>
        history.Count <= MaxRollingTurns * 2
            ? history
            : history.Skip(history.Count - MaxRollingTurns * 2).ToList();

    public static int EstimateTokens(ReadingStructuredOutput structured)
    {
        var sb = new StringBuilder(structured.Summary);
        foreach (var section in structured.Sections)
        {
            sb.Append(section.Title).Append(section.Body);
        }

        return Math.Max(1, sb.Length / 4);
    }
}
