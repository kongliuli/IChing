using IChing.Lab.PluginLoader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// 类型 PluginLoader 与命名空间 IChing.Lab.PluginLoader 同名，在 IChing.Lab.Tests 内未限定名会解析为命名空间，
// 故以别名 PluginLoaderType 显式绑定到类型，避免 CS0118。
using PluginLoaderType = IChing.Lab.PluginLoader.PluginLoader;

namespace IChing.Lab.Tests;

/// <summary>
/// <see cref="VersionCompatibilityChecker"/> 的 SemVer 兼容性单元测试。
/// </summary>
public class VersionCompatibilityCheckerTests
{
    [Theory]
    [InlineData("0.1", "0.1", true)]   // 完全相等
    [InlineData("0.1", "0.2", true)]   // major 相同即兼容
    [InlineData("0.1", "0.9", true)]
    [InlineData("0", "0.5", true)]
    [InlineData("1.0", "0.1", false)]  // major 不同
    [InlineData("99.0", "0.1", false)]
    [InlineData("2.3.4", "2.9.1", true)]
    [InlineData("3.0", "2.0", false)]
    [InlineData("", "0.1", false)]     // 空字符串无法解析
    [InlineData(null, "0.1", false)]   // null 无法解析
    [InlineData("abc", "0.1", false)]  // 非数字 major
    public void IsCompatible_MajorMatch(string? required, string current, bool expected)
    {
        Assert.Equal(expected, VersionCompatibilityChecker.IsCompatible(required, current));
    }
}

/// <summary>
/// <see cref="IChing.Lab.PluginLoader.PluginLoader"/> 的端到端集成测试：加载示例插件、注册服务、卸载回收。
/// </summary>
public class PluginLoaderTests
{
    private static ILogger<PluginLoaderType> Logger => NullLogger<PluginLoaderType>.Instance;

    /// <summary>
    /// 在仓库内查找已编译的 SamplePlugin.dll（优先 plugins/，其次示例项目输出目录）。
    /// </summary>
    private static string? FindSamplePluginDll()
    {
        var candidates = new List<string>
        {
            Path.Combine(Environment.CurrentDirectory, "plugins", "SamplePlugin.dll"),
            Path.Combine(Environment.CurrentDirectory, "samples", "SamplePlugin", "bin", "Debug", "net10.0", "SamplePlugin.dll")
        };

        // 从测试输出目录向上查找仓库根（含 src/IChing.Lab.sln），再拼接候选路径。
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "IChing.Lab.sln")))
            {
                candidates.Add(Path.Combine(dir.FullName, "plugins", "SamplePlugin.dll"));
                candidates.Add(Path.Combine(dir.FullName, "samples", "SamplePlugin", "bin", "Debug", "net10.0", "SamplePlugin.dll"));
                break;
            }
            dir = dir.Parent;
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// 构造一个带预置键值的 <see cref="IConfiguration"/>（避免依赖 Configuration.Memory 包）。
    /// </summary>
    private static IConfiguration BuildConfig(IDictionary<string, string?> data)
        => new ConfigurationBuilder().Add(new InMemorySource(data)).Build();

    [Fact]
    public void Discover_EmptyConfig_ReturnsNoManifests()
    {
        var loader = new PluginLoaderType(new ConfigurationBuilder().Build(), Logger);

        var manifests = loader.Discover();

        Assert.Empty(manifests);
    }

    [Fact]
    public void DiscoverAndRegister_EmptyConfig_RegistersNothing()
    {
        var loader = new PluginLoaderType(new ConfigurationBuilder().Build(), Logger);
        var services = new ServiceCollection();

        loader.DiscoverAndRegister(services);

        // 无外部插件时不应注册任何服务。
        Assert.Empty(services);
    }

    [Fact]
    public void DiscoverAndRegister_WithSamplePlugin_RegistersMarkerService()
    {
        var dllPath = FindSamplePluginDll();
        Assert.True(dllPath is not null, "未找到 SamplePlugin.dll，请先构建 samples/SamplePlugin");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["plugins:externalAssemblies:0:name"] = "SamplePlugin",
            ["plugins:externalAssemblies:0:path"] = dllPath!
        });

        var loader = new PluginLoaderType(config, Logger);
        var services = new ServiceCollection();

        loader.DiscoverAndRegister(services);

        // 按全名匹配，规避跨 ALC 类型标识问题。
        Assert.Contains(services, d => d.ServiceType.FullName == "SamplePlugin.SampleMarkerService");
    }

    [Fact]
    public void LoadAndUnload_ReleasesAssemblyLoadContext()
    {
        var dllPath = FindSamplePluginDll();
        Assert.True(dllPath is not null, "未找到 SamplePlugin.dll，请先构建 samples/SamplePlugin");

        var loader = new PluginLoaderType(new ConfigurationBuilder().Build(), Logger);
        var manifest = new PluginManifest("SamplePlugin", dllPath!, string.Empty);

        // 仅加载不注册，避免 ServiceCollection 持有插件类型导致 ALC 无法回收。
        // 在独立方法中加载并丢弃返回的 Assembly 引用，确保不持有强引用。
        LoadAndDiscardAssembly(loader, manifest);

        var weakRef = loader.GetLoadContextWeakReference("SamplePlugin");
        Assert.NotNull(weakRef);

        loader.Unload("SamplePlugin");

        // 再做一轮 GC 确保 ALC 被回收。
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakRef!.IsAlive);
    }

    private static void LoadAndDiscardAssembly(PluginLoaderType loader, PluginManifest manifest)
    {
        loader.LoadAssembly(manifest);
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
