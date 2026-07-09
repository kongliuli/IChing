using IChing.App.Services;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;

namespace IChing.App.Pages;

public partial class LiuyaoPage : ContentPage
{
    private LiuyaoNajiaResult? _currentChart;
    private LiuyaoRuleDigest? _currentDigest;
    private string _currentSummary = string.Empty;
    private string? _currentQuestion;
    private string? _currentFocus;
    private string? _currentInterpretation;
    private int? _currentSeed;
    private string _currentMethod = "coin";

    public LiuyaoPage()
    {
        InitializeComponent();
        MethodPicker.SelectedIndex = 0;
        SizeChanged += (_, _) => UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        if (double.IsNaN(Width) || Width <= 0)
        {
            return;
        }

        var wide = Width >= 920;
        var margin = wide ? 96 : 32;
        var cap = wide ? 1360 : 680;
        PageBody.WidthRequest = Math.Min(cap, Math.Max(320, Width - margin));

        ResponsiveGrid.ColumnDefinitions.Clear();
        ResponsiveGrid.RowDefinitions.Clear();
        if (wide)
        {
            ResponsiveGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(420)));
            ResponsiveGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            ResponsiveGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetColumn(PlaceholderPanel, 1);
            Grid.SetRow(PlaceholderPanel, 0);
            Grid.SetColumn(ResultPanel, 1);
            Grid.SetRow(ResultPanel, 0);
            return;
        }

        ResponsiveGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        ResponsiveGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        ResponsiveGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Grid.SetColumn(PlaceholderPanel, 0);
        Grid.SetRow(PlaceholderPanel, 1);
        Grid.SetColumn(ResultPanel, 0);
        Grid.SetRow(ResultPanel, 1);
    }

    private void OnCastClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        InterpretationPanel.IsVisible = false;
        ReadingWebView.IsVisible = false;
        ExportButton.IsVisible = false;
        FollowUpButton.IsVisible = false;
        _currentInterpretation = null;

        try
        {
            var seed = int.TryParse(SeedEntry.Text, out var parsed) ? parsed : (int?)null;
            _currentMethod = MethodPicker.SelectedIndex == 1 ? "time" : "coin";
            _currentSeed = seed;
            var chart = _currentMethod == "time"
                ? LiuyaoNajiaService.Time(DateTimeOffset.Now)
                : LiuyaoNajiaService.Coin(DateTimeOffset.Now, seed);
            var question = Blank(QuestionEntry.Text);
            var focus = Blank(FocusEntry.Text);
            var digest = ReadingSummaries.BuildLiuyaoRuleDigest(chart, question, focus);
            var original = HtmlReadingTemplate.HexagramName(chart.OriginalHexagram);
            var changed = chart.ChangedHexagram is null ? "无变卦" : $"变卦 {HtmlReadingTemplate.HexagramName(chart.ChangedHexagram)}";

            _currentChart = chart;
            _currentDigest = digest;
            _currentQuestion = question;
            _currentFocus = focus;
            _currentSummary = $"{original}，{changed}，{digest.YongShenSummary}";

            HexagramLabel.Text = $"{original} · {changed}";
            DigestLabel.Text =
                $"{digest.ShiYaoSummary}\n{digest.YingYaoSummary}\n{digest.YongShenSummary}";
            LinesView.ItemsSource = chart.Lines
                .OrderByDescending(l => l.Index)
                .Select(l => new LineRow(
                    l.Position,
                    LineGlyph(l.YinYang, l.IsChanging),
                    $"{l.SixKin} {l.StemBranch} {l.SixSpirit} {l.Role}".Trim(),
                    l.IsChanging ? "动爻" : string.Empty))
                .ToList();
            PlaceholderPanel.IsVisible = false;
            ResultPanel.IsVisible = true;
            InterpretButton.IsEnabled = true;

            App.History.Add("六爻", original, question, _currentSummary, null);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
            ResultPanel.IsVisible = false;
            PlaceholderPanel.IsVisible = true;
            InterpretButton.IsEnabled = false;
        }
    }

    private async void OnInterpretClicked(object? sender, EventArgs e)
    {
        if (_currentChart is null || _currentDigest is null)
        {
            return;
        }

        InterpretButton.IsEnabled = false;
        InterpretIndicator.IsVisible = true;
        InterpretIndicator.IsRunning = true;
        InterpretationPanel.IsVisible = true;
        ExportButton.IsVisible = false;
        FollowUpButton.IsVisible = false;
        ReadingWebView.IsVisible = false;
        InterpretStatusLabel.Text = "正在调用远程 API...";
        InterpretStatusLabel.TextColor = (Color)Application.Current!.Resources["Muted"];
        InterpretationLabel.Text = string.Empty;

        var packet = ReadingPromptPackets.LiuyaoInitial(_currentChart, _currentDigest, _currentQuestion, _currentFocus);
        var labBody = new
        {
            method = _currentMethod,
            seed = _currentSeed,
            question = _currentQuestion,
            focus = _currentFocus
        };
        var result = await App.Interpretation.InterpretLiuyaoAsync(App.Settings, labBody, packet);

        InterpretIndicator.IsRunning = false;
        InterpretIndicator.IsVisible = false;
        InterpretButton.IsEnabled = true;

        if (result.IsFallback)
        {
            InterpretStatusLabel.Text = "AI 解读不可用";
            InterpretStatusLabel.TextColor = (Color)Application.Current.Resources["Danger"];
            InterpretationLabel.Text = result.Error ?? "解读未返回内容";
            return;
        }

        _currentInterpretation = result.Text;
        InterpretStatusLabel.Text = "AI 解读展示";
        InterpretStatusLabel.TextColor = (Color)Application.Current.Resources["Jade"];
        InterpretationLabel.Text = string.Empty;
        ReadingWebView.Source = new HtmlWebViewSource
        {
            Html = HtmlReadingTemplate.BuildLiuyao(_currentChart, _currentDigest, _currentQuestion, _currentInterpretation)
        };
        ReadingWebView.IsVisible = true;
        ExportButton.IsVisible = true;
        FollowUpButton.IsVisible = true;
        App.History.Add("六爻", HtmlReadingTemplate.HexagramName(_currentChart.OriginalHexagram), _currentQuestion, _currentSummary, result.Text);
    }

    private async void OnFollowUpClicked(object? sender, EventArgs e)
    {
        if (_currentChart is null || _currentDigest is null || string.IsNullOrWhiteSpace(_currentInterpretation))
        {
            return;
        }

        var seed = FollowUpPromptTemplates.Liuyao(_currentChart, _currentDigest, _currentQuestion, _currentFocus, _currentInterpretation);
        var sessionId = App.Sessions.CreateSession("liuyao", App.Settings.InterpretTier, _currentChart, _currentDigest);
        await Navigation.PushAsync(new FollowUpChatPage(new FollowUpChatArgs("六爻追问", "liuyao", sessionId, seed.SystemPrompt, seed.Context)));
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await ImageExportService.ExportVisibleAsync("liuyao-reading");
            await DisplayAlertAsync("已导出", path, "好的");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("导出失败", ex.Message, "好的");
        }
    }

    private static string LineGlyph(string yinYang, bool changing)
    {
        var yin = yinYang.Contains('阴') || yinYang.Contains("yin", StringComparison.OrdinalIgnoreCase);
        var glyph = yin ? "━━  ━━" : "━━━━━";
        return changing ? $"{glyph}  ○" : glyph;
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record LineRow(string Position, string Glyph, string Detail, string Marker);
}
