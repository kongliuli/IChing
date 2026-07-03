using IChing.Lab.Abstractions.Engines;
using IChing.Lab.Abstractions.Models;

namespace IChing.Lab.Engines.Tarot;

/// <summary>
/// Deckaura 数据插件引擎：内嵌 78 牌 12 维牌义数据集，提供 Tier 0 牌义库查询。
/// <para>EngineId = "tarot-deckaura-data"，Domain = "tarot"。</para>
/// <para>Calculate 输入 Args["cardName"]，返回该牌的 12 维牌义对象；未提供 cardName 时返回全部 78 牌索引。</para>
/// <para>未命中返回含 error 的对象，不抛异常。</para>
/// </summary>
public sealed class TarotDeckauraDataEngine : IChartEngine
{
    /// <inheritdoc />
    public string Domain => "tarot";

    /// <inheritdoc />
    public string EngineId => "tarot-deckaura-data";

    /// <inheritdoc />
    public EngineMetadata Metadata { get; } = new(
        Source: "tarot-card-meanings(Deckaura)",
        Version: "1.0.2",
        AlgorithmBasis: "78 牌 12 维牌义数据集（DOI 10.5281/zenodo.19152918）",
        TemplateHint: "deckaura",
        ModuleFocus: ["paiyi", "12wei"]);

    /// <summary>
    /// 查询牌义：Args["cardName"] 指定牌名（不区分大小写），命中返回 12 维牌义对象；
    /// 未提供 cardName 时返回 { engine, total=78, cards=[牌名列表] }；未命中返回 { engine, error }。
    /// </summary>
    public object Calculate(ChartRequest request)
    {
        var cardName = TryGetArg<string>(request.Args, "cardName");

        if (string.IsNullOrWhiteSpace(cardName))
        {
            return new
            {
                engine = new { paipan = EngineId, ready = true },
                total = TarotDeckData.Cards.Count,
                cards = TarotDeckData.Cards.Select(c => c.Name).ToArray()
            };
        }

        var card = TarotDeckData.FindByNameIgnoreCase(cardName);
        if (card is null)
        {
            return new
            {
                engine = new { paipan = EngineId, ready = true },
                error = "card not found",
                cardName
            };
        }

        return new
        {
            engine = new { paipan = EngineId, ready = true },
            card = new
            {
                number = card.Number,
                name = card.Name,
                arcana = card.Arcana,
                suit = card.Suit,
                element = card.Element,
                planet = card.Planet,
                upright = card.Upright,
                reversed = card.Reversed,
                love = card.Love,
                career = card.Career,
                yesNo = card.YesNo,
                keywords = card.Keywords
            }
        };
    }

    private static T? TryGetArg<T>(IDictionary<string, object?> args, string key)
    {
        if (args.TryGetValue(key, out var raw) && raw is T typed)
        {
            return typed;
        }

        // 容错：JSON 反序列化可能产生非 T 类型（如 JsonElement），尝试字符串转换。
        if (args.TryGetValue(key, out var rawObj) && rawObj is not null)
        {
            return (T)(object)Convert.ToString(rawObj)!;
        }

        return default;
    }
}
