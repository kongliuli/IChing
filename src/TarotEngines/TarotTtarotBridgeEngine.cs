using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// ttarot HTTP 桥接引擎：调用本地 ttarot sidecar（0.3.0），取其 78 牌 upright/reversed 数据。
/// <para>EngineId = "tarot-ttarot-bridge"，Domain = "tarot"。</para>
/// <para>SidecarUrl = "http://localhost:5009/tarot"。sidecar 不可达时由基类返回错误对象。</para>
/// </summary>
public sealed class TarotTtarotBridgeEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5009/tarot";

    /// <inheritdoc />
    public override string Domain => "tarot";

    /// <inheritdoc />
    public override string EngineId => "tarot-ttarot-bridge";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "ttarot",
        Version: "0.3.0",
        AlgorithmBasis: "78 牌 upright/reversed 数据（gfargo/ttarot）",
        TemplateHint: "ttarot",
        ModuleFocus: ["upright", "reversed"]);
}
