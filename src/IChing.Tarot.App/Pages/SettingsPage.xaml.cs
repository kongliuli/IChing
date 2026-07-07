using IChing.Tarot.App.Services;

namespace IChing.Tarot.App.Pages;

public partial class SettingsPage : ContentPage
{
    private bool _showKey;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = App.Settings;
        ProviderPicker.SelectedIndex = settings.Provider switch
        {
            "openai" => 1,
            "custom" => 2,
            _ => 0
        };
        BaseUrlEntry.Text = settings.BaseUrl;
        ModelEntry.Text = settings.Model;
        ApiKeyEntry.Text = settings.ApiKey;
        ApiKeyEntry.IsPassword = !_showKey;
        ToggleKeyButton.Text = _showKey ? "隐藏" : "显示";
        UseLabSwitch.IsToggled = settings.UseLabApi;
        LabApiUrlEntry.Text = settings.LabApiUrl;
        App.CardImages.CdnBaseUrl = settings.CardCdnBaseUrl;
        UpdateStatus();
        UpdateCacheStatus();
        VersionLabel.Text = $"星轨塔罗 v{AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";
        AndroidToolsPanel.IsVisible = DeviceInfo.Platform == DevicePlatform.Android;
        ExploreConfigPathLabel.Text =
            $"内置 explore-modules.json；可放置覆盖：{Path.Combine(FileSystem.AppDataDirectory, "explore-modules.json")}";
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        switch (ProviderPicker.SelectedIndex)
        {
            case 0:
                App.Settings.ApplyProviderPreset("deepseek");
                break;
            case 1:
                App.Settings.ApplyProviderPreset("openai");
                break;
            case 2:
                App.Settings.Provider = "custom";
                break;
        }

        if (ProviderPicker.SelectedIndex != 2)
        {
            BaseUrlEntry.Text = App.Settings.BaseUrl;
            ModelEntry.Text = App.Settings.Model;
        }
    }

    private void OnToggleKeyClicked(object? sender, EventArgs e)
    {
        _showKey = !_showKey;
        ApiKeyEntry.IsPassword = !_showKey;
        ToggleKeyButton.Text = _showKey ? "隐藏" : "显示";
    }

    private void OnUseLabToggled(object? sender, ToggledEventArgs e)
    {
        App.Settings.UseLabApi = e.Value;
        UpdateStatus();
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        App.Settings.BaseUrl = BaseUrlEntry.Text?.Trim() ?? AppSettings.DefaultDeepSeekUrl;
        App.Settings.Model = ModelEntry.Text?.Trim() ?? "deepseek-chat";
        App.Settings.ApiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
        App.Settings.LabApiUrl = LabApiUrlEntry.Text?.Trim() ?? AppSettings.DefaultLabApiUrl;
        App.Settings.UseLabApi = UseLabSwitch.IsToggled;
        App.CardImages.CdnBaseUrl = App.Settings.CardCdnBaseUrl;
        if (ProviderPicker.SelectedIndex == 2)
        {
            App.Settings.Provider = "custom";
        }

        UpdateStatus();
        await DisplayAlertAsync("已保存", "API 设置已写入本机。", "好的");
    }

    private async void OnTestClicked(object? sender, EventArgs e)
    {
        App.Settings.BaseUrl = BaseUrlEntry.Text?.Trim() ?? AppSettings.DefaultDeepSeekUrl;
        App.Settings.Model = ModelEntry.Text?.Trim() ?? "deepseek-chat";
        App.Settings.ApiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
        App.Settings.LabApiUrl = LabApiUrlEntry.Text?.Trim() ?? AppSettings.DefaultLabApiUrl;
        App.Settings.UseLabApi = UseLabSwitch.IsToggled;

        if (!App.Settings.IsConfigured && !App.Settings.UseLabApi)
        {
            await DisplayAlertAsync("缺少配置", "请填写 API Key 或启用 Lab API。", "好的");
            return;
        }

        TestButton.IsEnabled = false;
        TestIndicator.IsVisible = true;
        TestIndicator.IsRunning = true;

        try
        {
            var result = await App.Interpretation.TestConnectionAsync(App.Settings);
            StatusLabel.Text = result.Ok
                ? $"✓ 连接成功 · {App.Settings.Model}"
                : $"✗ 连接失败：{UserFacingZh.Error(result.Error)}";
            StatusLabel.TextColor = result.Ok
                ? Color.FromArgb("#7FD992")
                : Color.FromArgb("#E06C75");
        }
        finally
        {
            TestIndicator.IsRunning = false;
            TestIndicator.IsVisible = false;
            TestButton.IsEnabled = true;
        }
    }

    private void UpdateCacheStatus()
    {
        var count = App.CardImages.CachedCount;
        var mb = App.CardImages.GetCacheSizeBytes() / 1024d / 1024d;
        CacheStatusLabel.Text = $"已缓存 {count}/78 张 · {mb:F1} MB";
        CacheStatusLabel.TextColor = count > 0
            ? Color.FromArgb("#7FD992")
            : Color.FromArgb("#6E6380");
    }

    private async void OnPreloadClicked(object? sender, EventArgs e)
    {
        PreloadButton.IsEnabled = false;
        ClearCacheButton.IsEnabled = false;
        CacheIndicator.IsVisible = true;
        CacheIndicator.IsRunning = true;

        try
        {
            var progress = new Progress<(int done, int total)>(p =>
                CacheStatusLabel.Text = $"下载中 {p.done}/{p.total}…");

            var (ok, fail) = await App.CardImages.PreloadAllAsync(progress);
            UpdateCacheStatus();
            await DisplayAlertAsync(
                "预下载完成",
                $"成功 {ok} 张，失败 {fail} 张。",
                "好的");
        }
        finally
        {
            CacheIndicator.IsRunning = false;
            CacheIndicator.IsVisible = false;
            PreloadButton.IsEnabled = true;
            ClearCacheButton.IsEnabled = true;
        }
    }

    private async void OnClearCacheClicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("清除缓存", "确定删除全部已下载牌面图？", "清除", "取消"))
        {
            return;
        }

        var removed = App.CardImages.ClearCache();
        UpdateCacheStatus();
        await DisplayAlertAsync("已清除", $"已删除 {removed} 个缓存文件。", "好的");
    }

    private void UpdateStatus()
    {
        StatusLabel.Text = App.Settings.UseLabApi
            ? (App.Settings.IsLabConfigured ? "✓ 已启用 Lab API 解读" : "Lab API 地址为空")
            : App.Settings.IsConfigured
                ? "✓ 已配置远程 API Key"
                : "尚未配置解读来源（Lab 或远程 API）";
        StatusLabel.TextColor = App.Settings.UseLabApi || App.Settings.IsConfigured
            ? Color.FromArgb("#7FD992")
            : Color.FromArgb("#6E6380");
    }

    private async void OnInstallApkClicked(object? sender, EventArgs e)
    {
#if ANDROID
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "选择 APK 文件",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = [".apk", "application/vnd.android.package-archive"]
            })
        });
        if (result is null)
        {
            return;
        }

        var cachePath = Path.Combine(FileSystem.CacheDirectory, "install-update.apk");
        await using (var read = await result.OpenReadAsync())
        await using (var write = File.OpenWrite(cachePath))
        {
            await read.CopyToAsync(write);
        }

        Platforms.Android.ApkInstallHelper.LaunchInstaller(cachePath);
        await DisplayAlertAsync(
            "正在安装",
            "请在系统安装界面确认。若只显示「完成」，安装后请查看通知栏「点击打开」，或从桌面图标进入。",
            "好的");
#else
        await DisplayAlertAsync("仅 Android", "此功能仅在 Android 上可用。", "好的");
#endif
    }
}

