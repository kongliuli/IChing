namespace IChing.Lab.Abstractions.Plugins;

/// <summary>
/// 插件清单接口，提供插件元数据用于启动时的版本兼容性校验。
/// </summary>
public interface IPluginManifest
{
    /// <summary>插件名称。</summary>
    string Name { get; }

    /// <summary>插件版本号。</summary>
    string Version { get; }

    /// <summary>插件要求的抽象层 API 版本，需与 <see cref="AbstractionsVersion.Current"/> 兼容。</summary>
    string RequiredApiVersion { get; }
}
