using System.Net.Http.Json;
using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Calendar;
using IChing.Lab.Core.Engines;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Services;
using IChing.Lab.Engines.Bazi;
using IChing.Lab.Engines.Calendar;
using IChing.Lab.Engines.Liuyao;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Tests;

public class BaziLiuyaoRoutingTests
{
    [Fact]
    public void CalculateCalendar_BuiltinChain_ReturnsHuangLiDay()
    {
        var router = BuildRouter(includeCalendar: true);
        var args = new Dictionary<string, object?> { ["year"] = 2026, ["month"] = 1, ["day"] = 1, ["sect"] = 1 };
        var result = router.Calculate("calendar", args, ["lunar-csharp-1.6.8"]);
        var day = ChartResultMapper.AsCalendarDay(result.Result, 2026, 1, 1, 1);
        Assert.Equal("lunar-csharp-1.6.8", result.EngineId);
        Assert.False(string.IsNullOrWhiteSpace(day.Solar));
        Assert.NotEmpty(day.Yi);
    }

    private static ChartEngineRouter BuildRouter(bool includeCalendar = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IChing.Lab.Abstractions.Engines.IChartEngine, BaziChartEngine>();
        services.AddSingleton<IChing.Lab.Abstractions.Engines.IChartEngine, LiuyaoChartEngine>();
        if (includeCalendar)
        {
            services.AddSingleton<IChing.Lab.Abstractions.Engines.IChartEngine, CalendarEngine>();
            new CalendarEnginesModule().Register(services);
        }

        new BaziEnginesModule().Register(services);
        new LiuyaoEnginesModule().Register(services);
        var sp = services.BuildServiceProvider();
        return new ChartEngineRouter(sp.GetServices<IChing.Lab.Abstractions.Engines.IChartEngine>());
    }

    [Fact]
    public void CalculateBazi_BuiltinChain_ReturnsBaziChart()
    {
        var router = BuildRouter();
        var input = new BaziInput(1990, 5, 20, 10, Gender: 1);
        var json = System.Text.Json.JsonSerializer.Serialize(input);
        var args = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
        var result = router.Calculate("bazi", args, ["lunar-csharp-1.6.8"]);
        var chart = ChartResultMapper.AsBaziChart(result.Result, input);
        Assert.Equal("lunar-csharp-1.6.8", result.EngineId);
        Assert.False(string.IsNullOrWhiteSpace(chart.DayPillar.GanZhi));
    }

    [Fact]
    public void CalculateLiuyao_BuiltinChain_ReturnsHexagram()
    {
        var router = BuildRouter();
        var args = new Dictionary<string, object?> { ["method"] = "coin", ["seed"] = 42 };
        var result = router.Calculate("liuyao", args, ["iching-sixlines-2.0.3"]);
        var chart = ChartResultMapper.AsLiuyaoChart(result.Result, "coin", DateTimeOffset.Now, 42);
        Assert.Equal("iching-sixlines-2.0.3", result.EngineId);
        Assert.False(string.IsNullOrWhiteSpace(chart.OriginalHexagram));
    }

    [Fact]
    public async Task Sidecar_BaziEndpoint_WhenRunning_ReturnsChart()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var health = await client.GetAsync("http://127.0.0.1:5001/health");
            if (!health.IsSuccessStatusCode)
            {
                return;
            }

            using var response = await client.PostAsJsonAsync("http://127.0.0.1:5001/bazi", new
            {
                args = new { year = 1990, month = 5, day = 20, hour = 10, gender = 1 }
            });

            Assert.True(response.IsSuccessStatusCode);
            var chart = await response.Content.ReadFromJsonAsync<BaziChart>();
            Assert.NotNull(chart);
            Assert.False(string.IsNullOrWhiteSpace(chart!.DayPillar.GanZhi));
        }
        catch (HttpRequestException)
        {
            // sidecar 未启动时跳过
        }
        catch (TaskCanceledException)
        {
            // sidecar 未启动时跳过
        }
    }

    [Fact]
    public async Task Sidecar_TarotEndpoint_WhenRunning_ReturnsEnrichedReading()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var health = await client.GetAsync("http://127.0.0.1:5001/health");
            if (!health.IsSuccessStatusCode)
            {
                return;
            }

            using var response = await client.PostAsJsonAsync("http://127.0.0.1:5001/tarot", new
            {
                args = new { spreadId = "single-card", question = "test", seed = 42 }
            });

            Assert.True(response.IsSuccessStatusCode);
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            Assert.True(doc.RootElement.TryGetProperty("reading", out var reading));
            Assert.True(reading.TryGetProperty("positions", out var positions));
            Assert.True(positions.GetArrayLength() > 0);
            Assert.True(doc.RootElement.TryGetProperty("engine", out var engine));
            Assert.Equal("iching-tarot-built-in", engine.GetProperty("paipan").GetString());
        }
        catch (HttpRequestException)
        {
            // sidecar 未启动时跳过
        }
        catch (TaskCanceledException)
        {
            // sidecar 未启动时跳过
        }
    }

    [Fact]
    public async Task Sidecar_CalendarEndpoint_WhenRunning_ReturnsHuangLiDay()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var health = await client.GetAsync("http://127.0.0.1:5001/health");
            if (!health.IsSuccessStatusCode)
            {
                return;
            }

            using var response = await client.PostAsJsonAsync("http://127.0.0.1:5001/calendar", new
            {
                args = new { year = 2026, month = 1, day = 1, sect = 1 }
            });

            Assert.True(response.IsSuccessStatusCode);
            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            Assert.True(doc.RootElement.TryGetProperty("day", out var dayEl));
            var day = JsonSerializer.Deserialize<HuangLiDay>(dayEl.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            Assert.NotNull(day);
            Assert.False(string.IsNullOrWhiteSpace(day!.Solar));
        }
        catch (HttpRequestException)
        {
            // sidecar 未启动时跳过
        }
        catch (TaskCanceledException)
        {
            // sidecar 未启动时跳过
        }
    }

    [Fact]
    public async Task Sidecar_LiuyaoEndpoint_WhenRunning_ReturnsHexagram()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            using var health = await client.GetAsync("http://127.0.0.1:5001/health");
            if (!health.IsSuccessStatusCode)
            {
                return;
            }

            using var response = await client.PostAsJsonAsync("http://127.0.0.1:5001/liuyao", new
            {
                args = new { method = "coin", seed = 7 }
            });

            Assert.True(response.IsSuccessStatusCode);
            var chart = await response.Content.ReadFromJsonAsync<LiuyaoNajiaResult>();
            Assert.NotNull(chart);
            Assert.False(string.IsNullOrWhiteSpace(chart!.OriginalHexagram));
        }
        catch (HttpRequestException)
        {
            // sidecar 未启动时跳过
        }
        catch (TaskCanceledException)
        {
            // sidecar 未启动时跳过
        }
    }
}
