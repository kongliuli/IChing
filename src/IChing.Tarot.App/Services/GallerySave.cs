namespace IChing.Tarot.App.Services;

/// <summary>跨平台保存图片到相册；失败时返回 null，调用方仍可用缓存路径分享。</summary>
public static class GallerySave
{
    public static Task<string?> TrySaveAsync(string filePath, CancellationToken ct = default) =>
#if ANDROID
        Platforms.Android.GallerySave.SaveAsync(filePath, ct);
#elif IOS
        Platforms.iOS.GallerySave.SaveAsync(filePath, ct);
#else
        Task.FromResult<string?>(null);
#endif
}
