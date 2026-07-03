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

        ChangedHexagramDetail? changedDetail = null;
        if (d.Changed is not null)
        {
            // ponytail: 将变卦视作主卦重建，补齐伏神/六神/神煞（库默认不对变卦算这些）
            var changedFull = SixLineDivination.Create(d.CastingTime.Solar, d.Changed.Meta);
            var changedLines = changedFull.Original.Lines
                .Select((line, idx) => MapLine(line, idx, changedFull))
                .ToList();

            changedDetail = new ChangedHexagramDetail(
                Hexagram: d.Changed.Meta.Label,
                Nature: d.Changed.Meta.GetNature()?.Label,
                HexagramBody: changedFull.Original.FindHexagramBody()?.Label,
                SymbolicStars: MapSymbolicStars(changedFull),
                Lines: changedLines,
                Comparison: LiuyaoComparisonBuilder.Build(lines, changedLines));
        }

        ChangedComparison? comparison = changedDetail?.Comparison;

        return new LiuyaoNajiaResult(
            Engine: "IChingLibrary.SixLines",
            Method: method,
            Seed: seed,
            CastingTime: d.CastingTime.Solar.ToString("O"),
            OriginalHexagram: d.Original.Meta.Label,
            ChangedHexagram: d.Changed?.Meta.Label,
            OriginalNature: d.Original.Meta.GetNature()?.Label,
            ChangedNature: d.Changed?.Meta.GetNature()?.Label,
            HexagramBody: d.Original.FindHexagramBody()?.Label,
            SymbolicStars: MapSymbolicStars(d),
            Lines: lines,
            Changed: changedDetail,
            Comparison: comparison
        );
    }

    private static IReadOnlyList<SymbolicStarEntry> MapSymbolicStars(SixLineDivination d)
    {
        if (d.SymbolicStars is null)
        {
            return [];
        }

        var entries = new List<SymbolicStarEntry>();
        foreach (var star in KnownStars)
        {
            var branches = d.SymbolicStars.GetStars(star);
            if (branches is null || branches.Length == 0)
            {
                continue;
            }

            entries.Add(new SymbolicStarEntry(
                star.Label,
                branches.Select(b => b.Label).ToList()));
        }

        return entries;
    }

    private static readonly SymbolicStar[] KnownStars =
    [
        SymbolicStar.Nobleman,
        SymbolicStar.SalarySpirit,
        SymbolicStar.CultureFlourish,
        SymbolicStar.PostHorse,
        SymbolicStar.PeachBlossom,
        SymbolicStar.YangBlade,
        SymbolicStar.GeneralsStar,
        SymbolicStar.Canopy,
        SymbolicStar.StarOfStrategy,
        SymbolicStar.DisasterMalignity,
        SymbolicStar.RobberyMalignity,
        SymbolicStar.DeathSpirit,
        SymbolicStar.CelestialPhysician,
        SymbolicStar.HeavenlyJoy,
        SymbolicStar.MarriageBed,
        SymbolicStar.BridalChamber
    ];

    private static LiuyaoLineDetail MapLine(Line line, int idx, SixLineDivination source)
    {
        var changing = source.Changed is not null &&
                       idx < source.Changed.Lines.Count &&
                       source.Original.Lines[idx].YinYang != source.Changed.Lines[idx].YinYang;

        HiddenDeityDetail? hidden = null;
        if (line.HasHiddenDeity)
        {
            var hiddenInfo = HiddenDeityInfo.FromLine(line);
            hidden = new HiddenDeityDetail(
                hiddenInfo.StemBranch.ToString(),
                hiddenInfo.SixKin.Label);
        }

        IReadOnlyList<string>? lineStars = null;
        if (source.SymbolicStars is { } symbolicStars && line.TryGetStemBranch(out var lineStemBranch))
        {
            var stars = symbolicStars.GetStarsForBranch(lineStemBranch!.Branch).ToList();
            if (stars.Count > 0)
            {
                lineStars = stars.Select(s => s.Label).ToList();
            }
        }

        return new LiuyaoLineDetail(
            Index: idx + 1,
            Position: line.LinePosition.Label,
            YinYang: line.YinYang.Label,
            StemBranch: line.TryGetStemBranch(out var stemBranch) ? stemBranch?.ToString() : null,
            SixKin: line.TryGetSixKin(out var kin) ? kin?.Label : null,
            SixSpirit: line.SixSpirit?.Label,
            Role: line.Position?.Label,
            IsChanging: changing,
            HiddenDeity: hidden,
            SymbolicStars: lineStars);
    }
}

public record HiddenDeityDetail(string StemBranch, string SixKin);

public record SymbolicStarEntry(string Name, IReadOnlyList<string> Branches);

public record LiuyaoLineDetail(
    int Index,
    string Position,
    string YinYang,
    string? StemBranch,
    string? SixKin,
    string? SixSpirit,
    string? Role,
    bool IsChanging,
    HiddenDeityDetail? HiddenDeity,
    IReadOnlyList<string>? SymbolicStars);

public record ChangedHexagramDetail(
    string Hexagram,
    string? Nature,
    string? HexagramBody,
    IReadOnlyList<SymbolicStarEntry> SymbolicStars,
    IReadOnlyList<LiuyaoLineDetail> Lines,
    ChangedComparison Comparison);

public record LiuyaoNajiaResult(
    string Engine,
    string Method,
    int? Seed,
    string CastingTime,
    string OriginalHexagram,
    string? ChangedHexagram,
    string? OriginalNature,
    string? ChangedNature,
    string? HexagramBody,
    IReadOnlyList<SymbolicStarEntry> SymbolicStars,
    IReadOnlyList<LiuyaoLineDetail> Lines,
    ChangedHexagramDetail? Changed,
    ChangedComparison? Comparison);
