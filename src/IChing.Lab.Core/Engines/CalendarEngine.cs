using System.Text.Json;
using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;
using IChing.Lab.Core.Calendar;

namespace IChing.Lab.Core.Engines;

/// <summary>
/// 黄历（日历）排盘引擎包装类，将静态 HuangLiService 包装为 IChartEngine 实现。
/// </summary>
public sealed class CalendarEngine : IChartEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Domain => "calendar";

    public string EngineId => "lunar-csharp-1.6.8";

    /// <summary>从参数字典读取年月日，委托给原静态方法 HuangLiService.GetDay。</summary>
    public object Calculate(ChartRequest request)
    {
        var input = DeserializeArgs<CalendarInput>(request.Args);
        return HuangLiService.GetDay(input.Year, input.Month, input.Day, input.Sect);
    }

    private static T DeserializeArgs<T>(IDictionary<string, object?> args)
    {
        var json = JsonSerializer.Serialize(args);
        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException("无法从参数字典反序列化输入");
    }

    private sealed record CalendarInput(int Year, int Month, int Day, int Sect = 1);
}
