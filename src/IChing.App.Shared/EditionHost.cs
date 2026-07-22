using IChing.Client.Shared.Editions;

namespace IChing.App;

/// <summary>
/// 版本 head 在 MauiProgram 注入；Shared UI 据此裁剪设置与 Provider。
/// </summary>
public static class EditionHost
{
    public static EditionCapabilities Capabilities { get; set; } = EditionCapabilities.DevShell;

    /// <summary>商业版正式 Lab 地址；未设置时回退 localhost（仅 DevShell）。</summary>
    public static string DefaultLabApiUrl { get; set; } = "http://localhost:5000";

    public static string? UpgradeStoreUrl { get; set; }

    public static string DisplayName => Capabilities.Kind switch
    {
        EditionKind.Free => "易占·免费版",
        EditionKind.Byok => "易占·自助版",
        EditionKind.Commercial => "易占·商业版",
        _ => "易占"
    };
}
