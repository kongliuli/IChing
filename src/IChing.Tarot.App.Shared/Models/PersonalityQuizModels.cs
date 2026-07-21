namespace IChing.Tarot.App.Models;

public sealed class PersonalityQuizDefinition
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public required string Scoring { get; init; }
    public required string Disclaimer { get; init; }
    public required List<PersonalityQuestion> Questions { get; init; }
}

public sealed class PersonalityQuestion
{
    public required string Text { get; init; }
    public required List<PersonalityOption> Options { get; init; }
}

public sealed class PersonalityOption
{
    public required string Text { get; init; }
    public Dictionary<string, int> Scores { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record PersonalityReportSection(string Title, string Content);

public sealed record PersonalityDimensionBar(
    string Title,
    string LeftLabel,
    string RightLabel,
    int LeftPercent);

public sealed record PersonalityQuizResult(
    string Code,
    string Title,
    string Summary,
    string Detail,
    IReadOnlyDictionary<string, int> DimensionScores,
    IReadOnlyList<PersonalityDimensionBar> DimensionBars,
    IReadOnlyList<PersonalityReportSection> Sections);

public sealed record PersonalityQuizListItem(string Id, string Title, string Subtitle, int QuestionCount);
