using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Tests;

public class ExchangeInputBuilderTests
{
    [Fact]
    public void ForTarot_IncludesSpreadInComputedFacts()
    {
        var reading = new TarotReading(
            "past-present-future",
            "Past Present Future",
            "过去现在未来",
            "desc",
            false,
            "问",
            1,
            [
                new TarotPositionReading(
                    "past", "Past", "过去", "ctx",
                    "The Fool", "愚者", "url", false, "新开始")
            ]);
        var input = ExchangeInputBuilder.ForTarot(reading, "问");

        Assert.Contains("spread: 过去现在未来", input.ComputedFacts);
        Assert.Equal("问", input.Question);
        Assert.Single(input.RuleDigest);
    }
}

public class ExchangeInferenceRouterTests
{
    [Fact]
    public void BuildInitialContext_PreservesStructuredDigest()
    {
        var input = new ExchangeInput(null, "事业", ["fact"], ["rule"], []);
        var exchange = ReadingExchangeFactory.CreateInitial("bazi", 1, input, null, "bazi-tier1-default");
        var structured = new { pillar = "stub" };
        var ctx = ExchangeInferenceRouter.BuildInitialContext(exchange, input, structured, null, null, 512);

        Assert.Same(structured, ctx.RuleDigest);
        Assert.Equal("事业", ctx.Focus);
    }
}
