// DEPRECATED: Desktop client paused. Use IChing.Lab.Api + plugins. See spec: deprecate-desktop.
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;
using IChing.Lab.Core.Tarot;

namespace IChing.Desktop;

public partial class MainWindow : Window
{
    private readonly OpenAiChatClient _client = new();
    private DesktopSettings _settings = DesktopSettingsStore.Load();
    private string _domain = "bazi";
    private object? _lastChart;
    private object? _lastDigest;
    private string? _lastQuestion;
    private string? _lastFocus;

    public MainWindow()
    {
        InitializeComponent();
        TarotSpread.ItemsSource = SpreadCatalog.List();
        TarotSpread.SelectedValue = "past-present-future";
        LoadSettingsToUi();
        ShowDomain("bazi");
    }

    private void SwitchDomain(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string domain })
        {
            ShowDomain(domain);
        }
    }

    private void ShowDomain(string domain)
    {
        _domain = domain;
        TitleText.Text = domain switch
        {
            "bazi" => "BaZi",
            "liuyao" => "Liuyao",
            "tarot" => "Tarot",
            _ => "Settings"
        };

        BaziPanel.Visibility = domain == "bazi" ? Visibility.Visible : Visibility.Collapsed;
        LiuyaoPanel.Visibility = domain == "liuyao" ? Visibility.Visible : Visibility.Collapsed;
        TarotPanel.Visibility = domain == "tarot" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPanel.Visibility = domain == "settings" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Calculate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_domain == "settings")
            {
                SaveSettings_Click(sender, e);
                return;
            }

            CalculateCurrent();
            InterpretBox.Text = "";
        }
        catch (Exception ex)
        {
            ResultBox.Text = ex.Message;
        }
    }

    private async void Interpret_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_lastChart is null)
            {
                CalculateCurrent();
            }

            var prompt = ReadingSummaries.BuildChatPrompt(_domain, _lastQuestion, _lastFocus, _lastChart!, _lastDigest);
            InterpretBox.Text = "Reading...";
            var result = await _client.InterpretAsync(_settings, prompt);
            InterpretBox.Text = result.IsFallback ? result.Error : result.Text;
        }
        catch (Exception ex)
        {
            InterpretBox.Text = ex.Message;
        }
    }

    private void CalculateCurrent()
    {
        switch (_domain)
        {
            case "bazi":
                var bazi = BaziEngine.Calculate(new BaziInput(
                    Int(BaziYear), Int(BaziMonth), Int(BaziDay), Int(BaziHour),
                    Minute: Int(BaziMinute),
                    Longitude: DoubleOrNull(BaziLongitude),
                    City: TextOrNull(BaziCity),
                    Gender: SelectedInt(BaziGender)));
                _lastChart = bazi;
                _lastFocus = TextOrNull(BaziFocus);
                _lastQuestion = null;
                _lastDigest = ReadingSummaries.BuildBaziPreview(bazi, _lastFocus);
                break;

            case "liuyao":
                var method = SelectedTag(LiuyaoMethod);
                var seed = IntOrNull(LiuyaoSeed);
                var liuyao = method == "time"
                    ? LiuyaoNajiaService.Time(DateTimeOffset.Now)
                    : LiuyaoNajiaService.Coin(DateTimeOffset.Now, seed);
                _lastChart = liuyao;
                _lastQuestion = TextOrNull(LiuyaoQuestion);
                _lastFocus = TextOrNull(LiuyaoFocus);
                _lastDigest = ReadingSummaries.BuildLiuyaoRuleDigest(liuyao, _lastQuestion, _lastFocus);
                break;

            case "tarot":
                var spreadId = TarotSpread.SelectedValue?.ToString() ?? "past-present-future";
                var tarot = TarotEngine.Draw(spreadId, TextOrNull(TarotQuestion), IntOrNull(TarotSeed));
                _lastChart = tarot;
                _lastQuestion = tarot.Question;
                _lastFocus = null;
                _lastDigest = ReadingSummaries.BuildTarotRuleDigest(tarot);
                break;
        }

        ResultBox.Text = JsonSerializer.Serialize(new { chart = _lastChart, ruleDigest = _lastDigest }, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _settings = new DesktopSettings(BaseUrlBox.Text.Trim(), ModelBox.Text.Trim(), ApiKeyBox.Password);
        DesktopSettingsStore.Save(_settings);
        ResultBox.Text = $"Saved settings to {DesktopSettingsStore.SettingsPath}";
    }

    private void LoadSettingsToUi()
    {
        BaseUrlBox.Text = _settings.BaseUrl;
        ModelBox.Text = _settings.Model;
        ApiKeyBox.Password = _settings.ApiKey ?? "";
    }

    private static int Int(TextBox box) => int.Parse(box.Text.Trim());
    private static int? IntOrNull(TextBox box) => int.TryParse(box.Text.Trim(), out var v) ? v : null;
    private static double? DoubleOrNull(TextBox box) => double.TryParse(box.Text.Trim(), out var v) ? v : null;
    private static string? TextOrNull(TextBox box) => string.IsNullOrWhiteSpace(box.Text) ? null : box.Text.Trim();

    private static int? SelectedInt(ComboBox box) =>
        int.TryParse(SelectedTag(box), out var v) ? v : null;

    private static string? SelectedTag(ComboBox box) =>
        (box.SelectedItem as ComboBoxItem)?.Tag?.ToString();
}
