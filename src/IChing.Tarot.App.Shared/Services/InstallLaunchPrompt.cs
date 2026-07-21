namespace IChing.Tarot.App.Services;

/// <summary>安装/更新后引导用户进入应用（系统安装器有时只显示「完成」）。</summary>
public static class InstallLaunchPrompt
{
    private const string LastPromptedBuildKey = "install_last_prompted_build";

    public static async Task TryPromptAsync(Window window)
    {
        await Task.Delay(300);
        var page = Shell.Current?.CurrentPage as ContentPage
                   ?? window.Page as ContentPage;
        if (page is null)
        {
            return;
        }

        var build = AppInfo.Current.BuildString;
        var lastPrompted = Preferences.Default.Get(LastPromptedBuildKey, string.Empty);
        if (lastPrompted == build)
        {
            return;
        }

        var isFirstEver = VersionTracking.IsFirstLaunchEver;
        var isNewVersion = VersionTracking.IsFirstLaunchForCurrentVersion;
        if (!isFirstEver && !isNewVersion)
        {
            return;
        }

        Preferences.Default.Set(LastPromptedBuildKey, build);
        var title = isFirstEver ? "安装完成" : "更新完成";
        var message = isFirstEver
            ? "星轨塔罗已安装。现在可以开始探索趣味工具与占卜功能。"
            : $"已更新到 v{AppInfo.Current.VersionString}。是否立即打开探索页？";

        var open = await page.DisplayAlertAsync(title, message, "打开探索", "稍后");
        if (open)
        {
            await Shell.Current!.GoToAsync("//explore");
        }
    }
}
