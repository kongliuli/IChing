using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Tests;

/// <summary>
/// 排盘引擎发现测试：验证四域（bazi/liuyao/tarot/calendar）各至少 5 个 IChartEngine 注册，
/// 且每条引擎元数据的 source / algorithmBasis 非空，确保插件模块装配完整、metadata 已填充。
/// </summary>
public class EngineDiscoveryTests
{
    private static IReadOnlyList<IChartEngine> BuildAllEngines()
    {
        var services = new ServiceCollection();
        services.AddLabChartEngines();
        return services.BuildServiceProvider().GetRequiredService<IEnumerable<IChartEngine>>().ToList();
    }

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
