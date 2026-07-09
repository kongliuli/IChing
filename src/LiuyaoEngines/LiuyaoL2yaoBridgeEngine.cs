using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Liuyao;

/// <summary>
/// HTTP 桥接引擎：包装 <c>l2yao/iching</c>，提供八字与六爻对照排盘。
/// 通过本地 sidecar（默认 http://localhost:5006/liuyao）暴露八字/六爻对照查询。
/// <para>sidecar 不可达时由 <see cref="ExternalHttpChartBridge"/> 基类返回 <c>{ engine, error }</c> 对象，不抛异常。</para>
/// </summary>
public sealed class LiuyaoL2yaoBridgeEngine : ExternalHttpChartBridge
{
    /// <summary>构造桥接。未提供 <paramref name="httpClient"/> 时使用默认 <see cref="HttpClient"/>。</summary>
    public LiuyaoL2yaoBridgeEngine(HttpClient? httpClient = null) : base(httpClient) { }

    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5006/liuyao";

    /// <inheritdoc />
    public override string EngineId => "liuyao-l2yao-bridge";

    /// <inheritdoc />
    public override string Domain => "liuyao";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "l2yao/iching",
        Version: "0.x",
        AlgorithmBasis: "八字+六爻对照",
        TemplateHint: "l2yao",
        ModuleFocus: ["duizhao"]);
}
