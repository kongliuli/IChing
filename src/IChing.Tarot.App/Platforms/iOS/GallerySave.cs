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
                var data = NSData.FromFile(filePath);
                if (data is null)
                {
                    tcs.TrySetResult(null);
                    return;
                }

                PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(
                    () => PHAssetCreationRequest.FromImage(data),
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
