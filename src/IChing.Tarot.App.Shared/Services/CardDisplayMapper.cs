using IChing.Lab.Core.Tarot;
using IChing.Tarot.App.Models;

namespace IChing.Tarot.App.Services;

public static class CardDisplayMapper
{
    public static CardDisplayItem FromPosition(TarotPositionReading position, ImageSource? image = null) => new()
    {
        PositionTitle = position.PositionTitleZh,
        CardLine = UserFacingZh.CardLine(position),
        Meaning = position.Meaning,
        SuitAccent = TarotCardVisual.SuitAccent(position.CardName),
        Abbrev = TarotCardVisual.Abbrev(position.CardNameZh),
        IsReversed = position.Reversed,
        CardImage = image ?? TarotCardVisual.TryImage(position.CardName)
    };

    public static List<CardDisplayItem> FromReading(TarotReading reading) =>
        reading.Positions.Select(p => FromPosition(p)).ToList();

    public static async Task<List<CardDisplayItem>> FromReadingAsync(
        TarotReading reading,
        CancellationToken ct = default)
    {
        var items = new List<CardDisplayItem>(reading.Positions.Count);
        foreach (var position in reading.Positions)
        {
            var image = await App.CardImages.GetImageAsync(position.CardName, ct);
            items.Add(FromPosition(position, image));
        }

        return items;
    }
}
