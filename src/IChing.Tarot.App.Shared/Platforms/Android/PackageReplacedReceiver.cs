using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace IChing.Tarot.App;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionMyPackageReplaced })]
public class PackageReplacedReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent?.Action != Intent.ActionMyPackageReplaced)
        {
            return;
        }

        InstallNotificationHelper.ShowOpenAppNotification(context);
    }
}

internal static class InstallNotificationHelper
{
    private const string ChannelId = "iching_tarot_install";
    private const int NotificationId = 1001;

    public static void ShowOpenAppNotification(Context context)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
            var channel = new NotificationChannel(ChannelId, "安装提示", NotificationImportance.High)
            {
                Description = "安装或更新完成后的打开提示"
            };
            manager?.CreateNotificationChannel(channel);
        }

        var launch = new Intent(context, typeof(MainActivity));
        launch.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask | ActivityFlags.SingleTop);
        var pending = PendingIntent.GetActivity(
            context,
            0,
            launch,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var iconId = context.ApplicationInfo?.Icon ?? Android.Resource.Drawable.StatSysDownloadDone;
        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle("星轨塔罗已就绪")
            .SetContentText("点击打开应用")
            .SetSmallIcon(iconId)
            .SetAutoCancel(true)
            .SetContentIntent(pending)
            .AddAction(0, "打开", pending)
            .Build();

        NotificationManagerCompat.From(context).Notify(NotificationId, notification);
    }
}
