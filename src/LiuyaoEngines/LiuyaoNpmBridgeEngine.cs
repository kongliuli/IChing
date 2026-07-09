using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Liuyao;

/// <summary>
/// HTTP 桥接引擎：包装 npm 包 <c>liuyao</c> 0.3.2（baendlorel/kt-packages），
/// 通过本地 sidecar（默认 http://localhost:5004/liuyao）暴露六爻元数据 + 六神表 + 变卦计算。
/// <para>sidecar 不可达时由 <see cref="ExternalHttpChartBridge"/> 基类返回 <c>{ engine, error }</c> 对象，不抛异常。</para>
/// </summary>
public sealed class LiuyaoNpmBridgeEngine : ExternalHttpChartBridge
{
    /// <summary>构造桥接。未提供 <paramref name="httpClient"/> 时使用默认 <see cref="HttpClient"/>。</summary>
    public LiuyaoNpmBridgeEngine(HttpClient? httpClient = null) : base(httpClient) { }

    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5004/liuyao";

    /// <inheritdoc />
    public override string EngineId => "liuyao-npm-bridge";

    /// <inheritdoc />
    public override string Domain => "liuyao";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "liuyao(npm)",
        Version: "0.3.2",
        AlgorithmBasis: "六爻元数据+六神表+变卦（baendlorel/kt-packages）",
        TemplateHint: "liuyao-npm",
        ModuleFocus: ["yao", "liushen", "biangua"]);
}
