using System.Text.Json;
using IChing.App.Services;
using IChing.Lab.Core.Bazi;
using IChing.Lab.Core.Readings;

namespace IChing.App.Pages;

public partial class BaziPage : ContentPage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private BaziChart? _currentChart;
    private BaziRuleDigest? _currentDigest;
    private string _currentSummary = string.Empty;
    private string? _currentFocus;
    private string? _currentInterpretation;

    public BaziPage()
    {
        InitializeComponent();
        GenderPicker.SelectedIndex = 1;
    }

    private void OnCalculateClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        InterpretationPanel.IsVisible = false;
        ReadingWebView.IsVisible = false;
        ExportButton.IsVisible = false;
        FollowUpButton.IsVisible = false;
        _currentInterpretation = null;

        try
        {
            var chart = BaziEngine.Calculate(new BaziInput(
                ReadInt(YearEntry, "年"),
                ReadInt(MonthEntry, "月"),
                ReadInt(DayEntry, "日"),
                ReadInt(HourEntry, "时"),
                Minute: ReadInt(MinuteEntry, "分"),
                Longitude: ReadDoubleOrNull(LongitudeEntry),
                City: Blank(CityEntry.Text),
                Gender: GenderPicker.SelectedIndex < 0 ? null : GenderPicker.SelectedIndex));
            var focus = Blank(FocusEntry.Text);
            var digest = ReadingSummaries.BuildBaziRuleDigest(chart, focus);

            _currentChart = chart;
            _currentDigest = digest;
            _currentFocus = focus;
            _currentSummary = $"日主 {chart.DayMaster}，{digest.PillarSummary}；{digest.YongShenSummary}";

            BaziTitleLabel.Text = $"日主 {chart.DayMaster} · {chart.WallClock}";
            YearPillarLabel.Text = chart.YearPillar.GanZhi;
            MonthPillarLabel.Text = chart.MonthPillar.GanZhi;
            DayPillarLabel.Text = chart.DayPillar.GanZhi;
            HourPillarLabel.Text = chart.HourPillar.GanZhi;
            BaziSummaryLabel.Text =
                $"农历 {chart.Lunar}\n五行偏重：{chart.WuXingSummary.Dominant}；格局：{chart.YongShen.GeJu.Pattern}；身强弱：{chart.YongShen.Strength}";
            YongshenLabel.Text = $"用神：{chart.YongShen.PrimaryYongShen}\n{chart.YongShen.Summary}";
            DaYunLabel.Text = chart.DaYun is { Count: > 0 }
                ? "大运：" + string.Join("  ", chart.DaYun.Take(5).Select(x => $"{x.StartAge}-{x.EndAge}岁 {x.GanZhi}"))
                : "大运：选择性别后显示";
            ResultPanel.IsVisible = true;
            InterpretButton.IsEnabled = true;

            App.History.Add("八字", chart.DayPillar.GanZhi, focus, _currentSummary, null);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
            ResultPanel.IsVisible = false;
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

        var result = await App.Remote.InterpretAsync(App.Settings, "八字", BuildPrompt(_currentChart, _currentDigest, _currentFocus));

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
            Html = HtmlReadingTemplate.BuildBazi(_currentChart, _currentDigest, _currentFocus, _currentInterpretation)
        };
        ReadingWebView.IsVisible = true;
        ExportButton.IsVisible = true;
        FollowUpButton.IsVisible = true;
        App.History.Add("八字", _currentChart.DayPillar.GanZhi, _currentFocus, _currentSummary, result.Text);
    }

    private async void OnFollowUpClicked(object? sender, EventArgs e)
    {
        if (_currentChart is null || _currentDigest is null || string.IsNullOrWhiteSpace(_currentInterpretation))
        {
            return;
        }

        var seed = FollowUpPromptTemplates.Bazi(_currentChart, _currentDigest, _currentFocus, _currentInterpretation);
        await Navigation.PushAsync(new FollowUpChatPage("八字追问", seed.SystemPrompt, seed.Context));
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        try
        {
            var path = await ImageExportService.ExportVisibleAsync("bazi-reading");
            await DisplayAlertAsync("已导出", path, "好的");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("导出失败", ex.Message, "好的");
        }
    }

    private static string BuildPrompt(BaziChart chart, BaziRuleDigest digest, string? focus) =>
        $"""
        关注点：{focus ?? "综合"}

        请用简体中文输出三段：
        1. 命盘重点
        2. 用神与风险
        3. 近期建议

        规则摘要：
        {JsonSerializer.Serialize(digest, JsonOptions)}

        排盘结果：
        {JsonSerializer.Serialize(chart, JsonOptions)}
        """;

    private static int ReadInt(Entry entry, string name) =>
        int.TryParse(entry.Text, out var value) ? value : throw new InvalidOperationException($"{name}不是有效数字");

    private static double? ReadDoubleOrNull(Entry entry) =>
        double.TryParse(entry.Text, out var value) ? value : null;

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
