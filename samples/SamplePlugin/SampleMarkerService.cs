namespace SamplePlugin;

/// <summary>
/// 示例插件注册的标记服务，用于验证插件已被加载并向 DI 注册服务。
/// </summary>
public sealed class SampleMarkerService
{
    /// <summary>供调用方确认服务来自 SamplePlugin。</summary>
    public string Greeting => "hello from SamplePlugin";
}
