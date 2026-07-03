using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Engines.Liuyao;

/// <summary>
/// LiuyaoEngines 插件模块：通过 <see cref="Manifest"/> 暴露清单，
/// 向主程序 DI 容器注册本插件提供的 5 个 <see cref="IChartEngine"/> 实现，
/// 与内置 <c>LiuyaoChartEngine</c>（iching-sixlines-2.0.3）合计 6 个 liuyao 域排盘引擎。
/// <para>需提供无参公共构造函数，以便 PluginLoader 通过 <c>Activator.CreateInstance</c> 实例化模块。</para>
/// </summary>
public sealed class LiuyaoEnginesModule : IPluginModule
{
    /// <summary>插件清单，供 PluginLoader 读取并校验 <c>RequiredApiVersion</c>。</summary>
    public IPluginManifest Manifest { get; } = new LiuyaoEnginesPluginManifest();

    /// <inheritdoc />
    public void Register(IServiceCollection services)
    {
        // 模式 B：HTTP 桥接引擎（sidecar 不可达时由基类返回错误对象，不抛异常）
        services.AddSingleton<IChartEngine, LiuyaoNpmBridgeEngine>();
        services.AddSingleton<IChartEngine, LiuyaoIchingshifaBridgeEngine>();
        services.AddSingleton<IChartEngine, LiuyaoL2yaoBridgeEngine>();
        services.AddSingleton<IChartEngine, LiuyaoZhouyilabBridgeEngine>();

        // 模式 A：C# 直接引用引擎（YiJingFramework.Annotating 5.0.1，真实《周易》爻辞数据）
        services.AddSingleton<IChartEngine, LiuyaoYijingframeworkAnnotEngine>();
    }
}
