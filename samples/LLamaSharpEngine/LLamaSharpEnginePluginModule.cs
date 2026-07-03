using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace IChing.Lab.Engines.LLamaSharp;

/// <summary>
/// LLamaSharpEngine 插件模块：通过 <see cref="Manifest"/> 暴露清单，
/// 向主程序 DI 容器注册 <see cref="LLamaSharpEngine"/> 作为 <see cref="IInferenceEngine"/> 实现。
/// <para>需提供无参公共构造函数，以便 PluginLoader 通过 <c>Activator.CreateInstance</c> 实例化模块。</para>
/// </summary>
public sealed class LLamaSharpEnginePluginModule : IPluginModule
{
    /// <summary>插件清单，供 PluginLoader 读取并校验 <c>RequiredApiVersion</c>。</summary>
    public IPluginManifest Manifest { get; } = new LLamaSharpEnginePluginManifest();

    /// <inheritdoc />
    public void Register(IServiceCollection services)
    {
        // 引擎通过 DI 注入 IConfiguration / ILogger<LLamaSharpEngine>，因此注册为类型而非实例。
        services.AddSingleton<IInferenceEngine, LLamaSharpEngine>();
    }
}
