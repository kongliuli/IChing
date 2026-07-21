namespace IChing.Tarot.App.Services;

/// <summary>牌面占位视觉：花色配色 + 缓存 RWS 图片（TarotCardImageCache）。</summary>
public static class TarotCardVisual
{
    public static string Slug(string cardName) =>
        TarotRwsImageCatalog.Slug(cardName);

    public static Color SuitAccent(string cardName)
    {
        if (cardName.Contains(" of Wands", StringComparison.Ordinal))
        {
            return Color.FromArgb("#E07A3A");
        }

        if (cardName.Contains(" of Cups", StringComparison.Ordinal))
        {
            return Color.FromArgb("#4A9FD4");
        }

        if (cardName.Contains(" of Swords", StringComparison.Ordinal))
        {
            return Color.FromArgb("#8B9CB3");
        }

        if (cardName.Contains(" of Pentacles", StringComparison.Ordinal))
        {
            return Color.FromArgb("#6FAF6A");
        }

        return Color.FromArgb("#D4AF37");
    }

    public static string Abbrev(string cardNameZh) =>
        cardNameZh.Length <= 2 ? cardNameZh : cardNameZh[..2];

    public static ImageSource? TryImage(string cardName) =>
        App.CardImages.TryGetLocal(cardName);
}
