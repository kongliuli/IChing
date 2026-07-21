using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

public static class PersonalityQuizReportBuilder
{
    public static PersonalityQuizResult Build(
        string scoring,
        string code,
        string title,
        string summary,
        string detail,
        IReadOnlyDictionary<string, int> totals)
    {
        return scoring switch
        {
            "mbti16" => BuildMbti(code, title, summary, detail, totals),
            "enneagram9" => BuildEnneagram(code, title, summary, detail, totals),
            "holland" => BuildHolland(code, title, summary, detail, totals),
            _ => new PersonalityQuizResult(code, title, summary, detail, totals, [], [])
        };
    }

    private static PersonalityQuizResult BuildMbti(
        string code,
        string title,
        string summary,
        string detail,
        IReadOnlyDictionary<string, int> totals)
    {
        var report = PersonalityTypeCopy.Mbti16Report(code);
        var bars = new[]
        {
            PairBar("能量来源", "外向 E", "内向 I", "E", "I", totals),
            PairBar("信息感知", "实感 S", "直觉 N", "S", "N", totals),
            PairBar("决策方式", "思考 T", "情感 F", "T", "F", totals),
            PairBar("生活方式", "判断 J", "感知 P", "J", "P", totals)
        };
        return new PersonalityQuizResult(
            code,
            report.Title,
            report.Summary,
            detail,
            totals,
            bars,
            report.Sections);
    }

    private static PersonalityQuizResult BuildEnneagram(
        string code,
        string title,
        string summary,
        string detail,
        IReadOnlyDictionary<string, int> totals)
    {
        if (!int.TryParse(code, out var primary))
        {
            primary = 1;
        }

        var ranked = Enumerable.Range(1, 9)
            .Select(n => (Type: n, Score: totals.GetValueOrDefault(n.ToString())))
            .OrderByDescending(x => x.Score)
            .ToList();

        var report = PersonalityTypeCopy.EnneagramReport(primary, ranked);
        var bars = ranked
            .Take(5)
            .Select(x => ScoreBar($"{x.Type} 号", x.Score, ranked[0].Score))
            .ToList();

        return new PersonalityQuizResult(code, report.Title, report.Summary, detail, totals, bars, report.Sections);
    }

    private static PersonalityQuizResult BuildHolland(
        string code,
        string title,
        string summary,
        string detail,
        IReadOnlyDictionary<string, int> totals)
    {
        var order = new[] { "R", "I", "A", "S", "E", "C" };
        var ranked = order
            .Select(k => (Key: k, Score: totals.GetValueOrDefault(k)))
            .OrderByDescending(x => x.Score)
            .ToList();

        var report = PersonalityTypeCopy.HollandReport(code, ranked);
        var max = Math.Max(1, ranked[0].Score);
        var bars = order
            .Select(k =>
            {
                var score = totals.GetValueOrDefault(k);
                var pct = (int)Math.Round(100.0 * score / max);
                return new PersonalityDimensionBar(PersonalityTypeCopy.HollandDimensionName(k), $"{pct}%", "", pct);
            })
            .ToList();

        return new PersonalityQuizResult(code, report.Title, report.Summary, detail, totals, bars, report.Sections);
    }

    private static PersonalityDimensionBar PairBar(
        string title,
        string left,
        string right,
        string leftKey,
        string rightKey,
        IReadOnlyDictionary<string, int> totals)
    {
        var leftScore = totals.GetValueOrDefault(leftKey);
        var rightScore = totals.GetValueOrDefault(rightKey);
        var sum = leftScore + rightScore;
        var leftPct = sum > 0 ? (int)Math.Round(100.0 * leftScore / sum) : 50;
        return new PersonalityDimensionBar(title, left, right, leftPct);
    }

    private static PersonalityDimensionBar ScoreBar(string title, int score, int maxScore)
    {
        var pct = maxScore > 0 ? (int)Math.Round(100.0 * score / maxScore) : 0;
        return new PersonalityDimensionBar(title, $"{score} 分", "", pct);
    }
}
