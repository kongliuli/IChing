using IChing.Lab.Api.Controllers;
using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Tests;

/// <summary>
/// 排盘引擎选择测试：验证 <see cref="LabController.ResolveChartEngine"/> 能正确读取
/// <c>plugins:chartEngines[domain].default</c> 配置项，切换不同 default engineId 时返回值相应变化。
/// 使用最小内存配置直接验证 internal static 方法，避免构造完整 LabController 的重型依赖。
/// </summary>
public class EngineSelectionTests
{
    /// <summary>
    /// 构造一个带预置键值的 <see cref="IConfiguration"/>（避免依赖 Configuration.Memory 包）。
    /// 复用 PluginLoaderTests 内的 InMemorySource 模式。
    /// </summary>
    private static IConfiguration BuildConfig(IDictionary<string, string?> data)
        => new ConfigurationBuilder().Add(new InMemorySource(data)).Build();

    /// <summary>
    /// 配置 plugins:chartEngines[0].domain=bazi / default=bazi-cnlunar-port 时，
    /// ResolveChartEngine 返回 "bazi-cnlunar-port"。
    /// </summary>
    [Fact]
    public void ResolveChartEngine_BaziConfigured_ReturnsConfiguredDefault()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "bazi-cnlunar-port"
        });

        var resolved = LabController.ResolveChartEngine(config, "bazi");

        Assert.Equal("bazi-cnlunar-port", resolved);
    }

    /// <summary>
    /// 切换 plugins:chartEngines[domain=bazi].default 到不同值后，ResolveChartEngine 返回新值，
    /// 满足 SubTask 12.4 "切换 default → 响应字段相应变化" 的最小验证。
    /// </summary>
    [Fact]
    public void ResolveChartEngine_SwitchDefault_ReturnsNewValue()
    {
        // 第一组配置：default = bazi-cnlunar-port
        var configA = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "bazi-cnlunar-port"
        });
        Assert.Equal("bazi-cnlunar-port", LabController.ResolveChartEngine(configA, "bazi"));

        // 第二组配置：default = lunar-csharp-1.6.8（切回内置）
        var configB = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "lunar-csharp-1.6.8"
        });
        Assert.Equal("lunar-csharp-1.6.8", LabController.ResolveChartEngine(configB, "bazi"));
    }

    /// <summary>未配置 plugins:chartEngines 时返回 null（调用方回退到原硬编码逻辑）。</summary>
    [Fact]
    public void ResolveChartEngine_NotConfigured_ReturnsNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>());

        var resolved = LabController.ResolveChartEngine(config, "bazi");

        Assert.Null(resolved);
    }

    /// <summary>配置中无匹配 domain 项时返回 null。</summary>
    [Fact]
    public void ResolveChartEngine_DomainNotMatched_ReturnsNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "bazi-cnlunar-port"
        });

        var resolved = LabController.ResolveChartEngine(config, "tarot");

        Assert.Null(resolved);
    }

    /// <summary>四域全部配置时按 domain 精确匹配返回各自的 default。</summary>
    [Fact]
    public void ResolveChartEngine_AllDomains_ReturnsRespectiveDefaults()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "lunar-csharp-1.6.8",
            ["plugins:chartEngines:1:domain"] = "liuyao",
            ["plugins:chartEngines:1:default"] = "iching-sixlines-2.0.3",
            ["plugins:chartEngines:2:domain"] = "tarot",
            ["plugins:chartEngines:2:default"] = "iching-tarot-built-in",
            ["plugins:chartEngines:3:domain"] = "calendar",
            ["plugins:chartEngines:3:default"] = "lunar-csharp-1.6.8"
        });

        Assert.Equal("lunar-csharp-1.6.8", LabController.ResolveChartEngine(config, "bazi"));
        Assert.Equal("iching-sixlines-2.0.3", LabController.ResolveChartEngine(config, "liuyao"));
        Assert.Equal("iching-tarot-built-in", LabController.ResolveChartEngine(config, "tarot"));
        Assert.Equal("lunar-csharp-1.6.8", LabController.ResolveChartEngine(config, "calendar"));
    }

    /// <summary>最小内存配置源，提供预置键值（替代不可用的 Configuration.Memory 包）。</summary>
    private sealed class InMemorySource : IConfigurationSource
    {
        private readonly IDictionary<string, string?> _data;
        public InMemorySource(IDictionary<string, string?> data) => _data = data;
        public IConfigurationProvider Build(IConfigurationBuilder builder) => new InMemoryProvider(_data);
    }

    private sealed class InMemoryProvider : ConfigurationProvider
    {
        public InMemoryProvider(IDictionary<string, string?> data)
        {
            foreach (var kv in data)
            {
                Data[kv.Key] = kv.Value;
            }
        }
    }
}
