using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Core.Engines;
using IChing.Lab.Engines.Bazi;
using IChing.Lab.Engines.Calendar;
using IChing.Lab.Engines.Liuyao;
using IChing.Lab.Engines.Tarot;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Tests;

/// <summary>
/// 排盘引擎发现测试：验证四域（bazi/liuyao/tarot/calendar）各至少 5 个 IChartEngine 注册，
/// 且每条引擎元数据的 source / algorithmBasis 非空，确保插件模块装配完整、metadata 已填充。
/// 通过直接构造各 Module 的 ServiceProvider 装配 4 个内置包装器 + 5 个插件引擎，避免启动完整 Web 宿主。
/// </summary>
public class EngineDiscoveryTests
{
    /// <summary>
    /// 装配全部已注册的 IChartEngine：4 个内置包装器（BaziChartEngine/LiuyaoChartEngine/TarotChartEngine/CalendarEngine）
    /// 加上 4 个域各 5 个插件引擎（BaziEnginesModule / LiuyaoEnginesModule / TarotEnginesModule / CalendarEnginesModule），
    /// 共 24 个 IChartEngine 实例。
    /// </summary>
    private static IReadOnlyList<IChartEngine> BuildAllEngines()
    {
        var services = new ServiceCollection();
        // 4 个内置包装器（与 Program.cs 注册顺序一致）
        services.AddSingleton<IChartEngine, BaziChartEngine>();
        services.AddSingleton<IChartEngine, LiuyaoChartEngine>();
        services.AddSingleton<IChartEngine, TarotChartEngine>();
        services.AddSingleton<IChartEngine, CalendarEngine>();
        // 4 个域各 5 个插件引擎
        new BaziEnginesModule().Register(services);
        new LiuyaoEnginesModule().Register(services);
        new TarotEnginesModule().Register(services);
        new CalendarEnginesModule().Register(services);
        return services.BuildServiceProvider().GetRequiredService<IEnumerable<IChartEngine>>().ToList();
    }

    /// <summary>四域各至少 5 条 IChartEngine 注册（实际每域 6 条：1 内置 + 5 插件）。</summary>
    [Fact]
    public void AllDomains_HaveAtLeastFiveEngines()
    {
        var engines = BuildAllEngines();

        var byDomain = engines.GroupBy(e => e.Domain).ToDictionary(g => g.Key, g => g.ToList());

        Assert.Contains("bazi", byDomain.Keys);
        Assert.Contains("liuyao", byDomain.Keys);
        Assert.Contains("tarot", byDomain.Keys);
        Assert.Contains("calendar", byDomain.Keys);

        Assert.True(byDomain["bazi"].Count >= 5, $"bazi 域引擎数应 ≥5，实际 {byDomain["bazi"].Count}");
        Assert.True(byDomain["liuyao"].Count >= 5, $"liuyao 域引擎数应 ≥5，实际 {byDomain["liuyao"].Count}");
        Assert.True(byDomain["tarot"].Count >= 5, $"tarot 域引擎数应 ≥5，实际 {byDomain["tarot"].Count}");
        Assert.True(byDomain["calendar"].Count >= 5, $"calendar 域引擎数应 ≥5，实际 {byDomain["calendar"].Count}");
    }

    /// <summary>每条引擎元数据的 source / algorithmBasis 字段非空，确保 /lab/engines 返回完整 metadata。</summary>
    [Fact]
    public void EveryEngine_MetadataSourceAndAlgorithmBasis_NonEmpty()
    {
        var engines = BuildAllEngines();

        Assert.NotEmpty(engines);
        foreach (var engine in engines)
        {
            Assert.False(string.IsNullOrWhiteSpace(engine.Metadata.Source),
                $"{engine.Domain}/{engine.EngineId} 的 Metadata.Source 不应为空");
            Assert.False(string.IsNullOrWhiteSpace(engine.Metadata.AlgorithmBasis),
                $"{engine.Domain}/{engine.EngineId} 的 Metadata.AlgorithmBasis 不应为空");
        }
    }

    /// <summary>每条引擎的 Domain / EngineId 字段非空，且同域 EngineId 唯一。</summary>
    [Fact]
    public void EveryEngine_DomainAndEngineId_UniquePerDomain()
    {
        var engines = BuildAllEngines();

        var byDomain = engines.GroupBy(e => e.Domain).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var kv in byDomain)
        {
            var ids = kv.Value.Select(e => e.EngineId).ToList();
            Assert.All(ids, id => Assert.False(string.IsNullOrWhiteSpace(id)));
            Assert.Equal(ids.Count, ids.Distinct().Count());
        }
    }
}
