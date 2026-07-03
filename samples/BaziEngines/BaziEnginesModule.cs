using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Engines.Bazi;

/// <summary>
/// Bazi 域扩展引擎插件模块：向主程序 DI 容器注册 5 个 IChartEngine 实现
/// （cnlunar C# 移植 + openfate/Alvamind/lunar-python HTTP 桥接 + mymcp MCP 桥接）。
/// 加上内置 BaziChartEngine，Bazi 域共 6 个引擎。
/// <para>需提供无参公共构造函数，以便 PluginLoader 通过 <c>Activator.CreateInstance</c> 实例化模块。</para>
/// </summary>
public sealed class BaziEnginesModule : IPluginModule
{
    /// <summary>插件清单，供 PluginLoader 读取并校验 <c>RequiredApiVersion</c>。</summary>
    public IPluginManifest Manifest { get; } = new BaziEnginesPluginManifest();

    /// <summary>
    /// 向主程序 DI 容器注册本插件提供的全部排盘引擎。
    /// 多个 IChartEngine 实现以集合形式注册，供降级链按 EngineId 选取。
    /// </summary>
    public void Register(IServiceCollection services)
    {
        // C# 移植引擎（真实算法：建除十二神 + 宜忌等第）
        services.AddSingleton<IChartEngine, BaziCnlunarPortEngine>();

        // HTTP 桥接引擎（sidecar 不可达时由基类返回错误对象，不阻断其他引擎）
        services.AddSingleton<IChartEngine, BaziOpenfateBridgeEngine>();
        services.AddSingleton<IChartEngine, BaziAlvamindBridgeEngine>();
        services.AddSingleton<IChartEngine, BaziLunarPythonBridgeEngine>();

        // MCP 桥接引擎
        services.AddSingleton<IChartEngine, BaziMymcpBridgeEngine>();
    }
}
