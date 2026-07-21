using Foundation;
using Photos;
using UIKit;

namespace IChing.Tarot.App.Platforms.iOS;

public static class GallerySave
{
    public static Task<string?> SaveAsync(string filePath, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>();
        ct.Register(() => tcs.TrySetCanceled());

        PHPhotoLibrary.RequestAuthorization(status =>
        {
            if (status != PHAuthorizationStatus.Authorized &&
                status != PHAuthorizationStatus.Limited)
            {
                tcs.TrySetResult(null);
                return;
            }

            try
            {
                var image = UIImage.FromFile(filePath);
                if (image is null)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(
                    () => PHAssetCreationRequest.FromImage(image),
                    (ok, _) => tcs.TrySetResult(ok ? filePath : null));
            }
            catch
            {
                tcs.TrySetResult(null);
            }
        });

        return tcs.Task;
    }
}
