using IChing.Lab.Abstractions.Plugins;

namespace IChing.Lab.Engines.Bazi;

/// <summary>
/// BaziEngines 插件清单，声明 <c>RequiredApiVersion = "0.1"</c>，
/// 与 <see cref="AbstractionsVersion.Current"/> 兼容，供 PluginLoader 启动时校验。
/// </summary>
public sealed class BaziEnginesPluginManifest : IPluginManifest
{
    /// <inheritdoc />
    public string Name => "BaziEngines";

    /// <inheritdoc />
    public string Version => "0.1.0";

    /// <inheritdoc />
    public string RequiredApiVersion => "0.1";
}
