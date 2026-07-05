using System.Reflection;
using System.Text.Json;

namespace IChing.Lab.Core.Tarot;

/// <summary>
/// Layer-1 deterministic tarot: spread schema + seeded shuffle + upright/reversed.
/// </summary>
public static class TarotEngine
{
    public static TarotReading Draw(string spreadId, string? question, int? seed = null)
    {
        var spread = SpreadCatalog.Get(spreadId);
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var deck = (spread.MajorOnly ? TarotDeck.MajorOnly : TarotDeck.All).ToArray();
        for (var i = deck.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        var positions = new List<TarotPositionReading>();
        for (var i = 0; i < spread.Positions.Count; i++)
        {
            var card = deck[i];
            var reversed = rng.Next(2) == 0;
            var pos = spread.Positions[i];
            positions.Add(new TarotPositionReading(
                pos.Key,
                pos.Title,
                pos.TitleZh,
                pos.Context,
                card.Name,
                card.NameZh,
                card.ImageUrl,
                reversed,
                reversed ? card.ReversedMeaning : card.UprightMeaning
            ));
        }

        return new TarotReading(
            spread.Id,
            spread.Title,
            spread.TitleZh,
            spread.Description,
            spread.MajorOnly,
            question,
            seed,
            positions);
    }
}

public record TarotCard(int Id, string Name, string NameZh, string UprightMeaning, string ReversedMeaning, bool IsMajor)
{
    public string ImageUrl => $"/tarot/rws/{Name.ToLowerInvariant().Replace(" ", "-")}.jpeg";
}

public record TarotSpread(
    string Id,
    string Title,
    string TitleZh,
    string Description,
    string Category,
    string Difficulty,
    bool MajorOnly,
    IReadOnlyList<TarotPosition> Positions)
{
    public int CardCount => Positions.Count;
    public string DeckMode => MajorOnly ? "仅大阿卡纳" : "全牌 78 张";
}

public record TarotPosition(string Key, string Title, string TitleZh, string Context);

public record TarotPositionReading(
    string PositionKey,
    string PositionTitle,
    string PositionTitleZh,
    string PositionContext,
    string CardName,
    string CardNameZh,
    string ImageUrl,
    bool Reversed,
    string Meaning
);

public record TarotReading(
    string SpreadId,
    string SpreadTitle,
    string SpreadTitleZh,
    string SpreadDescription,
    bool MajorOnly,
    string? Question,
    int? Seed,
    IReadOnlyList<TarotPositionReading> Positions
);

public static class SpreadCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, TarotSpread>> SpreadsLazy = new(LoadSpreads);

    private static IReadOnlyDictionary<string, TarotSpread> Spreads => SpreadsLazy.Value;

    public static TarotSpread Get(string id) =>
        Spreads.TryGetValue(id, out var spread) ? spread : Spreads["past-present-future"];

    public static IReadOnlyList<TarotSpread> List() => Spreads.Values.ToList();

    private static IReadOnlyDictionary<string, TarotSpread> LoadSpreads()
    {
        var fromJson = TryLoadFromJson();
        return fromJson ?? BuildFallbackSpreads();
    }

    private static IReadOnlyDictionary<string, TarotSpread>? TryLoadFromJson()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("IChing.Lab.Core.Tarot.spreads.json");
            if (stream is null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var doc = JsonSerializer.Deserialize<SpreadConfigDocument>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (doc?.Spreads is null || doc.Spreads.Count == 0)
            {
                return null;
            }

            return doc.Spreads.ToDictionary(
                s => s.Id,
                s => new TarotSpread(
                    s.Id,
                    s.Title,
                    s.TitleZh,
                    s.Description,
                    s.Category,
                    s.Difficulty,
                    s.MajorOnly,
                    s.Positions.Select(p => new TarotPosition(p.Key, p.Title, p.TitleZh, p.Context)).ToList()),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, TarotSpread> BuildFallbackSpreads() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["past-present-future"] = new(
                "past-present-future",
                "Past-Present-Future",
                "过去-现在-未来",
                "三张牌时间线。",
                "general",
                "easiest",
                false,
                [
                    new("past", "Past", "过去", "已经发生的影响"),
                    new("present", "Present", "现在", "当前核心议题"),
                    new("future", "Future", "未来", "趋势与可能走向")
                ])
        };

    private sealed class SpreadConfigDocument
    {
        public List<SpreadConfigEntry> Spreads { get; set; } = [];
    }

    private sealed class SpreadConfigEntry
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string TitleZh { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public bool MajorOnly { get; set; }
        public List<SpreadPositionEntry> Positions { get; set; } = [];
    }

    private sealed class SpreadPositionEntry
    {
        public string Key { get; set; } = "";
        public string Title { get; set; } = "";
        public string TitleZh { get; set; } = "";
        public string Context { get; set; } = "";
    }
}
