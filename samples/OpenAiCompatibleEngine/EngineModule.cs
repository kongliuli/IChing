using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace OpenAiCompatibleEngine;

/// <summary>
/// OpenAI 兼容引擎插件模块：统一注册 4 个 <see cref="IInferenceEngine"/> 实现。
/// 由 PluginLoader 扫描程序集中实现 <see cref="IPluginModule"/> 的类型并实例化、调用 <see cref="Register"/>。
/// </summary>
public sealed class EngineModule : IPluginModule
{
    /// <summary>插件清单，供 PluginLoader 读取并校验 <c>RequiredApiVersion</c>。</summary>
    public IPluginManifest Manifest { get; } = new EngineModuleManifest();

    /// <summary>
    /// 向主程序 DI 容器注册本插件提供的全部解读引擎。
    /// 多个 IInferenceEngine 实现以集合形式注册，供降级链按 EngineId 选取。
    /// </summary>
    public void Register(IServiceCollection services)
    {
        // 模式 B：本地 HTTP 引擎
        services.AddSingleton<IInferenceEngine, OllamaLocalEngine>();
        services.AddSingleton<IInferenceEngine, LlamaServerLocalEngine>();

        // 模式 C：远程 API 引擎
        services.AddSingleton<IInferenceEngine, OpenAiRemoteEngine>();
        services.AddSingleton<IInferenceEngine, AzureOpenAiEngine>();
        services.AddSingleton<IInferenceEngine, DeepSeekEngine>();
    }
}

/// <summary>
/// OpenAI 兼容引擎插件清单，声明 <c>RequiredApiVersion = "0.1"</c>，与 <c>AbstractionsVersion.Current</c> 兼容。
/// </summary>
public sealed class EngineModuleManifest : IPluginManifest
{
    public string Name => "OpenAiCompatibleEngine";
    public string Version => "0.1";
    public string RequiredApiVersion => "0.1";
}
