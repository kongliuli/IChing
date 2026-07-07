using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// RoxyAPI 远程商业 tarot API 引擎：调用 https://api.roxyapi.com/tarot，
/// 提供 daily/three-card/yes-no/celtic-cross 等抽取接口。
/// <para>EngineId = "tarot-roxyapi-remote"，Domain = "tarot"。</para>
/// <para>远程 API 不可达时由基类返回错误对象（不抛异常）。</para>
/// </summary>
public sealed class TarotRoxyapiRemoteEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    protected override string SidecarUrl => "https://api.roxyapi.com/tarot";

    /// <inheritdoc />
    public override string Domain => "tarot";

    /// <inheritdoc />
    public override string EngineId => "tarot-roxyapi-remote";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "RoxyAPI tarot",
        Version: "v2",
        AlgorithmBasis: "商业 tarot API（daily/three-card/yes-no/celtic-cross）",
        TemplateHint: "roxyapi",
        ModuleFocus: ["draw"]);
}
