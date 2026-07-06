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
                Hexagram: HexagramNames.Display(d.Changed.Meta.Label),
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
            OriginalHexagram: HexagramNames.Display(d.Original.Meta.Label),
            ChangedHexagram: d.Changed is null ? null : HexagramNames.Display(d.Changed.Meta.Label),
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
                SixKinZh(hiddenInfo.SixKin.Label) ?? "");
        }

        IReadOnlyList<string>? lineStars = null;
        if (source.SymbolicStars is { } symbolicStars && line.TryGetStemBranch(out var lineStemBranch))
        {
            var stars = symbolicStars.GetStarsForBranch(lineStemBranch!.Branch).ToList();
            if (stars.Count > 0)
            {
                lineStars = stars.Select(s => StarZh(s.Label)).ToList();
            }
        }

        var yinYang = YinYangZh(line.YinYang.Label);
        var sixKin = line.TryGetSixKin(out var kin) ? SixKinZh(kin?.Label) : null;

        return new LiuyaoLineDetail(
            Index: idx + 1,
            Position: PositionZh(line.LinePosition.Label),
            YinYang: yinYang,
            YinYangDescription: DescribeYinYang(yinYang, changing),
            StemBranch: line.TryGetStemBranch(out var stemBranch) ? stemBranch?.ToString() : null,
            SixKin: sixKin,
            SixKinDescription: DescribeSixKin(sixKin),
            SixSpirit: SixSpiritZh(line.SixSpirit?.Label),
            Role: RoleZh(line.Position?.Label),
            IsChanging: changing,
            HiddenDeity: hidden,
            SymbolicStars: lineStars);
    }

    private static string DescribeYinYang(string label, bool changing)
    {
        var baseText = label.Contains('阳')
            ? "阳爻，主外显、主动、推进、刚健。"
            : "阴爻，主内敛、承载、蓄势、柔顺。";
        return changing ? $"{baseText}此爻发动，需重点看它对本卦、变卦和用神的牵动。" : baseText;
    }

    private static string YinYangZh(string? label) => label switch
    {
        "Yang" => "阳爻",
        "Yin" => "阴爻",
        _ => label ?? ""
    };

    private static string? DescribeSixKin(string? label) => label switch
    {
        "父母" => "父母爻：文书、证件、消息、长辈、房屋、学习与庇护。",
        "兄弟" => "兄弟爻：同辈、竞争、消耗、分财、朋友与阻力。",
        "子孙" => "子孙爻：成果、下属、孩子、放松、解忧，也可克制官鬼。",
        "妻财" => "妻财爻：钱财、资源、客户、现实收益；男测感情也常取为对象。",
        "官鬼" => "官鬼爻：事业、职位、压力、规则、疾病、风险与约束。",
        _ => null
    };

    private static string? SixKinZh(string? label) => label switch
    {
        "Parent" => "父母",
        "Sibling" => "兄弟",
        "Offspring" => "子孙",
        "WifeWealth" or "Wealth" => "妻财",
        "Officer" or "OfficerGhost" => "官鬼",
        _ => label
    };

    private static string? SixSpiritZh(string? label) => label switch
    {
        "AzureDragon" => "青龙",
        "VermilionBird" => "朱雀",
        "HookChen" => "勾陈",
        "CoiledSnake" => "螣蛇",
        "WhiteTiger" => "白虎",
        "BlackTortoise" => "玄武",
        _ => label
    };

    private static string? RoleZh(string? label) => label switch
    {
        "Worldly" => "世",
        "Corresponding" => "应",
        _ => label
    };

    private static string PositionZh(string label) => label switch
    {
        "First" => "初爻",
        "Second" => "二爻",
        "Third" => "三爻",
        "Fourth" => "四爻",
        "Fifth" => "五爻",
        "Sixth" => "上爻",
        _ => label
    };

    private static string StarZh(string label) => label switch
    {
        "Nobleman" => "贵人",
        "SalarySpirit" => "禄神",
        "CultureFlourish" => "文昌",
        "PostHorse" => "驿马",
        "PeachBlossom" => "桃花",
        "YangBlade" => "羊刃",
        "GeneralsStar" => "将星",
        "Canopy" => "华盖",
        "StarOfStrategy" => "谋星",
        "DisasterMalignity" => "灾煞",
        "RobberyMalignity" => "劫煞",
        "DeathSpirit" => "亡神",
        "CelestialPhysician" => "天医",
        "HeavenlyJoy" => "天喜",
        "MarriageBed" => "婚床",
        "BridalChamber" => "洞房",
        _ => label
    };
}

public record HiddenDeityDetail(string StemBranch, string SixKin);

public record SymbolicStarEntry(string Name, IReadOnlyList<string> Branches);

public record LiuyaoLineDetail(
    int Index,
    string Position,
    string YinYang,
    string YinYangDescription,
    string? StemBranch,
    string? SixKin,
    string? SixKinDescription,
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
