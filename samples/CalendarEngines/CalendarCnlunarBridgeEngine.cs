using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Calendar;

/// <summary>
/// cnlunar HTTP 桥接引擎：转发排盘请求到本地 sidecar（默认 http://localhost:5010/calendar），
/// 由 sidecar 调用 Python <c>cnlunar</c> 0.2.4 计算宜忌等第。
/// sidecar 不可达时由 <see cref="ExternalHttpChartBridge"/> 返回错误对象，不抛异常。
/// </summary>
public sealed class CalendarCnlunarBridgeEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    public override string EngineId => "calendar-cnlunar-bridge";

    /// <inheritdoc />
    public override string Domain => "calendar";

    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5010/calendar";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "cnlunar",
        Version: "0.2.4",
        AlgorithmBasis: "《钦定协纪辨方书》宜忌等第",
        TemplateHint: "cnlunar",
        ModuleFocus: ["yiji", "dengdi"]);
}
