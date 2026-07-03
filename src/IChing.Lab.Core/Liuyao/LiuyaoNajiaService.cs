using IChingLibrary.Core;
using IChingLibrary.SixLines;
using IChingLibrary.SixLines.Builder;

namespace IChing.Lab.Core.Liuyao;

public static class LiuyaoNajiaService
{
    public static LiuyaoNajiaResult Coin(DateTimeOffset at, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var symbols = new FourSymbol[6];
        for (var i = 0; i < 6; i++)
        {
            var sum = 0;
            for (var c = 0; c < 3; c++)
            {
                sum += rng.Next(2) == 0 ? 2 : 3;
            }
            symbols[i] = sum switch
            {
                6 => FourSymbol.OldYin,
                7 => FourSymbol.YoungYang,
                8 => FourSymbol.YoungYin,
                9 => FourSymbol.OldYang,
                _ => throw new InvalidOperationException("invalid coin sum")
            };
        }

        var divination = SixLineDivination
            .CreateBuilder()
            .UseMethod(new CoinCastingMethod(at, symbols))
            .WithDefaultSteps()
            .Build();

        return Map(divination, "coin", seed);
    }

    public static LiuyaoNajiaResult Time(DateTimeOffset at) =>
        Map(SixLineDivination.Create(at), "time", null);

    private static LiuyaoNajiaResult Map(SixLineDivination d, string method, int? seed)
    {
        var lines = d.Original.Lines
            .Select((line, idx) => MapLine(line, idx, d))
            .ToList();

        IReadOnlyList<LiuyaoLineDetail>? changedLines = d.Changed?.Lines
            .Select((line, idx) => MapLine(line, idx, null))
            .ToList();

        return new LiuyaoNajiaResult(
            Engine: "IChingLibrary.SixLines",
            Method: method,
            Seed: seed,
            CastingTime: d.CastingTime.Solar.ToString("O"),
            OriginalHexagram: d.Original.Meta.Label,
            ChangedHexagram: d.Changed?.Meta.Label,
            Lines: lines,
            ChangedLines: changedLines
        );
    }

    private static LiuyaoLineDetail MapLine(Line line, int idx, SixLineDivination? source)
    {
        var changing = source?.Changed is not null &&
                       idx < source.Changed!.Lines.Count &&
                       source.Original.Lines[idx].YinYang != source.Changed!.Lines[idx].YinYang;

        return new LiuyaoLineDetail(
            Index: idx + 1,
            Position: line.LinePosition.Label,
            YinYang: line.YinYang.Label,
            StemBranch: line.TryGetStemBranch(out var sb) ? sb.ToString() : null,
            SixKin: line.TryGetSixKin(out var kin) ? kin.Label : null,
            SixSpirit: line.SixSpirit?.Label,
            Role: line.Position?.Label,
            IsChanging: changing);
    }
}

public record LiuyaoLineDetail(
    int Index,
    string Position,
    string YinYang,
    string? StemBranch,
    string? SixKin,
    string? SixSpirit,
    string? Role,
    bool IsChanging);

public record LiuyaoNajiaResult(
    string Engine,
    string Method,
    int? Seed,
    string CastingTime,
    string OriginalHexagram,
    string? ChangedHexagram,
    IReadOnlyList<LiuyaoLineDetail> Lines,
    IReadOnlyList<LiuyaoLineDetail>? ChangedLines);
