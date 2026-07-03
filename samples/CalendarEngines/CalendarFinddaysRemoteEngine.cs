using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Calendar;

/// <summary>
/// find-days 节气表远程 API 引擎：通过 HTTP 桥接调用
/// <c>https://find-days.com/solar-terms/</c> 获取 2026 节气日期表。
/// 远程端点不提供 <c>/health</c> 探活路径，<see cref="ExternalHttpChartBridge"/> 探活失败时返回错误对象，不抛异常。
/// </summary>
public sealed class CalendarFinddaysRemoteEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    public override string EngineId => "calendar-finddays-remote";

    /// <inheritdoc />
    public override string Domain => "calendar";

    /// <inheritdoc />
    protected override string SidecarUrl => "https://find-days.com/solar-terms/";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "find-days solar-terms",
        Version: "2026",
        AlgorithmBasis: "2026 节气日期表",
        TemplateHint: "finddays",
        ModuleFocus: ["jieqi"]);
}
