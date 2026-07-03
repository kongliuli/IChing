using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// Tarot 域扩展插件模块：通过 <see cref="Manifest"/> 暴露清单，
/// 向主程序 DI 容器注册 5 个新 tarot 排盘引擎作为 <see cref="IChartEngine"/> 实现。
/// <para>需提供无参公共构造函数，以便 PluginLoader 通过 <c>Activator.CreateInstance</c> 实例化模块。</para>
/// </summary>
public sealed class TarotEnginesModule : IPluginModule
{
    /// <summary>插件清单，供 PluginLoader 读取并校验 <c>RequiredApiVersion</c>。</summary>
    public IPluginManifest Manifest { get; } = new TarotEnginesPluginManifest();

    /// <inheritdoc />
    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IChartEngine, TarotDeckauraDataEngine>();
        services.AddSingleton<IChartEngine, TarotArcaniteBridgeEngine>();
        services.AddSingleton<IChartEngine, TarotTtarotBridgeEngine>();
        services.AddSingleton<IChartEngine, TarotRoxyapiRemoteEngine>();
        services.AddSingleton<IChartEngine, TarotMoraxMcpBridgeEngine>();
    }
}
