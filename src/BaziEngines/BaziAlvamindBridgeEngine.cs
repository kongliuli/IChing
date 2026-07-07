using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Bazi;

/// <summary>
/// 包装 <c>bazi-calculator-by-alvamind</c> 的 HTTP 桥接引擎。
/// 通过本地 sidecar（默认 http://localhost:5002/bazi）转发排盘请求，
/// 探活失败或协议异常由基类返回错误对象，子类不抛异常。
/// </summary>
public sealed class BaziAlvamindBridgeEngine : ExternalHttpChartBridge
{
    public BaziAlvamindBridgeEngine(HttpClient? httpClient = null) : base(httpClient)
    {
    }

    protected override string SidecarUrl => "http://localhost:5002/bazi";

    public override string EngineId => "bazi-alvamind-bridge";

    public override string Domain => "bazi";

    public override EngineMetadata Metadata { get; } = new(
        Source: "bazi-calculator-by-alvamind",
        Version: "1.0.2",
        AlgorithmBasis: "四柱+八宅+贵人+文昌",
        TemplateHint: "alvamind",
        ModuleFocus: ["bazhai", "guiren"]);
}
