using IChing.Lab.Abstractions.Plugins;

namespace SamplePlugin;

/// <summary>
/// 示例插件清单，声明 <c>RequiredApiVersion = "0.1"</c>，与 <c>AbstractionsVersion.Current</c> 兼容。
/// </summary>
public sealed class SamplePluginManifest : IPluginManifest
{
    public string Name => "SamplePlugin";
    public string Version => "0.1";
    public string RequiredApiVersion => "0.1";
}
