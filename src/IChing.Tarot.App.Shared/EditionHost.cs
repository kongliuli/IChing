using IChing.Client.Shared.Editions;
using IChing.Client.Shared.Monetization;

namespace IChing.Tarot.App;

/// <summary>
/// 版本 head 在 MauiProgram 里注入；Shared UI 据此显隐设置项与 Provider。
/// </summary>
public static class EditionHost
{
    public static EditionCapabilities Capabilities { get; set; } = EditionCapabilities.DevShell;

    /// <summary>商业版正式 Lab 地址；未设置时回退 localhost（仅 DevShell）。</summary>
    public static string DefaultLabApiUrl { get; set; } = "http://localhost:5000";

    /// <summary>可空；非空时覆盖牌面图 CDN 默认。</summary>
    public static string? DefaultCardCdnBase { get; set; }

    /// <summary>商店升级链接占位（免费版设置页）。</summary>
    public static string? UpgradeStoreUrl { get; set; }

    public static IReadOnlyList<IMonetizationSlot> MonetizationSlots { get; set; } =
        [new NoOpMonetizationSlot("banner.after-reading")];

    public static string DisplayName => Capabilities.Kind switch
    {
        EditionKind.Free => "星轨塔罗·免费版",
        EditionKind.Byok => "星轨塔罗·自助版",
        EditionKind.Commercial => "星轨塔罗·商业版",
        _ => "星轨塔罗"
    };
}
