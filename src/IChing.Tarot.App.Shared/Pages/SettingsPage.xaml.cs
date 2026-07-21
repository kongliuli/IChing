using IChing.Client.Shared.Editions;
using IChing.Client.Shared.Onnx;
using IChing.Tarot.App.Services;

namespace IChing.Tarot.App.Pages;

public partial class SettingsPage : ContentPage
{
    private bool _showKey;
    private readonly LocalModelDownloader _modelDownloader = new();
    private CancellationTokenSource? _downloadCts;

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
        var caps = EditionHost.Capabilities;
        var settings = App.Settings;

        ApiSettingsPanel.IsVisible = caps.ShowApiKeySettings;
        LabApiPanel.IsVisible = caps.ShowLabUrlSettings;
        OnnxPanel.IsVisible = caps.AllowLocalOnnx;
        BizServicePanel.IsVisible = caps.Kind == EditionKind.Commercial;
        FreeUpgradePanel.IsVisible = caps.Kind == EditionKind.Free;
        AndroidToolsPanel.IsVisible = DeviceInfo.Platform == DevicePlatform.Android
                                      && (caps.ShowLabUrlSettings || caps.AllowLocalOnnx);

        EditionHintLabel.Text = caps.Kind switch
        {
            EditionKind.Free =>
                "免费版：本地抽牌 + 牌面与基本牌义。不含 AI 解读、无 API Key、无追问。",
            EditionKind.Byok =>
                "自助版：使用你自己的 OpenAI 兼容 API Key（SecureStorage 加密存储）。",
            EditionKind.Commercial =>
                "商业版：解读走自建 Lab 服务，Key 只在服务端。",
            _ => "开发壳：Lab / BYOK / 端侧 ONNX 均可配置。"
        };

        if (FreeUpgradePanel.IsVisible)
        {
            UpgradeLinkLabel.Text = string.IsNullOrWhiteSpace(EditionHost.UpgradeStoreUrl)
                ? "商店升级链接待配置（EditionHost.UpgradeStoreUrl）。"
                : $"升级入口：{EditionHost.UpgradeStoreUrl}";
        }

        if (BizServicePanel.IsVisible)
        {
            BizServiceStatusLabel.Text = $"已连接服务配置：{MaskHost(App.Settings.LabApiUrl)}";
        }

        ProviderPicker.SelectedIndex = settings.Provider switch
        {
            "openai" => 1,
            "ollama" => 2,
            "custom" => 3,
            _ => 0
        };
        BaseUrlEntry.Text = settings.BaseUrl;
        ModelEntry.Text = settings.Model;
        ApiKeyEntry.Text = settings.ApiKey;
        ApiKeyEntry.IsPassword = !_showKey;
        ToggleKeyButton.Text = _showKey ? "隐藏" : "显示";
        RefreshLocalEndpointHint();
        UseLabSwitch.IsToggled = settings.UseLabApi;
        LabApiUrlEntry.Text = settings.LabApiUrl;
        App.CardImages.CdnBaseUrl = settings.CardCdnBaseUrl;
        UpdateStatus();
        UpdateCacheStatus();
        UpdateOnnxStatus();
        VersionLabel.Text = $"{EditionHost.DisplayName} v{AppInfo.Current.VersionString} (build {AppInfo.Current.BuildString})";
        if (App.Settings.IsApiKeyUsingFallbackStorage && caps.ShowApiKeySettings)
        {
            StatusLabel.Text = "提示：本机 SecureStorage 不可用，Key 回退 Preferences（明文）";
            StatusLabel.TextColor = Color.FromArgb("#E0A84C");
        }

        ExploreConfigPathLabel.Text =
            $"内置 explore-modules.json；可放置覆盖：{Path.Combine(FileSystem.AppDataDirectory, "explore-modules.json")}";
    }

    private static string MaskHost(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "（未配置）";
        }

        return $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : ":" + uri.Port)}";
    }

    private async void OnTestBizServiceClicked(object? sender, EventArgs e)
    {
        var result = await App.Interpretation.TestConnectionAsync(App.Settings);
        BizServiceStatusLabel.Text = result.Ok
            ? $"✓ 服务在线 · {MaskHost(App.Settings.LabApiUrl)}"
            : "✗ 服务暂不可用，解读将回退基本牌义";
        BizServiceStatusLabel.TextColor = result.Ok
            ? Color.FromArgb("#7FD992")
            : Color.FromArgb("#E06C75");
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
                App.Settings.ApplyProviderPreset("ollama");
                break;
            case 3:
                App.Settings.Provider = "custom";
                break;
        }

        if (ProviderPicker.SelectedIndex != 3)
        {
            BaseUrlEntry.Text = App.Settings.BaseUrl;
            ModelEntry.Text = App.Settings.Model;
        }

        RefreshLocalEndpointHint();
        UpdateStatus();
    }

    private void RefreshLocalEndpointHint()
    {
        var local = ProviderPicker.SelectedIndex == 2
                    || IChing.Lab.Core.Integrations.OpenAiEndpointHelpers.IsLocalBaseUrl(BaseUrlEntry.Text);
        LocalEndpointHintLabel.IsVisible = local && ApiSettingsPanel.IsVisible;
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
        App.Settings.LabApiUrl = LabApiUrlEntry.Text?.Trim() ?? EditionHost.DefaultLabApiUrl;
        App.Settings.UseLabApi = UseLabSwitch.IsToggled;
        App.CardImages.CdnBaseUrl = App.Settings.CardCdnBaseUrl;
        if (ProviderPicker.SelectedIndex == 3)
        {
            App.Settings.Provider = "custom";
        }

        UpdateStatus();
        await DisplayAlertAsync("已保存", "设置已写入本机。", "好的");
    }

    private async void OnTestClicked(object? sender, EventArgs e)
    {
        App.Settings.BaseUrl = BaseUrlEntry.Text?.Trim() ?? AppSettings.DefaultDeepSeekUrl;
        App.Settings.Model = ModelEntry.Text?.Trim() ?? "deepseek-chat";
        App.Settings.ApiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
        App.Settings.LabApiUrl = LabApiUrlEntry.Text?.Trim() ?? EditionHost.DefaultLabApiUrl;
        App.Settings.UseLabApi = UseLabSwitch.IsToggled;

        var caps = EditionHost.Capabilities;
        if (caps.AllowLocalOnnx && !caps.ShowApiKeySettings && !caps.ShowLabUrlSettings)
        {
            await TestOnnxAsync();
            return;
        }

        if (!caps.AllowAiInterpretation)
        {
            StatusLabel.Text = "免费版无需配置解读服务";
            StatusLabel.TextColor = Color.FromArgb("#7FD992");
            return;
        }

        if (!App.Settings.IsConfigured && !App.Settings.UseLabApi)
        {
            if (caps.AllowLocalOnnx && App.Settings.HasLocalOnnxModel)
            {
                await TestOnnxAsync();
                return;
            }

            await DisplayAlertAsync("缺少配置", "请填写 API Key、启用 Lab API，或先导入端侧 ONNX。", "好的");
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

    private void UpdateOnnxStatus()
    {
        if (!OnnxPanel.IsVisible)
        {
            return;
        }

        var path = App.Settings.ResolveLocalOnnxModelPath();
        var ready = File.Exists(Path.Combine(path, "genai_config.json"));
        OnnxStatusLabel.Text = ready
            ? $"✓ 模型就绪：{path}"
            : $"未就绪。目标目录：{path}";
        OnnxStatusLabel.TextColor = ready
            ? Color.FromArgb("#7FD992")
            : Color.FromArgb("#6E6380");
        OnnxProgress.Progress = ready ? 1 : 0;
    }

    private async void OnDownloadModelClicked(object? sender, EventArgs e)
    {
        var pack = OnnxModelPackCatalog.Qwen25_15b;
        var ok = await DisplayAlertAsync(
            "下载端侧模型",
            $"将从 HuggingFace 下载 {pack.DisplayName}（约 6GB）到本机应用目录。继续？",
            "开始下载",
            "取消");
        if (!ok)
        {
            return;
        }

        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();
        DownloadModelButton.IsEnabled = false;
        ImportModelButton.IsEnabled = false;
        try
        {
            var dest = Path.Combine(FileSystem.AppDataDirectory, "models", pack.Id);
            var progress = new Progress<ModelDownloadProgress>(p =>
            {
                var filePart = (p.Index - 1 + p.FileFraction) / Math.Max(p.Total, 1);
                OnnxProgress.Progress = Math.Clamp(filePart, 0, 1);
                OnnxStatusLabel.Text = $"{p.Phase} {p.FileName} ({p.Index}/{p.Total})";
            });
            await _modelDownloader.EnsurePackAsync(pack, dest, progress, _downloadCts.Token);
            App.Settings.LocalOnnxModelId = pack.Id;
            App.Settings.LocalOnnxModelPath = dest;
            UpdateOnnxStatus();
            await DisplayAlertAsync("完成", "模型已下载，可在抽牌时使用端侧解读。", "好的");
        }
        catch (OperationCanceledException)
        {
            OnnxStatusLabel.Text = "下载已取消";
        }
        catch (Exception ex)
        {
            OnnxStatusLabel.Text = $"下载失败：{ex.Message}";
            OnnxStatusLabel.TextColor = Color.FromArgb("#E06C75");
            await DisplayAlertAsync("下载失败", ex.Message, "好的");
        }
        finally
        {
            DownloadModelButton.IsEnabled = true;
            ImportModelButton.IsEnabled = true;
        }
    }

    private async void OnImportModelClicked(object? sender, EventArgs e)
    {
        ImportModelButton.IsEnabled = false;
        try
        {
            var progress = new Progress<string>(msg => OnnxStatusLabel.Text = msg);
            var dest = LocalModelDownloader.TryImportFromDevRepo(
                Qwen35ModelCatalog.LegacyId,
                FileSystem.AppDataDirectory,
                progress);
            if (dest is null)
            {
                await DisplayAlertAsync(
                    "未找到本地包",
                    "未在仓库 models/qwen2.5-1.5b-genai 找到 genai_config.json。可先运行 scripts/download-qwen-15b-model.ps1，或改用 HuggingFace 下载。",
                    "好的");
                return;
            }

            App.Settings.LocalOnnxModelId = Qwen35ModelCatalog.LegacyId;
            App.Settings.LocalOnnxModelPath = dest;
            UpdateOnnxStatus();
            await DisplayAlertAsync("已导入", dest, "好的");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("导入失败", ex.Message, "好的");
        }
        finally
        {
            ImportModelButton.IsEnabled = true;
        }
    }

    private async void OnTestOnnxClicked(object? sender, EventArgs e) => await TestOnnxAsync();

    private async Task TestOnnxAsync()
    {
        var result = await App.Interpretation.TestConnectionAsync(App.Settings);
        OnnxStatusLabel.Text = result.Ok
            ? $"✓ {result.Error}"
            : $"✗ {result.Error}";
        OnnxStatusLabel.TextColor = result.Ok
            ? Color.FromArgb("#7FD992")
            : Color.FromArgb("#E06C75");
        if (!OnnxPanel.IsVisible)
        {
            StatusLabel.Text = OnnxStatusLabel.Text;
            StatusLabel.TextColor = OnnxStatusLabel.TextColor;
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
        var caps = EditionHost.Capabilities;
        if (!caps.AllowAiInterpretation)
        {
            StatusLabel.Text = "免费版：仅牌面与基本牌义";
            StatusLabel.TextColor = Color.FromArgb("#7FD992");
            return;
        }

        if (caps.AllowLocalOnnx && !caps.ShowApiKeySettings)
        {
            StatusLabel.Text = "可选用本地端侧 ONNX（开发壳）";
            StatusLabel.TextColor = Color.FromArgb("#7FD992");
            return;
        }

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
