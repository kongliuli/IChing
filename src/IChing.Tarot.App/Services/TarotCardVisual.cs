namespace IChing.Tarot.App.Services;

/// <summary>牌面占位视觉：花色配色 + 可选本地 RWS 图片（Resources/Images/tarot/）。</summary>
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

    /// <summary>若已打包 RWS 资源（Resources/Images/tarot_*.jpeg）则返回 ImageSource。</summary>
    public static ImageSource? TryImage(string cardName) =>
        ImageCache.TryGet(Slug(cardName));

    private static class ImageCache
    {
        private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static ImageSource? TryGet(string slug)
        {
            if (Cache.TryGetValue(slug, out var cached))
            {
                return cached;
            }

            var file = $"tarot_{slug}.jpeg";
            ImageSource? source = null;
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync(file).GetAwaiter().GetResult();
                if (stream.Length > 0)
                {
                    source = ImageSource.FromFile(file);
                }
            }
            catch
            {
                // 未下载牌面图时用占位 UI
            }

            Cache[slug] = source;
            return source;
        }
    }
}
