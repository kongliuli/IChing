using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// Morax Tarot MCP Server 桥接引擎：通过 stdio 启动 npx tarot-mcp-server@latest 子进程，
/// 调用其 tarot_reading 工具获取 11 牌阵 + 元素平衡排盘结果。
/// <para>EngineId = "tarot-morax-mcp-bridge"，Domain = "tarot"。</para>
/// <para>MCP server 不可达时由基类返回错误对象（不抛异常）。</para>
/// </summary>
public sealed class TarotMoraxMcpBridgeEngine : McpChartBridge
{
    /// <inheritdoc />
    protected override string McpServerCommand => "npx";

    /// <inheritdoc />
    protected override string[] McpServerArgs => ["tarot-mcp-server@latest"];

    /// <inheritdoc />
    protected override string McpToolName => "tarot_reading";

    /// <inheritdoc />
    public override string Domain => "tarot";

    /// <inheritdoc />
    public override string EngineId => "tarot-morax-mcp-bridge";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "Tarot MCP Server(Morax)",
        Version: "1.x",
        AlgorithmBasis: "11 牌阵+元素平衡（lobehub/morax-tarot-mcp）",
        TemplateHint: "morax",
        ModuleFocus: ["paizhen", "yuansu"]);
}
