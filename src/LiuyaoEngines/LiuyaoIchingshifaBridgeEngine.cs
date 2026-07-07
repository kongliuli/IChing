using IChing.Lab.Abstractions.Models;
using IChing.Lab.ChartBridge;

namespace IChing.Lab.Engines.Liuyao;

/// <summary>
/// HTTP 桥接引擎：包装 <c>ichingshifa</c>（kentang2017），按周易筮法 / 大衍之数起卦并取爻辞。
/// 通过本地 sidecar（默认 http://localhost:5005/liuyao）暴露筮法、大衍、爻辞查询。
/// <para>sidecar 不可达时由 <see cref="ExternalHttpChartBridge"/> 基类返回 <c>{ engine, error }</c> 对象，不抛异常。</para>
/// </summary>
public sealed class LiuyaoIchingshifaBridgeEngine : ExternalHttpChartBridge
{
    /// <summary>构造桥接。未提供 <paramref name="httpClient"/> 时使用默认 <see cref="HttpClient"/>。</summary>
    public LiuyaoIchingshifaBridgeEngine(HttpClient? httpClient = null) : base(httpClient) { }

    /// <inheritdoc />
    protected override string SidecarUrl => "http://localhost:5005/liuyao";

    /// <inheritdoc />
    public override string EngineId => "liuyao-ichingshifa-bridge";

    /// <inheritdoc />
    public override string Domain => "liuyao";

    /// <inheritdoc />
    public override EngineMetadata Metadata { get; } = new(
        Source: "ichingshifa",
        Version: "0.x",
        AlgorithmBasis: "周易筮法/大衍之数/爻辞（kentang2017）",
        TemplateHint: "ichingshifa",
        ModuleFocus: ["shifa", "dayan", "yaoci"]);
}
