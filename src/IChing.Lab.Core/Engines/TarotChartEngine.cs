using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Tarot;

namespace IChing.Lab.Core.Engines;

/// <summary>
/// 塔罗排盘引擎包装类，将静态 TarotEngine 包装为 IChartEngine 实现。
/// </summary>
public sealed class TarotChartEngine : IChartEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => "tarot";

    public string EngineId => "iching-tarot-built-in";

    /// <summary>从参数字典读取牌阵/问题/种子，委托给原静态方法 TarotEngine.Draw。</summary>
    public object Calculate(ChartRequest request)
    {
        var input = DeserializeArgs<TarotInput>(request.Args);
        return TarotEngine.Draw(input.SpreadId ?? "past-present-future", input.Question, input.Seed);
    }

    private static T DeserializeArgs<T>(IDictionary<string, object?> args)
    {
        var json = JsonSerializer.Serialize(args);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法从参数字典反序列化输入");
    }

    private sealed record TarotInput(string? SpreadId, string? Question, int? Seed);
}
