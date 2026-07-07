using IChing.Tarot.App.Models;
using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

public partial class PersonalityQuizPage : ContentPage
{
    private readonly string _quizId;
    private PersonalityQuizDefinition? _quiz;
    private int _index;
    private readonly Dictionary<int, int> _answers = new();
    private PersonalityQuizResult? _result;

    public PersonalityQuizPage(string quizId)
    {
        _quizId = quizId;
        InitializeComponent();
        SubPageSetup.Configure(this, "返回探索");
    }

    protected override bool OnBackButtonPressed()
    {
        _ = SubPageSetup.GoBackAsync(this);
        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_quiz is not null)
        {
            return;
        }

        try
        {
            _quiz = await PersonalityQuizCatalog.LoadAsync(_quizId);
            Title = _quiz.Title;
            TitleLabel.Text = _quiz.Title;
            SubtitleLabel.Text = _quiz.Subtitle ?? string.Empty;
            DisclaimerLabel.Text = _quiz.Disclaimer;
            QuizHost.IsVisible = true;
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            ShowQuestion();
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsRunning = false;
            var detail = ex.InnerException is null ? ex.Message : $"{ex.Message}\n{ex.InnerException.Message}";
            await DisplayAlertAsync("加载失败", detail, "返回");
            await SubPageSetup.GoBackAsync(this);
        }
    }

    private void ShowQuestion()
    {
        if (_quiz is null)
        {
            return;
        }

        ResultPanel.IsVisible = false;
        ProgressLabel.Text = $"第 {_index + 1} / {_quiz.Questions.Count} 题";
        var q = _quiz.Questions[_index];
        QuestionLabel.Text = q.Text;
        OptionsHost.Children.Clear();

        for (var i = 0; i < q.Options.Count; i++)
        {
            var optionIndex = i;
            var btn = new Button
            {
                Text = q.Options[i].Text,
                TextColor = Color.FromArgb("#F5F0E8"),
                BackgroundColor = Color.FromArgb("#221833"),
                BorderColor = Color.FromArgb("#9A7B2C"),
                BorderWidth = 1,
                CornerRadius = 12,
                Padding = new Thickness(12, 10)
            };
            btn.Clicked += (_, _) => OnOptionSelected(optionIndex);
            OptionsHost.Add(btn);
        }
    }

    private async void OnOptionSelected(int optionIndex)
    {
        if (_quiz is null)
        {
            return;
        }

        _answers[_index] = optionIndex;
        _index++;

        if (_index >= _quiz.Questions.Count)
        {
            ShowResult();
            return;
        }

        ShowQuestion();
        await MainScroll.ScrollToAsync(0, 0, false);
    }

    private async void ShowResult()
    {
        if (_quiz is null)
        {
            return;
        }

        _result = PersonalityQuizScorer.Score(_quiz, _answers);
        ResultCodeLabel.Text = _result.Code;
        ResultTitleLabel.Text = _result.Title;
        ResultSummaryLabel.Text = _result.Summary;
        RenderDimensionBars(_result.DimensionBars);
        RenderSections(_result.Sections);
        ResultPanel.IsVisible = true;
        ProgressLabel.Text = "测评完成";
        await MainScroll.ScrollToAsync(ResultPanel, ScrollToPosition.Start, true);
    }

    private void RenderDimensionBars(IReadOnlyList<PersonalityDimensionBar> bars)
    {
        DimensionBarsHost.Children.Clear();
        if (bars.Count == 0)
        {
            return;
        }

        DimensionBarsHost.Add(new Label
        {
            Text = "维度分布",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#D4AF37")
        });

        foreach (var bar in bars)
        {
            DimensionBarsHost.Add(BuildDimensionBar(bar));
        }
    }

    private static View BuildDimensionBar(PersonalityDimensionBar bar)
    {
        var rightPct = 100 - bar.LeftPercent;
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(new Label
        {
            Text = bar.Title,
            FontSize = 12,
            TextColor = Color.FromArgb("#B8AE9E")
        });

        var barRow = new Microsoft.Maui.Controls.Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(bar.LeftPercent, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(rightPct, GridUnitType.Star))
            },
            HeightRequest = 8,
            ColumnSpacing = 2
        };
        var leftBox = new BoxView { Color = Color.FromArgb("#D4AF37"), CornerRadius = 4 };
        var rightBox = new BoxView { Color = Color.FromArgb("#3D3550"), CornerRadius = 4 };
        barRow.Add(leftBox);
        barRow.Add(rightBox);
        Microsoft.Maui.Controls.Grid.SetColumn(rightBox, 1);
        stack.Add(barRow);

        if (!string.IsNullOrEmpty(bar.RightLabel))
        {
            var leftCaption = new Label
            {
                Text = $"{bar.LeftLabel} {bar.LeftPercent}%",
                FontSize = 11,
                TextColor = Color.FromArgb("#F5F0E8")
            };
            var rightCaption = new Label
            {
                Text = $"{bar.RightLabel} {rightPct}%",
                FontSize = 11,
                HorizontalTextAlignment = TextAlignment.End,
                TextColor = Color.FromArgb("#B8AE9E")
            };
            var captionRow = new Microsoft.Maui.Controls.Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                }
            };
            captionRow.Add(leftCaption);
            captionRow.Add(rightCaption);
            Microsoft.Maui.Controls.Grid.SetColumn(rightCaption, 1);
            stack.Add(captionRow);
        }
        else
        {
            stack.Add(new Label
            {
                Text = bar.LeftLabel,
                FontSize = 11,
                TextColor = Color.FromArgb("#F5F0E8")
            });
        }

        return stack;
    }

    private void RenderSections(IReadOnlyList<PersonalityReportSection> sections)
    {
        ResultSectionsHost.Children.Clear();
        foreach (var section in sections)
        {
            var body = new Label
            {
                Text = section.Content,
                FontSize = 13,
                TextColor = Color.FromArgb("#C9C0D4"),
                LineBreakMode = LineBreakMode.WordWrap
            };
            ResultSectionsHost.Add(CollapsibleSection.Create(section.Title, body, expanded: section.Title is "类型概览" or "兴趣概览" or "得分分布"));
        }
    }

    private void OnRestartClicked(object? sender, EventArgs e)
    {
        _index = 0;
        _answers.Clear();
        _result = null;
        DimensionBarsHost.Children.Clear();
        ResultSectionsHost.Children.Clear();
        ShowQuestion();
    }

    private async void OnSaveImageClicked(object? sender, EventArgs e)
    {
        if (_quiz is null || _result is null)
        {
            return;
        }

        try
        {
            var root = new VerticalStackLayout { Spacing = 0, BackgroundColor = Color.FromArgb("#0B0812") };
            root.Add(ExportService.BuildHeader(_quiz.Title, _result.Code));
            root.Add(ExportService.BuildTextBlock(_result.Title, _result.Summary, highlight: true));

            if (_result.DimensionBars.Count > 0)
            {
                root.Add(ExportService.BuildTextBlock("维度分布", FormatBarsForExport(_result.DimensionBars)));
            }

            foreach (var section in _result.Sections)
            {
                root.Add(ExportService.BuildTextBlock(section.Title, section.Content));
            }

            root.Add(new Label
            {
                Text = _quiz.Disclaimer,
                FontSize = 11,
                TextColor = Color.FromArgb("#6E6380"),
                Margin = new Thickness(20, 8),
                LineBreakMode = LineBreakMode.WordWrap
            });
            root.Add(ExportService.BuildFooter());

            var path = await ExportService.CaptureAndSaveAsync(root, _quiz.Id);
            if (path is null)
            {
                await DisplayAlertAsync("保存失败", "无法生成长图，请稍后重试。", "好的");
                return;
            }

            var inGallery = path.StartsWith("content://", StringComparison.OrdinalIgnoreCase)
                            || path.Contains("Pictures", StringComparison.OrdinalIgnoreCase);
            await DisplayAlertAsync(
                "已保存",
                inGallery ? "长图已保存到相册 Pictures/IChingTarot。" : $"长图已保存到：{path}",
                "好的");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PersonalityQuiz] save image: {ex}");
            await DisplayAlertAsync("保存失败", "导出时发生错误，请稍后重试。", "好的");
        }
    }

    private static string FormatBarsForExport(IReadOnlyList<PersonalityDimensionBar> bars)
    {
        var lines = new List<string>();
        foreach (var bar in bars)
        {
            if (string.IsNullOrEmpty(bar.RightLabel))
            {
                lines.Add($"· {bar.Title}：{bar.LeftLabel}");
                continue;
            }

            var rightPct = 100 - bar.LeftPercent;
            lines.Add($"· {bar.Title}：{bar.LeftLabel} {bar.LeftPercent}% / {bar.RightLabel} {rightPct}%");
        }

        return string.Join("\n", lines);
    }
}
