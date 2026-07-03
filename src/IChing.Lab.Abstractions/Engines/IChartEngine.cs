using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.Abstractions.Engines;

/// <summary>
/// 排盘算法抽象接口，由各领域（八字 / 六爻 / 塔罗等）排盘引擎实现。
/// </summary>
public interface IChartEngine
{
    /// <summary>领域标识，例如 bazi / liuyao / tarot。</summary>
    string Domain { get; }

    /// <summary>引擎标识，在同一领域内唯一区分不同实现。</summary>
    string EngineId { get; }

    /// <summary>
    /// 根据请求执行排盘计算并返回排盘结果对象。
    /// </summary>
    /// <param name="request">排盘请求，包含领域标识与参数字典。</param>
    /// <returns>排盘结果对象，具体类型由实现方决定。</returns>
    object Calculate(ChartRequest request);
}
