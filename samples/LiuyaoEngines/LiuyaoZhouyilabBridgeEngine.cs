using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Liuyao;

/// <summary>
/// HTTP 桥接引擎：包装 <c>ZhouYiLab</c>（C++23 实现，五术齐全：大六壬/六爻/紫微/八字/奇门）。
/// 通过本地 sidecar（默认 http://localhost:5007/liuyao）暴露其模块化排盘能力。
/// <para>sidecar 不可达时由 <see cref="ExternalHttpChartBridge"/> 基类返回 <c>{ engine, error }</c> 对象，不抛异常。</para>
/// </summary>
public sealed class LiuyaoZhouyilabBridgeEngine : ExternalHttpChartBridge
{
    /// <summary>构造桥接。未提供 <paramref name="httpClient"/> 时使用默认 <see cref="HttpClient"/>。</summary>
    public LiuyaoZhouyilabBridgeEngine(HttpClient? httpClient = null) : base(httpClient) { }

    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5007/liuyao";

    /// <inheritdoc />
    public override string EngineId => "liuyao-zhouyilab-bridge";

    /// <inheritdoc />
    public override string Domain => "liuyao";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "ZhouYiLab",
        Version: "C++23",
        AlgorithmBasis: "五术齐全（大六壬/六爻/紫微/八字/奇门）模块化",
        TemplateHint: "zhouyilab",
        ModuleFocus: ["wushu"]);
}
