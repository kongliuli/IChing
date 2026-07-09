using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Calendar;

/// <summary>
/// lunar-calendar HTTP 桥接引擎：转发排盘请求到本地 sidecar（默认 http://localhost:5011/calendar），
/// 由 sidecar 调用 <c>lunar-calendar</c>（VSOP87/LEA-406 天文算法，1901-2100 香港天文台数据）。
/// sidecar 不可达时由 <see cref="ExternalHttpChartBridge"/> 返回错误对象，不抛异常。
/// </summary>
public sealed class CalendarLunarCalendarBridgeEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    public override string EngineId => "calendar-lunar-calendar-bridge";

    /// <inheritdoc />
    public override string Domain => "calendar";

    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5011/calendar";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "lunar-calendar",
        Version: "0.x",
        AlgorithmBasis: "VSOP87/LEA-406 天文算法，1901-2100 香港天文台数据",
        TemplateHint: "lunar-calendar",
        ModuleFocus: ["jieqi", "tianwen"]);
}
