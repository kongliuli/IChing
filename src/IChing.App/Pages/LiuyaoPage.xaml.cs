using System.Text.Json;
using IChing.App.Services;
using IChing.Lab.Core.Liuyao;
using IChing.Lab.Core.Readings;

namespace IChing.App.Pages;

public partial class LiuyaoPage : ContentPage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private LiuyaoNajiaResult? _currentChart;
    private LiuyaoRuleDigest? _currentDigest;
    private string _currentSummary = string.Empty;
    private string? _currentQuestion;
    private string? _currentFocus;
    private string? _currentInterpretation;

    public LiuyaoPage()
    {
        InitializeComponent();
        MethodPicker.SelectedIndex = 0;
        SizeChanged += (_, _) => UpdateResponsiveLayout();
    }

    private void UpdateResponsiveLayout()
    {
        var wide = Width >= 900;
        ResponsiveGrid.ColumnDefinitions.Clear();
        ResponsiveGrid.RowDefinitions.Clear();
        ResponsiveGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        if (wide)
        {
            ResponsiveGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            ResponsiveGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetColumn(PlaceholderPanel, 1);
            Grid.SetRow(PlaceholderPanel, 0);
            Grid.SetColumn(ResultPanel, 1);
            Grid.SetRow(ResultPanel, 0);
            return;
        }

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
            var chart = MethodPicker.SelectedIndex == 1
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

        var result = await App.Remote.InterpretAsync(App.Settings, "六爻", BuildPrompt(_currentChart, _currentDigest, _currentQuestion, _currentFocus));

        InterpretIndicator.IsRunning = false;
        InterpretIndicator.IsVisible = false;
        InterpretButton.IsEnabled = true;

        if (result.IsFallback)
        {
            InterpretStatusLabel.Text = "AI 解读不可用";
            InterpretStatusLabel.TextColor = (Color)Application.Current.Resources["Danger"];
            InterpretationLabel.Text = result.Error ?? "远程 API 未返回内容";
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
        await Navigation.PushAsync(new FollowUpChatPage("六爻追问", seed.SystemPrompt, seed.Context));
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

    private static string BuildPrompt(
        LiuyaoNajiaResult chart,
        LiuyaoRuleDigest digest,
        string? question,
        string? focus) =>
        $"""
        问题：{question ?? "未填写"}
        关注点：{focus ?? "综合"}

        请用简体中文输出三段：
        1. 卦象核心
        2. 用神、世应、动爻判断
        3. 可执行建议

        规则摘要：
        {JsonSerializer.Serialize(digest, JsonOptions)}

        起卦结果：
        {JsonSerializer.Serialize(chart, JsonOptions)}
        """;

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
