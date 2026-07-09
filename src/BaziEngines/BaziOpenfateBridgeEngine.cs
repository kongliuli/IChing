using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Bazi;

/// <summary>
/// 包装 <c>@openfate/bazi-engine</c> 的 HTTP 桥接引擎。
/// 通过本地 sidecar（默认 http://localhost:5001/bazi）转发排盘请求，
/// 探活失败或协议异常由基类返回错误对象，子类不抛异常。
/// </summary>
public sealed class BaziOpenfateBridgeEngine : ExternalHttpChartBridge
{
    public BaziOpenfateBridgeEngine(HttpClient? httpClient = null) : base(httpClient)
    {
    }

    protected override string SidecarUrl => "http://localhost:5001/bazi";

    public override string EngineId => "bazi-openfate-bridge";

    public override string Domain => "bazi";

    public override EngineMetadata Metadata { get; } = new(
        Source: "@openfate/bazi-engine",
        Version: "1.1.1",
        AlgorithmBasis: "真太阳时+节气+大运+交互检测",
        TemplateHint: "openfate",
        ModuleFocus: ["zhentaiyangshi", "dayun", "jiaohu"]);
}
