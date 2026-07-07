using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Engines.Calendar;

/// <summary>
/// Calendar 域插件模块：通过 <see cref="Manifest"/> 暴露清单，
/// 向主程序 DI 容器注册 5 个 calendar 排盘桥接/远程引擎作为 <see cref="IChartEngine"/> 实现。
/// <para>需提供无参公共构造函数，以便 PluginLoader 通过 <c>Activator.CreateInstance</c> 实例化模块。</para>
/// </summary>
public sealed class CalendarEnginesModule : IPluginModule
{
    /// <summary>插件清单，供 PluginLoader 读取并校验 <c>RequiredApiVersion</c>。</summary>
    public IPluginManifest Manifest { get; } = new CalendarEnginesPluginManifest();

    /// <summary>
    /// 向主程序 DI 容器注册本插件提供的全部 calendar 排盘引擎。
    /// 多个 IChartEngine 实现以集合形式注册，供按 EngineId 选取；sidecar 离线时各引擎自行返回错误对象，不阻断其他引擎。
    /// </summary>
    public void Register(IServiceCollection services)
    {
        // HTTP 桥接：本地 sidecar
        services.AddSingleton<IChartEngine, CalendarCnlunarBridgeEngine>();
        services.AddSingleton<IChartEngine, CalendarLunarCalendarBridgeEngine>();
        services.AddSingleton<IChartEngine, CalendarLunarPythonBridgeEngine>();

        // 远程 API（HTTP 桥接）
        services.AddSingleton<IChartEngine, CalendarKoyomiRemoteEngine>();
        services.AddSingleton<IChartEngine, CalendarFinddaysRemoteEngine>();
    }
}
