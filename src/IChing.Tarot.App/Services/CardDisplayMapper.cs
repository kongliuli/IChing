using IChing.Lab.Core.Tarot;
using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

public static class CardDisplayMapper
{
    public static CardDisplayItem FromPosition(TarotPositionReading position) => new()
    {
        PositionTitle = position.PositionTitleZh,
        CardLine = $"{position.CardNameZh}（{position.CardName}）· {(position.Reversed ? "逆位" : "正位")}",
        Meaning = position.Meaning,
        SuitAccent = TarotCardVisual.SuitAccent(position.CardName),
        Abbrev = TarotCardVisual.Abbrev(position.CardNameZh),
        IsReversed = position.Reversed,
        CardImage = TarotCardVisual.TryImage(position.CardName)
    };

    public static List<CardDisplayItem> FromReading(TarotReading reading) =>
        reading.Positions.Select(FromPosition).ToList();
}
