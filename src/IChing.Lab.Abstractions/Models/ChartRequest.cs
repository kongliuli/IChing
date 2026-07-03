namespace IChing.Lab.Abstractions.Models;

/// <summary>
/// 排盘请求 DTO，由调用方向排盘引擎传递领域标识与参数字典。
/// </summary>
/// <param name="Domain">领域标识，例如 bazi / liuyao / tarot。</param>
/// <param name="Args">领域特定的输入参数字典，键为参数名，值为任意类型。</param>
public sealed record ChartRequest(string Domain, IDictionary<string, object?> Args);
