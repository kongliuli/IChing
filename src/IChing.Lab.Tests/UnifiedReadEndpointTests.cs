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
