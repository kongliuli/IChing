using IChing.Lab.Core.Liuyao;

namespace IChing.Liuyao.App.Services;

/// <summary>本地六爻起卦，复用 Core LiuyaoNajiaService。</summary>
public sealed class LiuyaoCastService
{
    public LiuyaoNajiaResult Cast(string method, DateTimeOffset at, int? seed) =>
        string.Equals(method, "time", StringComparison.OrdinalIgnoreCase)
            ? LiuyaoNajiaService.Time(at)
            : LiuyaoNajiaService.Coin(at, seed);
}
