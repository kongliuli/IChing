using IChing.Lab.Core.Tarot;
using IChing.Lab.Engines.Tarot;
using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

public partial class DrawPage : ContentPage
{
    private TarotReading? _currentReading;
    private string _engineId = "iching-tarot-built-in";
    private string _interpretationRaw = string.Empty;
    private readonly List<TarotSpread> _spreads = [];

    public DrawPage()
    {
        InitializeComponent();
        LoadSpreads();
        UpdateHistoryPanel();
    }

    private void LoadSpreads()
    {
        _spreads.Clear();
        _spreads.AddRange(App.Tarot.ListSpreads());
        SpreadPicker.ItemsSource = _spreads.Select(s => s.TitleZh).ToList();
        SpreadPicker.SelectedIndex = _spreads.FindIndex(s => s.Id == "past-present-future");
        if (SpreadPicker.SelectedIndex < 0 && _spreads.Count > 0)
        {
            SpreadPicker.SelectedIndex = 0;
        }

        OnSpreadChanged(null, EventArgs.Empty);
    }

    private void OnSpreadChanged(object? sender, EventArgs e)
    {
        var idx = SpreadPicker.SelectedIndex;
        if (idx < 0 || idx >= _spreads.Count)
        {
            return;
        }

        var spread = _spreads[idx];
        SpreadDescriptionLabel.Text = $"{spread.Description} · {spread.CardCount} 张";
        SpreadDifficultyLabel.Text = spread.Difficulty switch
        {
            "easiest" => "入门",
            "intermediate" => "进阶",
            "advanced" => "高阶",
            _ => spread.Difficulty
        };
    }

    private void OnAdvancedToggleClicked(object? sender, EventArgs e)
    {
        AdvancedPanel.IsVisible = !AdvancedPanel.IsVisible;
        AdvancedToggleButton.Text = AdvancedPanel.IsVisible ? "▾ 高级选项" : "▸ 高级选项";
    }

    private void OnSpreadToggleClicked(object? sender, EventArgs e)
    {
        SpreadBoardPanel.IsVisible = !SpreadBoardPanel.IsVisible;
        SpreadToggleButton.Text = SpreadBoardPanel.IsVisible ? "▾ 牌阵布局" : "▸ 牌阵布局";
    }

    private async void OnDrawClicked(object? sender, EventArgs e)
    {
        var idx = SpreadPicker.SelectedIndex;
        if (idx < 0 || idx >= _spreads.Count)
        {
            return;
        }

        int? seed = int.TryParse(SeedEntry.Text, out var parsed) ? parsed : null;
        var spread = _spreads[idx];

        DrawButton.IsEnabled = false;
        var drawLabel = DrawButton.Text;
        DrawButton.Text = "洗牌中…";

        try
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch
            {
                // ponytail: 部分桌面环境不支持触觉反馈
            }

            await Task.Delay(180);

            var result = App.Tarot.Draw(spread.Id, QuestionEntry.Text, seed);
            _currentReading = result.Reading;
            _engineId = result.EngineId;
            await ShowReadingAsync(_currentReading);
            App.History.Add(_currentReading, _engineId);
            UpdateHistoryPanel();
            InterpretationPanel.IsVisible = false;
            FollowUpButton.IsVisible = false;
            InterpretationBoardLayout.Render(InterpretationBoardHost, []);
            _interpretationRaw = string.Empty;
            InterpretButton.IsEnabled = true;
            EmptyStatePanel.IsVisible = false;

            await MainScroll.ScrollToAsync(ReadingPanel, ScrollToPosition.Start, true);
        }
        finally
        {
            DrawButton.Text = drawLabel;
            DrawButton.IsEnabled = true;
        }
    }

    private void OnRedrawClicked(object? sender, EventArgs e) => OnDrawClicked(sender, e);

    private async Task ShowReadingAsync(TarotReading reading)
    {
        ReadingPanel.IsVisible = true;
        ReadingPanel.Opacity = 0;
        ReadingPanel.Scale = 0.94;
        ReadingTitleLabel.Text = reading.Question is { Length: > 0 } q
            ? $"「{q}」· {reading.SpreadTitleZh}"
            : reading.SpreadTitleZh;
        ReadingMetaLabel.Text =
            $"引擎 {_engineId} · seed={reading.Seed?.ToString() ?? "随机"} · {reading.Positions.Count} 张 · Deckaura {TarotReadingEnricher.DeckauraCoveragePercent(reading)}%";

        var cards = CardDisplayMapper.FromReading(reading);
        SpreadBoardPanel.IsVisible = true;
        SpreadToggleButton.Text = "▾ 牌阵布局";
        SpreadBoardLayout.Render(SpreadBoardHost, reading.SpreadId, cards);

        await Task.WhenAll(
            ReadingPanel.FadeToAsync(1, 320),
            ReadingPanel.ScaleToAsync(1, 320, Easing.CubicOut));
    }

    private async void OnHistorySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not HistoryEntry entry)
        {
            return;
        }

        HistoryCollection.SelectedItem = null;
        var index = App.History.GetRecent().ToList().IndexOf(entry);
        if (index < 0)
        {
            return;
        }

        await Shell.Current.GoToAsync($"history-detail?index={index}");
    }

    private async void OnInterpretClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null)
        {
            return;
        }

        if (!App.Settings.IsConfigured)
        {
            await DisplayAlertAsync("需要 API Key", "请先在「设置」页填写 API Key 后再解读。", "去设置");
            await Shell.Current.GoToAsync("//settings");
            return;
        }

        InterpretButton.IsEnabled = false;
        var interpretLabel = InterpretButton.Text;
        InterpretButton.Text = "解读中…";
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var result = await App.Interpretation.InterpretAsync(
                App.Settings,
                _currentReading,
                QuestionEntry.Text);

            var sections = InterpretationSectionParser.Parse(result.Text);
            _interpretationRaw = result.Text;
            if (sections.Count == 0)
            {
                InterpretationPanel.IsVisible = false;
            }
            else
            {
                InterpretationPanel.IsVisible = true;
                InterpretationBoardLayout.Render(InterpretationBoardHost, sections, _currentReading);
            }
            InterpretationStatusLabel.Text = result.IsFallback
                ? $"⚠ 降级/部分失败：{result.Error ?? "使用规则摘要"}"
                : $"✓ {App.Settings.Provider} · {App.Settings.Model}";
            InterpretationStatusLabel.TextColor = result.IsFallback
                ? Color.FromArgb("#E06C75")
                : Color.FromArgb("#7FD992");
            FollowUpButton.IsVisible = !string.IsNullOrWhiteSpace(_interpretationRaw);

            await MainScroll.ScrollToAsync(InterpretationPanel, ScrollToPosition.End, true);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            InterpretButton.Text = interpretLabel;
            InterpretButton.IsEnabled = true;
        }
    }

    private async void OnFollowUpClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null || string.IsNullOrWhiteSpace(_interpretationRaw))
        {
            return;
        }

        var seed = FollowUpPromptTemplates.Tarot(_currentReading, QuestionEntry.Text, _interpretationRaw);
        await Navigation.PushAsync(new FollowUpChatPage(seed.SystemPrompt, seed.Context));
    }

    private async void OnCopyInterpretationClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_interpretationRaw))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(_interpretationRaw);
        await DisplayAlertAsync("已复制", "解读内容已复制到剪贴板。", "好的");
    }

    private async void OnCopySpreadClicked(object? sender, EventArgs e)
    {
        if (_currentReading is null)
        {
            return;
        }

        var lines = _currentReading.Positions.Select(p =>
            $"[{p.PositionTitleZh}] {p.CardNameZh} · {(p.Reversed ? "逆位" : "正位")}\n{p.Meaning}");
        var text = string.Join("\n\n", lines);
        await Clipboard.Default.SetTextAsync(text);
        await DisplayAlertAsync("已复制", "牌阵内容已复制到剪贴板。", "好的");
    }

    private void UpdateHistoryPanel()
    {
        var recent = App.History.GetRecent();
        HistoryPanel.IsVisible = recent.Count > 0;
        HistoryCollection.ItemsSource = recent;
    }

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("清空历史", "确定删除全部抽牌记录？", "清空", "取消"))
        {
            return;
        }

        App.History.Clear();
        UpdateHistoryPanel();
    }
}
