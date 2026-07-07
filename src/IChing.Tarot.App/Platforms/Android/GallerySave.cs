using Android.Content;
using Android.OS;
using Android.Provider;
using Environment = Android.OS.Environment;
using AndroidMedia = Android.Media;

namespace IChing.Tarot.App.Platforms.Android;

public static class GallerySave
{
    private const string AlbumFolder = "IChingTarot";

    public static Task<string?> SaveAsync(string filePath, CancellationToken ct) =>
        MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var context = Platform.CurrentActivity ?? Platform.AppContext;
                if (context is null)
                {
                    return null;
                }

                var fileName = $"tarot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                return Build.VERSION.SdkInt >= BuildVersionCodes.Q
                    ? SaveViaMediaStore(context, filePath, fileName)
                    : SaveLegacy(context, filePath, fileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GallerySave] {ex}");
                return null;
            }
        });

    private static string? SaveViaMediaStore(Context context, string filePath, string fileName)
    {
        var values = new ContentValues();
        values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
        values.Put(MediaStore.IMediaColumns.MimeType, "image/png");
        values.Put(MediaStore.IMediaColumns.RelativePath, $"Pictures/{AlbumFolder}");
        values.Put(MediaStore.IMediaColumns.IsPending, 1);

        var resolver = context.ContentResolver!;
        var uri = resolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);
        if (uri is null)
        {
            return null;
        }

        using (var input = File.OpenRead(filePath))
        using (var output = resolver.OpenOutputStream(uri))
        {
            if (output is null)
            {
                resolver.Delete(uri, null, null);
                return null;
            }

            input.CopyTo(output);
        }

        values.Clear();
        values.Put(MediaStore.IMediaColumns.IsPending, 0);
        resolver.Update(uri, values, null, null);
        return uri.ToString();
    }

    private static string? SaveLegacy(Context context, string filePath, string fileName)
    {
        var pictures = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures);
        var dir = new Java.IO.File(pictures, AlbumFolder);
        if (!dir.Exists() && !dir.Mkdirs())
        {
            return null;
        }

        var dest = new Java.IO.File(dir, fileName);
        File.Copy(filePath, dest.AbsolutePath, true);
        AndroidMedia.MediaScannerConnection.ScanFile(
            context,
            [dest.AbsolutePath],
            ["image/png"],
            null);
        return dest.AbsolutePath;
    }
}
