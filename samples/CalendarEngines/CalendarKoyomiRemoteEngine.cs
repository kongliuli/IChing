using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Calendar;

/// <summary>
/// 国立天文台 koyomi 远程 API 引擎：通过 HTTP 桥接调用
/// <c>https://eco2.mtk.nao.ac.jp/cgi-bin/koyomi/cande/phenomena_sy.cgi</c> 获取二十四节气・雑節长期版。
/// 远程端点不提供 <c>/health</c> 探活路径，<see cref="ExternalHttpChartBridge"/> 探活失败时返回错误对象，不抛异常。
/// </summary>
public sealed class CalendarKoyomiRemoteEngine : ExternalHttpChartBridge
{
    /// <inheritdoc />
    public override string EngineId => "calendar-koyomi-remote";

    /// <inheritdoc />
    public override string Domain => "calendar";

    /// <inheritdoc />
    protected override string SidecarUrl => "https://eco2.mtk.nao.ac.jp/cgi-bin/koyomi/cande/phenomena_sy.cgi";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "国立天文台 koyomi",
        Version: "2026",
        AlgorithmBasis: "二十四节气・雑節 長期版（黄経ベース）",
        TemplateHint: "koyomi",
        ModuleFocus: ["jieqi", "zatsusetsu"]);
}
