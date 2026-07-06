#if ANDROID
using Android.Content;
using AndroidX.Core.Content;
using Application = Android.App.Application;

namespace IChing.Tarot.App.Platforms.Android;

public static class ApkInstallHelper
{
    public static void LaunchInstaller(string apkPath)
    {
        var context = Platform.CurrentActivity ?? Application.Context;
        var file = new Java.IO.File(apkPath);
        var authority = $"{context.PackageName}.fileProvider";
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(context, authority, file);

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
        context.StartActivity(intent);
    }
}
#endif
