using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Engines.Bazi;

namespace IChing.Lab.Tests;

/// <summary>
/// BaziCnlunarPortEngine 单元测试：验证 2026 立春后某日的建除十二神计算非空、
/// 宜忌等第表非空，确保 C# 移植引擎产出真实算法结果而非桩数据。
/// </summary>
public class BaziCnlunarPortEngineTests
{
    private static readonly string[] ExpectedJianChu =
        ["建", "除", "满", "平", "定", "执", "破", "危", "成", "收", "开", "闭"];

    /// <summary>
    /// 2026 立春（2 月 4 日）后某日排盘：月支应为寅（立春后寅月），
    /// 建除十二神应非空且落在已知 12 神集合内。
    /// </summary>
    [Fact]
    public void Calculate_AfterLichun_2026_ReturnsNonEmptyJianChu()
    {
        var engine = new BaziCnlunarPortEngine();
        var request = new ChartRequest("bazi", new Dictionary<string, object?>
        {
            ["year"] = 2026,
            ["month"] = 2,
            ["day"] = 10,
            ["hour"] = 12,
            ["minute"] = 0,
            ["second"] = 0
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // engine 元信息
        Assert.True(root.TryGetProperty("engine", out var engineEl));
        Assert.Equal("bazi-cnlunar-port", engineEl.GetProperty("paipan").GetString());
        Assert.Equal("cnlunar", engineEl.GetProperty("source").GetString());

        // 月支应为寅（2026 立春后进入寅月）
        Assert.True(root.TryGetProperty("monthZhi", out var monthZhiEl));
        var monthZhi = monthZhiEl.GetString();
        Assert.Equal("寅", monthZhi);

        // 建除十二神非空且在 12 神集合内
        Assert.True(root.TryGetProperty("jianchu", out var jianchuEl));
        var jianchu = jianchuEl.GetString();
        Assert.False(string.IsNullOrWhiteSpace(jianchu));
        Assert.Contains(jianchu, ExpectedJianChu);
    }

    /// <summary>
    /// 宜忌等第表非空：宜/忌数组各至少 3 条，等第为吉/平/凶之一。
    /// </summary>
    [Fact]
    public void Calculate_AfterLichun_2026_ReturnsNonEmptyYiJi()
    {
        var engine = new BaziCnlunarPortEngine();
        var request = new ChartRequest("bazi", new Dictionary<string, object?>
        {
            ["year"] = 2026,
            ["month"] = 3,
            ["day"] = 15,
            ["hour"] = 9,
            ["minute"] = 30,
            ["second"] = 0
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("yiji", out var yijiEl));
        Assert.True(yijiEl.TryGetProperty("宜", out var yiEl));
        Assert.True(yijiEl.TryGetProperty("忌", out var jiEl));
        Assert.True(yijiEl.TryGetProperty("等第", out var dengDiEl));

        Assert.Equal(JsonValueKind.Array, yiEl.ValueKind);
        Assert.Equal(JsonValueKind.Array, jiEl.ValueKind);
        Assert.True(yiEl.GetArrayLength() >= 3, "宜数组应至少 3 条");
        Assert.True(jiEl.GetArrayLength() >= 3, "忌数组应至少 3 条");

        var dengDi = dengDiEl.GetString();
        Assert.Contains(dengDi, new[] { "吉", "平", "凶" });
    }

    /// <summary>
    /// 建除十二神算法自验：月支寅、日支寅应为建；月支寅、日支卯应为除。
    /// </summary>
    [Theory]
    [InlineData("寅", "寅", "建")]
    [InlineData("寅", "卯", "除")]
    [InlineData("寅", "辰", "满")]
    [InlineData("卯", "卯", "建")]
    [InlineData("子", "亥", "闭")] // 亥在子前一位，偏移 -1 ≡ 11，对应闭
    [InlineData("子", "子", "建")]
    public void ResolveJianChu_KnownPairs_MapsCorrectly(string monthZhi, string dayZhi, string expected)
    {
        var actual = BaziCnlunarPortEngine.ResolveJianChu(monthZhi, dayZhi);
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// 桥接引擎在 sidecar 不可达时应返回错误对象而非抛异常（Openfate 示例）。
    /// </summary>
    [Fact]
    public void BridgeEngine_SidecarUnreachable_ReturnsErrorObject()
    {
        // 默认 HttpClient 指向不存在的端口，探活必然失败
        var engine = new BaziOpenfateBridgeEngine();
        var request = new ChartRequest("bazi", new Dictionary<string, object?>
        {
            ["year"] = 2026,
            ["month"] = 2,
            ["day"] = 10,
            ["hour"] = 12
        });

        var result = engine.Calculate(request);
        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("error", out var errorEl));
        Assert.Equal("sidecar unavailable", errorEl.GetString());
    }

    /// <summary>
    /// 引擎元数据完整：Domain/EngineId/Metadata 字段符合 spec。
    /// </summary>
    [Fact]
    public void Metadata_FieldsMatchSpec()
    {
        var engine = new BaziCnlunarPortEngine();
        Assert.Equal("bazi", engine.Domain);
        Assert.Equal("bazi-cnlunar-port", engine.EngineId);
        Assert.Equal("cnlunar", engine.Metadata.Source);
        Assert.Equal("0.2.4-port", engine.Metadata.Version);
        Assert.Equal("cnlunar", engine.Metadata.TemplateHint);
        Assert.Contains("yiji", engine.Metadata.ModuleFocus);
        Assert.Contains("dengdi", engine.Metadata.ModuleFocus);
    }
}
