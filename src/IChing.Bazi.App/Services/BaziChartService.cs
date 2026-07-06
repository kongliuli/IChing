using IChing.Lab.Core.Bazi;

namespace IChing.Bazi.App.Services;

/// <summary>本地八字排盘，复用 Core BaziEngine。</summary>
public sealed class BaziChartService
{
    public BaziChart Calculate(BaziInput input) => BaziEngine.Calculate(input);
}
