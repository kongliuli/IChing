using IChing.Lab.Abstractions.Readings;
using IChing.Lab.Core.Readings;

namespace IChing.Lab.Tests;

public class FollowUpExchangeTests
{
    [Fact]
    public void CreateFollowUp_SetsModeAndDialogue()
    {
        var input = new ExchangeInput("问", "综合", ["fact"], ["rule"], []);
        var exchange = ReadingExchangeFactory.CreateFollowUp(
            input,
            "bazi",
            1,
            "sess1",
            "parent1",
            "还能换工作吗？",
            [],
            null);

        Assert.Equal("followup", exchange.Meta.Mode);
        Assert.Equal("还能换工作吗？", exchange.Dialogue?.UserQuestion);
        Assert.Equal("core-followup-json", exchange.Render.PromptTemplateId);
    }

    [Fact]
    public void ToFollowUpPacket_IncludesPriorAndUserQuestion()
    {
        var input = new ExchangeInput("问", "综合", ["fact"], ["rule"], []);
        var exchange = ReadingExchangeFactory.CreateFollowUp(
            input, "bazi", 1, "s", "p", "追问?", [], null);
        var packet = ExchangePromptAdapter.ToFollowUpPacket(exchange, null, "初始解读");
        var json = ReadingPromptProtocol.BuildUserMessage(packet);

        Assert.Contains("prior", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("userQuestion", json, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("followup", packet.Mode);
        Assert.Equal("追问?", packet.UserQuestion);
    }

    [Fact]
    public void FromExchange_MapsQuestionFromDialogue()
    {
        var input = new ExchangeInput(null, "综合", [], [], []);
        var exchange = ReadingExchangeFactory.CreateFollowUp(
            input, "liuyao", 1, "s", "p", "世应如何?", [], null);
        var ctx = ExchangePromptAdapter.FromExchange(exchange, null);
        Assert.Equal("世应如何?", ctx.Question);
    }
}

public class EntitlementGateTests
{
    [Fact]
    public void CheckEntertainmentAi_WithoutUser_Denies()
    {
        var d = IChing.Lab.Core.Readings.Entitlements.EntitlementGate.CheckEntertainmentAi(null, "quiz");
        Assert.False(d.Allowed);
    }
}
