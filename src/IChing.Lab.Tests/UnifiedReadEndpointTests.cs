using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IChing.Lab.Tests;

public class UnifiedReadEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UnifiedReadEndpointTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task UnifiedRead_BaziTier0_ReturnsEnvelope()
    {
        var response = await _client.PostAsJsonAsync(
            "/lab/bazi/read?tier=0",
            new { year = 1990, month = 5, day = 20, hour = 10, gender = 1 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"domain\":\"bazi\"", json.Replace(" ", ""));
        Assert.Contains("tier0Preview", json);
        Assert.Contains("disclaimer", json);
    }

    [Fact]
    public async Task UnifiedRead_UnknownDomain_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            "/lab/calendar/read?tier=0",
            new { year = 2026, month = 1, day = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
