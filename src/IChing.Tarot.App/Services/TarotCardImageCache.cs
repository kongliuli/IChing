using IChing.Lab.Core.Tarot;

namespace IChing.Tarot.App.Services;

/// <summary>牌面图按需下载 + 本地磁盘缓存（APK 不再打包 78 张图）。</summary>
public sealed class TarotCardImageCache
{
    public const string DefaultCdnBase =
        "https://cdn.jsdelivr.net/gh/mixvlad/TarotCards@main/tarot/rider-waite/720px";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    private readonly string _cacheDir;

    public TarotCardImageCache()
    {
        _cacheDir = Path.Combine(FileSystem.CacheDirectory, "tarot-rws");
        Directory.CreateDirectory(_cacheDir);
    }

    public string CdnBaseUrl { get; set; } = DefaultCdnBase;

    public string CacheDirectory => _cacheDir;

    public int CachedCount =>
        Directory.Exists(_cacheDir)
            ? Directory.GetFiles(_cacheDir, "*.jpeg").Length
            : 0;

    public ImageSource? TryGetLocal(string cardName)
    {
        var path = GetCachePath(cardName);
        return File.Exists(path) && new FileInfo(path).Length > 1000
            ? ImageSource.FromFile(path)
            : null;
    }

    public async Task<ImageSource?> GetImageAsync(string cardName, CancellationToken ct = default)
    {
        var local = TryGetLocal(cardName);
        if (local is not null)
        {
            return local;
        }

        var path = GetCachePath(cardName);
        foreach (var url in BuildDownloadUrls(cardName))
        {
            try
            {
                using var response = await Http.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                if (bytes.Length <= 1000)
                {
                    continue;
                }

                await File.WriteAllBytesAsync(path, bytes, ct);
                return ImageSource.FromFile(path);
            }
            catch
            {
                // ponytail: 尝试下一个镜像
            }
        }

        return null;
    }

    public async Task<(int ok, int fail)> PreloadAllAsync(
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        var cards = TarotDeck.All;
        var ok = 0;
        var fail = 0;
        for (var i = 0; i < cards.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var img = await GetImageAsync(cards[i].Name, ct);
            if (img is null)
            {
                fail++;
            }
            else
            {
                ok++;
            }

            progress?.Report((i + 1, cards.Count));
        }

        return (ok, fail);
    }

    public int ClearCache()
    {
        if (!Directory.Exists(_cacheDir))
        {
            return 0;
        }

        var count = 0;
        foreach (var file in Directory.GetFiles(_cacheDir))
        {
            try
            {
                File.Delete(file);
                count++;
            }
            catch
            {
                // ponytail: 忽略单文件删除失败
            }
        }

        return count;
    }

    public long GetCacheSizeBytes()
    {
        if (!Directory.Exists(_cacheDir))
        {
            return 0;
        }

        return Directory.GetFiles(_cacheDir).Sum(f => new FileInfo(f).Length);
    }

    internal string GetCachePath(string cardName) =>
        Path.Combine(_cacheDir, $"{TarotRwsImageCatalog.Slug(cardName)}.jpeg");

    internal IEnumerable<string> BuildDownloadUrls(string cardName)
    {
        var slug = TarotRwsImageCatalog.Slug(cardName);
        var settings = App.Settings;

        if (settings.UseLabApi && !string.IsNullOrWhiteSpace(settings.LabApiUrl))
        {
            yield return $"{settings.LabApiUrl.TrimEnd('/')}/tarot/rws/{slug}.jpeg";
        }

        if (TarotRwsImageCatalog.TryGetSrcFile(cardName, out var srcFile))
        {
            var cdn = (settings.CardCdnBaseUrl ?? DefaultCdnBase).TrimEnd('/');
            yield return $"{cdn}/{srcFile}";
            yield return $"https://raw.githubusercontent.com/mixvlad/TarotCards/main/tarot/rider-waite/720px/{srcFile}";
        }
    }
}
