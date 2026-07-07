using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

public partial class ElementQuizPage : ContentPage
{
    private int _index;
    private readonly Dictionary<int, string> _answers = new();
    private (string Element, string Title, string Summary, Color Color)? _result;

    public ElementQuizPage()
    {
        InitializeComponent();
        SubPageSetup.Configure(this, "返回探索");
        ShowQuestion();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = SubPageSetup.GoBackAsync(this);
        return true;
    }

    private void ShowQuestion()
    {
        ResultPanel.IsVisible = false;
        var questions = FunToolsService.ElementQuestions;
        ProgressLabel.Text = $"第 {_index + 1} / {questions.Count} 题";
        var q = questions[_index];
        QuestionLabel.Text = q.Text;
        OptionsHost.Children.Clear();

        foreach (var option in new[] { q.Fire, q.Water, q.Air, q.Earth })
        {
            var btn = new Button
            {
                Text = option,
                TextColor = Color.FromArgb("#F5F0E8"),
                BackgroundColor = Color.FromArgb("#221833"),
                BorderColor = Color.FromArgb("#9A7B2C"),
                BorderWidth = 1,
                CornerRadius = 12,
                Padding = new Thickness(12, 10)
            };
            btn.Clicked += (_, _) => OnOptionSelected(option);
            OptionsHost.Add(btn);
        }
    }

    private async void OnOptionSelected(string option)
    {
        _answers[_index] = option;
        _index++;

        if (_index >= FunToolsService.ElementQuestions.Count)
        {
            ShowResult();
            return;
        }

        ShowQuestion();
        await MainScroll.ScrollToAsync(0, 0, false);
    }

    private async void ShowResult()
    {
        _result = FunToolsService.ScoreElements(_answers);
        ResultTitleLabel.Text = _result.Value.Title;
        ResultTitleLabel.TextColor = _result.Value.Color;
        ResultSummaryLabel.Text = _result.Value.Summary;
        ResultPanel.IsVisible = true;
        await MainScroll.ScrollToAsync(ResultPanel, ScrollToPosition.Start, true);
    }

    private void OnRestartClicked(object? sender, EventArgs e)
    {
        _index = 0;
        _answers.Clear();
        _result = null;
        ShowQuestion();
    }

    private async void OnSaveImageClicked(object? sender, EventArgs e)
    {
        if (_result is null)
        {
            return;
        }

        var root = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#0B0812") };
        root.Add(ExportService.BuildHeader("四元素倾向", "趣味测试结果"));
        root.Add(ExportService.BuildTextBlock(_result.Value.Title, _result.Value.Summary, highlight: true));
        root.Add(ExportService.BuildFooter());

        var path = await ExportService.CaptureAndSaveAsync(root, "四元素测试");
        if (path is null)
        {
            await DisplayAlertAsync("保存失败", "无法生成长图。", "好的");
            return;
        }

        await DisplayAlertAsync("已保存", "长图已保存到相册或缓存目录。", "好的");
    }
}
