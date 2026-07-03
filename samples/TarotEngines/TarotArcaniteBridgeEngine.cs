using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// arcanite HTTP 桥接引擎：调用本地 arcanite sidecar（0.2.0），仅取其 11 牌阵与牌义映射，
/// 不使用其 LLM/RAG 层。
/// <para>EngineId = "tarot-arcanite-bridge"，Domain = "tarot"。</para>
/// <para>SidecarUrl = "http://localhost:5008/tarot"。sidecar 不可达时由基类返回错误对象。</para>
/// </summary>
public sealed class TarotArcaniteBridgeEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5008/tarot";

    /// <inheritdoc />
    public override string Domain => "tarot";

    /// <inheritdoc />
    public override string EngineId => "tarot-arcanite-bridge";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "arcanite",
        Version: "0.2.0",
        AlgorithmBasis: "11 牌阵+RAG 映射（仅取牌阵/牌义，不用其 LLM）",
        TemplateHint: "arcanite",
        ModuleFocus: ["paizhen", "paiyi"]);
}
