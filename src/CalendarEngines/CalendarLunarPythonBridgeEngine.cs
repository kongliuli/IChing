using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Calendar;

/// <summary>
/// lunar-python HTTP 桥接引擎：转发排盘请求到本地 sidecar（默认 http://localhost:5012/calendar），
/// 由 sidecar 调用 6tail <c>lunar-python</c> 1.3.x 计算节气/杂节。
/// sidecar 不可达时由 <see cref="ExternalHttpChartBridge"/> 返回错误对象，不抛异常。
/// </summary>
public sealed class CalendarLunarPythonBridgeEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    public override string EngineId => "calendar-lunar-python-bridge";

    /// <inheritdoc />
    public override string Domain => "calendar";

    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5012/calendar";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "lunar-python",
        Version: "1.3.x",
        AlgorithmBasis: "6tail lunar-python 节气/杂节",
        TemplateHint: "lunar-python",
        ModuleFocus: ["jieqi", "zatsusetsu"]);
}
