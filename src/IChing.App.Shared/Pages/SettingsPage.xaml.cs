using IChing.App.Services;
using IChing.Client.Shared.Editions;

namespace IChing.App.Pages;

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
        var caps = EditionHost.Capabilities;
        AiSettingsCard.IsVisible = caps.AllowAiInterpretation;
        ByokSettingsPanel.IsVisible = caps.ShowApiKeySettings;
        EditionHintLabel.IsVisible = caps.Kind == EditionKind.Commercial || caps.Kind == EditionKind.Free;
        EditionHintLabel.Text = caps.Kind switch
        {
            EditionKind.Commercial => $"商业版走自建 Lab（{App.Settings.LabApiUrl}）",
            EditionKind.Free => "免费版仅本地排盘，无 AI 解读",
            _ => string.Empty
        };

        if (!caps.ShowApiKeySettings)
        {
            UpdateStatus();
            return;
        }

        ProviderPicker.SelectedIndex = App.Settings.Provider switch
        {
            "openai" => 1,
            "custom" => 2,
            _ => 0
        };
        BaseUrlEntry.Text = App.Settings.BaseUrl;
        ModelEntry.Text = App.Settings.Model;
        ApiKeyEntry.Text = App.Settings.ApiKey;
        ApiKeyEntry.IsPassword = !_showKey;
        ToggleKeyButton.Text = _showKey ? "隐藏" : "显示";
        TemperatureEntry.Text = App.Settings.Temperature.ToString("0.##");
        MaxTokensEntry.Text = App.Settings.MaxTokens.ToString();
        UpdateStatus();
    }

    private void OnProviderChanged(object? sender, EventArgs e)
    {
        if (ProviderPicker.SelectedIndex == 0)
        {
            App.Settings.ApplyProviderPreset("deepseek");
        }
        else if (ProviderPicker.SelectedIndex == 1)
        {
            App.Settings.ApplyProviderPreset("openai");
        }
        else if (ProviderPicker.SelectedIndex == 2)
        {
            App.Settings.Provider = "custom";
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

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        Save();
        await DisplayAlertAsync("已保存", "AI 解读设置已保存到本机。", "好的");
    }

    private async void OnTestClicked(object? sender, EventArgs e)
    {
        Save();
        TestButton.IsEnabled = false;
        TestIndicator.IsVisible = true;
        TestIndicator.IsRunning = true;
        try
        {
            var result = await App.Interpretation.TestAsync(App.Settings);
            StatusLabel.Text = result.Ok ? $"连接成功 · {App.Settings.Model}" : $"连接失败：{result.Error}";
            StatusLabel.TextColor = result.Ok ? (Color)Application.Current!.Resources["Success"] : (Color)Application.Current!.Resources["Danger"];
        }
        finally
        {
            TestIndicator.IsRunning = false;
            TestIndicator.IsVisible = false;
            TestButton.IsEnabled = true;
        }
    }

    private void Save()
    {
        App.Settings.BaseUrl = BaseUrlEntry.Text?.Trim() ?? AppSettings.DefaultBaseUrl;
        App.Settings.Model = ModelEntry.Text?.Trim() ?? "deepseek-chat";
        App.Settings.ApiKey = ApiKeyEntry.Text?.Trim() ?? string.Empty;
        if (ProviderPicker.SelectedIndex == 2)
        {
            App.Settings.Provider = "custom";
        }

        if (double.TryParse(TemperatureEntry.Text, out var t))
        {
            App.Settings.Temperature = t;
        }

        if (int.TryParse(MaxTokensEntry.Text, out var m))
        {
            App.Settings.MaxTokens = m;
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        StatusLabel.Text = App.Settings.IsConfigured ? $"已配置 · {App.Settings.Model}" : "未配置 API Key";
        StatusLabel.TextColor = App.Settings.IsConfigured ? (Color)Application.Current!.Resources["Success"] : (Color)Application.Current!.Resources["Muted"];
    }
}
