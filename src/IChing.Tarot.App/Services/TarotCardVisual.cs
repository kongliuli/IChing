namespace IChing.Tarot.App.Services;

/// <summary>牌面占位视觉：花色配色 + 本地 RWS 图片（Resources/Images/tarot_*.jpeg）。</summary>
public static class TarotCardVisual
{
    public static string Slug(string cardName) =>
        cardName.ToLowerInvariant().Replace(" ", "-");

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

    /// <summary>MauiImage 打包资源，文件名 tarot_{slug}.jpeg（连字符转下划线）。</summary>
    public static ImageSource? TryImage(string cardName) =>
        ImageCache.TryGet(Slug(cardName));

    public static string AssetFileName(string slug) =>
        $"tarot_{slug.Replace('-', '_')}.jpeg";

    private static class ImageCache
    {
        private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static ImageSource? TryGet(string slug)
        {
            if (Cache.TryGetValue(slug, out var cached))
            {
                return cached;
            }

            var file = AssetFileName(slug);
            ImageSource? source = null;
            try
            {
                source = ImageSource.FromFile(file);
            }
            catch
            {
                // ponytail: 未 sync 牌面图时占位
            }

            Cache[slug] = source;
            return source;
        }
    }
}
