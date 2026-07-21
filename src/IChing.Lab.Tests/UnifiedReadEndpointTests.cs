using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IChing.Lab.Tests;

/// <summary>集成测试专用 WebApplicationFactory：Testing 环境不加载 plugins/ 外部 DLL。</summary>
public sealed class LabApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment("Testing");
}

public class UnifiedReadEndpointTests : IClassFixture<LabApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UnifiedReadEndpointTests(LabApiWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task UnifiedRead_BaziTier0_ReturnsEnvelope()
    {
        var response = await _client.PostAsJsonAsync(
            "/lab/bazi/read?tier=0",
            new { year = 1990, month = 5, day = 20, hour = 10, gender = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("reading-envelope.v2", json);
        Assert.Contains("tier0Preview", json);
        Assert.Contains("disclaimer", json);
        Assert.Contains("\"exchange\"", json);
        Assert.Contains("computedFacts", json);
        Assert.Contains("dayMaster", json);
    }

    [Fact]
    public async Task LabChat_RegisterAndFollowUp_ReturnsExchange()
    {
        var register = await _client.PostAsJsonAsync("/lab/chat", new
        {
            mode = "register",
            domain = "bazi",
            tier = 1,
            input = new
            {
                question = (string?)null,
                focus = "综合",
                computedFacts = new[] { "pillars: 甲子 丙寅 戊辰 庚午" },
                ruleDigest = new[] { "summary" },
                pluginContext = Array.Empty<object>()
            },
            initialOutput = "初始解读文本"
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var regJson = await register.Content.ReadAsStringAsync();
        Assert.Contains("sessionId", regJson);

        using var doc = System.Text.Json.JsonDocument.Parse(regJson);
        var sessionId = doc.RootElement.GetProperty("sessionId").GetString();
        var follow = await _client.PostAsJsonAsync("/lab/chat", new
        {
            mode = "followup",
            sessionId,
            userQuestion = "今年运势如何？",
            tier = 1
        });
        Assert.Equal(HttpStatusCode.OK, follow.StatusCode);
    }

    [Fact]
    public async Task LabChat_InitialWithoutDomain_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/lab/chat", new { mode = "initial" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LabChat_RegisterWithChart_AcceptsSession()
    {
        var register = await _client.PostAsJsonAsync("/lab/chat", new
        {
            mode = "register",
            domain = "tarot",
            tier = 1,
            input = new
            {
                question = "复合?",
                focus = (string?)null,
                computedFacts = new[] { "spread: 三张" },
                ruleDigest = new[] { "含义" },
                pluginContext = Array.Empty<object>()
            },
            initialOutput = "初始",
            chart = new { spreadTitleZh = "过去现在未来", positions = Array.Empty<object>() }
        });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
    }

    [Fact]
    public async Task LabChat_AppendHistory_UpdatesSession()
    {
        var register = await _client.PostAsJsonAsync("/lab/chat", new
        {
            mode = "register",
            domain = "bazi",
            tier = 1,
            input = new
            {
                question = (string?)null,
                focus = "综合",
                computedFacts = new[] { "fact" },
                ruleDigest = new[] { "rule" },
                pluginContext = Array.Empty<object>()
            },
            initialOutput = "初始"
        });
        using var regDoc = System.Text.Json.JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        var sessionId = regDoc.RootElement.GetProperty("sessionId").GetString();

        var append = await _client.PostAsJsonAsync("/lab/chat", new
        {
            mode = "append",
            sessionId,
            userQuestion = "追问1",
            assistantReply = "回答1"
        });
        Assert.Equal(HttpStatusCode.OK, append.StatusCode);
        var appendJson = await append.Content.ReadAsStringAsync();
        Assert.Contains("\"rounds\":1", appendJson.Replace(" ", ""));
    }

    [Fact]
    public async Task CreditsConsume_WhenAccountsDisabled_ReturnsOkSkipped()
    {
        var response = await _client.PostAsJsonAsync(
            "/lab/credits/consume",
            new { exchangeId = "test-exchange-1", domain = "bazi", mode = "followup", tier = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"ok\":true", json.Replace(" ", ""));
    }
}
