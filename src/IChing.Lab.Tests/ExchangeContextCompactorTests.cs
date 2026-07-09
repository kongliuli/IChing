using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;

namespace IChing.Lab.Tests;

public class ExchangeContextCompactorTests
{
    [Fact]
    public void SelectInitialStructured_TruncatesWhenOverBudget()
    {
        var sections = Enumerable.Range(0, 8)
            .Select(i => new ReadingStructuredSection($"s{i}", $"S{i}", new string('x', 400)))
            .ToList();
        var structured = new ReadingStructuredOutput(
            ReadingSchemas.OutputV2,
            new string('y', 400),
            sections,
            []);

        var compact = ExchangeContextCompactor.SelectInitialStructured(structured, tokenBudget: 100);
        Assert.NotNull(compact);
        Assert.True(compact!.Sections.Count <= ExchangeContextCompactor.MaxSectionsWhenTruncated);
    }

    [Fact]
    public void BuildFollowUpContext_IncludesFactsVerbatim()
    {
        var input = new ExchangeInput(
            "问事",
            "综合",
            ["fact-a"],
            ["rule-a"],
            []);
        var json = ExchangeContextCompactor.BuildFollowUpContext(input, null, [], "follow-up?");
        Assert.Contains("fact-a", json);
        Assert.Contains("rule-a", json);
        Assert.Contains("userQuestion", json);
        Assert.Contains("follow-up?", json);
    }
}
