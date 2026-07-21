namespace IChing.Client.Shared.Editions;

/// <summary>
/// 三个发布版本互不递进，可独立发布或不发布。
/// </summary>
public enum EditionKind
{
    Free = 0,
    Byok = 1,
    Commercial = 2
}

/// <summary>
/// 版本能力开关：由 head 工程的组合根注入，驱动 Settings UI 与 Provider 选择。
/// </summary>
public sealed record EditionCapabilities(
    EditionKind Kind,
    bool AllowRemoteByok,
    bool AllowLabCommercial,
    bool AllowLocalOnnx,
    bool AllowFollowUp,
    bool ShowApiKeySettings,
    bool ShowLabUrlSettings,
    bool ShowMonetizationSlots)
{
    public static EditionCapabilities Free { get; } = new(
        EditionKind.Free,
        AllowRemoteByok: false,
        AllowLabCommercial: false,
        AllowLocalOnnx: false,
        AllowFollowUp: false,
        ShowApiKeySettings: false,
        ShowLabUrlSettings: false,
        ShowMonetizationSlots: false);

    public static EditionCapabilities Byok { get; } = new(
        EditionKind.Byok,
        AllowRemoteByok: true,
        AllowLabCommercial: false,
        AllowLocalOnnx: false,
        AllowFollowUp: true,
        ShowApiKeySettings: true,
        ShowLabUrlSettings: false,
        ShowMonetizationSlots: false);

    public static EditionCapabilities Commercial { get; } = new(
        EditionKind.Commercial,
        AllowRemoteByok: false,
        AllowLabCommercial: true,
        AllowLocalOnnx: false,
        AllowFollowUp: true,
        ShowApiKeySettings: false,
        ShowLabUrlSettings: false,
        ShowMonetizationSlots: true);

    /// <summary>现有开发壳：三种路径都可用，便于回归。</summary>
    public static EditionCapabilities DevShell { get; } = new(
        EditionKind.Byok,
        AllowRemoteByok: true,
        AllowLabCommercial: true,
        AllowLocalOnnx: true,
        AllowFollowUp: true,
        ShowApiKeySettings: true,
        ShowLabUrlSettings: true,
        ShowMonetizationSlots: false);

    /// <summary>是否提供 AI 解读（BYOK / Lab / 端侧 ONNX）。免费版为 false。</summary>
    public bool AllowAiInterpretation =>
        AllowRemoteByok || AllowLabCommercial || AllowLocalOnnx;
}
