using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace SamplePlugin;

/// <summary>
/// 示例插件模块：通过 <see cref="Manifest"/> 暴露清单，向 DI 注册 <see cref="SampleMarkerService"/>。
/// </summary>
public sealed class SamplePluginModule : IPluginModule
{
    /// <summary>插件清单，供 PluginLoader 读取并校验 <c>RequiredApiVersion</c>。</summary>
    public IPluginManifest Manifest { get; } = new SamplePluginManifest();

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<SampleMarkerService>();
    }
}
