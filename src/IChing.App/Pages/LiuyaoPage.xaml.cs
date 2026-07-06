using System.Text.Json;
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

    public LiuyaoPage()
    {
        InitializeComponent();
        MethodPicker.SelectedIndex = 0;
    }

    private void OnCastClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        InterpretationPanel.IsVisible = false;

        try
        {
            var seed = int.TryParse(SeedEntry.Text, out var parsed) ? parsed : (int?)null;
            var chart = MethodPicker.SelectedIndex == 1
                ? LiuyaoNajiaService.Time(DateTimeOffset.Now)
                : LiuyaoNajiaService.Coin(DateTimeOffset.Now, seed);
            var question = Blank(QuestionEntry.Text);
            var focus = Blank(FocusEntry.Text);
            var digest = ReadingSummaries.BuildLiuyaoRuleDigest(chart, question, focus);
            var changed = chart.ChangedHexagram is null ? "无变卦" : $"变卦 {chart.ChangedHexagram}";

            _currentChart = chart;
            _currentDigest = digest;
            _currentQuestion = question;
            _currentFocus = focus;
            _currentSummary = $"{chart.OriginalHexagram}，{changed}；{digest.YongShenSummary}";

            HexagramLabel.Text = $"{chart.OriginalHexagram} · {changed}";
            DigestLabel.Text =
                $"{digest.ShiYaoSummary}\n{digest.YingYaoSummary}\n{digest.YongShenSummary}";
            LinesView.ItemsSource = chart.Lines
                .OrderByDescending(l => l.Index)
                .Select(l => new LineRow(
                    $"{l.Position} · {l.YinYang}{(l.IsChanging ? " 动" : "")}",
                    $"{l.SixKin} {l.StemBranch} {l.SixSpirit} {l.Role}".Trim()))
                .ToList();
            ResultPanel.IsVisible = true;
            InterpretButton.IsEnabled = true;

            App.History.Add("六爻", chart.OriginalHexagram, question, _currentSummary, null);
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
        InterpretStatusLabel.Text = "正在调用远程 API...";
        InterpretStatusLabel.TextColor = (Color)Application.Current!.Resources["Muted"];
        InterpretationLabel.Text = string.Empty;

        var prompt = BuildPrompt(_currentChart, _currentDigest, _currentQuestion, _currentFocus);
        var result = await App.Remote.InterpretAsync(App.Settings, "六爻", prompt);

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

        InterpretStatusLabel.Text = "AI 解读";
        InterpretStatusLabel.TextColor = (Color)Application.Current.Resources["Gold"];
        InterpretationLabel.Text = result.Text;
        App.History.Add("六爻", _currentChart.OriginalHexagram, _currentQuestion, _currentSummary, result.Text);
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

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record LineRow(string Title, string Detail);
}
