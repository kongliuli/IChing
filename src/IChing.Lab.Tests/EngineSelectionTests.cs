using IChing.Lab.Api.Services;
using Microsoft.Extensions.Configuration;

namespace IChing.Lab.Tests;

/// <summary>
/// 排盘引擎选择测试：验证 <see cref="LabChartEngineConfig.ResolveChartEngine"/> 能正确读取
/// <c>plugins:chartEngines[domain].default</c> 配置项。
/// </summary>
public class EngineSelectionTests
{
    private static IConfiguration BuildConfig(IDictionary<string, string?> data)
        => new ConfigurationBuilder().Add(new InMemorySource(data)).Build();

    [Fact]
    public void ResolveChartEngine_BaziConfigured_ReturnsConfiguredDefault()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "bazi-cnlunar-port"
        });

        var resolved = LabChartEngineConfig.ResolveChartEngine(config, "bazi");

        Assert.Equal("bazi-cnlunar-port", resolved);
    }

    [Fact]
    public void ResolveChartEngine_SwitchDefault_ReturnsNewValue()
    {
        var configA = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "bazi-cnlunar-port"
        });
        Assert.Equal("bazi-cnlunar-port", LabChartEngineConfig.ResolveChartEngine(configA, "bazi"));

        var configB = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "lunar-csharp-1.6.8"
        });
        Assert.Equal("lunar-csharp-1.6.8", LabChartEngineConfig.ResolveChartEngine(configB, "bazi"));
    }

    [Fact]
    public void ResolveChartEngine_NotConfigured_ReturnsNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        Assert.Null(LabChartEngineConfig.ResolveChartEngine(config, "bazi"));
    }

    [Fact]
    public void ResolveChartEngine_DomainNotMatched_ReturnsNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:chartEngines:0:domain"] = "bazi",
            ["plugins:chartEngines:0:default"] = "bazi-cnlunar-port"
        });

        Assert.Null(LabChartEngineConfig.ResolveChartEngine(config, "tarot"));
    }

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

        Assert.Equal("lunar-csharp-1.6.8", LabChartEngineConfig.ResolveChartEngine(config, "bazi"));
        Assert.Equal("iching-sixlines-2.0.3", LabChartEngineConfig.ResolveChartEngine(config, "liuyao"));
        Assert.Equal("iching-tarot-built-in", LabChartEngineConfig.ResolveChartEngine(config, "tarot"));
        Assert.Equal("lunar-csharp-1.6.8", LabChartEngineConfig.ResolveChartEngine(config, "calendar"));
    }

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
