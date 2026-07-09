using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Readings.Producers;
using IChing.Lab.Core.Rules;

namespace IChing.Lab.Tests;

public class ReadingResultProducerTests
{
    [Fact]
    public void BaziProducer_AddsPillarWidget()
    {
        var chart = BaziEngine.Calculate(new BaziInput(1990, 5, 20, 10, Gender: 1));
        var output = ReadingOutputParser.BuildExchangeOutput(
            "bazi",
            """{"schema":"reading-output.v2","summary":"测试","sections":[{"key":"overview","title":"总论","body":"正文"}],"warnings":[]}""",
            null,
            "test",
            false);
        var exchange = ReadingExchangeFactory.CreateInitial(
            "bazi",
            1,
            new ExchangeInput(null, "综合", [], [], []),
            output);
        var vm = ReadingResultProducerRegistry.Produce(exchange, chart);
        Assert.Equal("core.bazi", vm.ProducerId);
        Assert.Contains(vm.Widgets, w => w.Kind == "pillarGrid");
        Assert.Contains(vm.Summary, "测试");
    }

    [Fact]
    public void EntertainmentProducer_BuildsDimensionBars()
    {
        var input = new QuizProducerInput(
            "mbti16",
            "INTJ",
            "建筑师",
            "summary",
            "detail",
            new Dictionary<string, int> { ["E"] = 1, ["I"] = 9 },
            [new QuizDimensionBar("能量", "E", "I", 10)]);
        var exchange = ReadingExchangeFactory.CreateEntertainment(input);
        var vm = ReadingResultProducerRegistry.Produce(exchange, input);
        Assert.Equal("entertainment.quiz", vm.ProducerId);
        Assert.Contains(vm.Widgets, w => w.Kind == "dimensionBars");
    }
}

public class RulePromptExtensionRegistryTests
{
    [Fact]
    public void Merge_ActiveYongshen_AddsSection()
    {
        var merged = RulePromptExtensionRegistry.Merge(["bazi.yongshen.current"]);
        Assert.Contains(merged.OutputSections, s => s.Key == "yongshen");
        Assert.NotEmpty(merged.SystemDirectives);
    }
}
