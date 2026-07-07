using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Bazi;

/// <summary>
/// 包装 <c>lunar-python</c>（6tail）的 HTTP 桥接引擎。
/// 通过本地 sidecar（默认 http://localhost:5003/bazi）转发排盘请求，
/// 探活失败或协议异常由基类返回错误对象，子类不抛异常。
/// </summary>
public sealed class BaziLunarPythonBridgeEngine : ExternalHttpChartBridge
{
    public BaziLunarPythonBridgeEngine(HttpClient? httpClient = null) : base(httpClient)
    {
    }

    protected override string SidecarUrl => "http://localhost:5003/bazi";

    public override string EngineId => "bazi-lunar-python-bridge";

    public override string Domain => "bazi";

    public override EngineMetadata Metadata { get; } = new(
        Source: "lunar-python",
        Version: "1.3.x",
        AlgorithmBasis: "6tail lunar-python 藏干/十神/纳音",
        TemplateHint: "lunar-python",
        ModuleFocus: ["canggan", "shishen", "nayin"]);
}
