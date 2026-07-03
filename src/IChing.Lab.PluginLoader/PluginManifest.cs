namespace IChing.Lab.PluginLoader;

/// <summary>
/// 插件清单记录，描述一个外部插件程序集的元数据。
/// </summary>
/// <param name="Name">插件名称，用作唯一标识与卸载键。</param>
/// <param name="Path">插件主程序集的（已解析的）绝对路径。</param>
/// <param name="RequiredApiVersion">插件要求的抽象层 API 版本；发现阶段未知时为空字符串。</param>
public sealed record PluginManifest(string Name, string Path, string RequiredApiVersion);
