using IChing.Bazi.App.Services;

namespace IChing.Bazi.App.Pages;

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
        UpdateStatus();
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

        TestButton.IsEnabled = false;
        TestIndicator.IsVisible = true;
        TestIndicator.IsRunning = true;

        try
        {
            var result = await App.Interpretation.TestConnectionAsync(App.Settings);
            StatusLabel.Text = result.Ok
                ? $"✓ {result.Error ?? "Lab API 在线"}"
                : $"✗ 连接失败：{result.Error}";
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
}

