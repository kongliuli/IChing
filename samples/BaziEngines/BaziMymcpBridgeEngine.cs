using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Bazi;

/// <summary>
/// 包装 <c>@mymcp-fun/bazi</c> 的 MCP 桥接引擎。
/// 通过 stdio 启动 MCP server 子进程，按 JSON-RPC 调用其 <c>get_bazi_details</c> 工具获取排盘结果。
/// 进程启动失败或协议异常由基类返回错误对象，子类不抛异常。
/// </summary>
public sealed class BaziMymcpBridgeEngine : McpChartBridge
{
    protected override string McpServerCommand => "npx";

    protected override string[] McpServerArgs => ["-y", "@mymcp-fun/bazi"];

    protected override string McpToolName => "get_bazi_details";

    public override string EngineId => "bazi-mymcp-bridge";

    public override string Domain => "bazi";

    public override EngineMetadata Metadata { get; } = new(
        Source: "@mymcp-fun/bazi",
        Version: "2.0.2",
        AlgorithmBasis: "MCP 协议八字服务（四柱/五行/生肖/农历）",
        TemplateHint: "mymcp",
        ModuleFocus: ["sizhu", "wuxing"]);
}
