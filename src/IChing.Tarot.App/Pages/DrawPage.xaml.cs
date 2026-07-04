using IChing.Lab.Core.Tarot;
using IChing.Tarot.App.Models;
using IChing.Tarot.App.Services;

namespace IChing.Tarot.App.Pages;

public partial class DrawPage : ContentPage
{
    private TarotReading? _currentReading;
    private string _engineId = "iching-tarot-built-in";
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

    private async void OnDrawClicked(object? sender, EventArgs e)
    {
        var idx = SpreadPicker.SelectedIndex;
        if (idx < 0 || idx >= _spreads.Count)
        {
            return;
        }

        int? seed = int.TryParse(SeedEntry.Text, out var parsed) ? parsed : null;
        var spread = _spreads[idx];

        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // ponytail: 部分桌面环境不支持触觉反馈
        }

        var result = App.Tarot.Draw(spread.Id, QuestionEntry.Text, seed);
        _currentReading = result.Reading;
        _engineId = result.EngineId;
        ShowReading(_currentReading);
        App.History.Add(_currentReading, _engineId);
        UpdateHistoryPanel();
        InterpretationPanel.IsVisible = false;
        InterpretButton.IsEnabled = true;
        EmptyStatePanel.IsVisible = false;

        await MainScroll.ScrollToAsync(ReadingPanel, ScrollToPosition.Start, true);
    }

    private void OnRedrawClicked(object? sender, EventArgs e) => OnDrawClicked(sender, e);

    private void ShowReading(TarotReading reading)
    {
        ReadingPanel.IsVisible = true;
        ReadingTitleLabel.Text = reading.Question is { Length: > 0 } q
            ? $"「{q}」· {reading.SpreadTitleZh}"
            : reading.SpreadTitleZh;
        ReadingMetaLabel.Text =
            $"引擎 {_engineId} · seed={reading.Seed?.ToString() ?? "随机"} · {reading.Positions.Count} 张 · Deckaura 牌义";

        CardsCollection.ItemsSource = reading.Positions.Select(p => new CardDisplayItem
        {
            PositionTitle = p.PositionTitleZh,
            CardLine = $"{p.CardNameZh}（{p.CardName}）· {(p.Reversed ? "逆位" : "正位")}",
            Meaning = p.Meaning,
            SuitAccent = TarotCardVisual.SuitAccent(p.CardName),
            Abbrev = TarotCardVisual.Abbrev(p.CardNameZh),
            IsReversed = p.Reversed,
            CardImage = TarotCardVisual.TryImage(p.CardName)
        }).ToList();
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
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var result = await App.Interpretation.InterpretAsync(
                App.Settings,
                _currentReading,
                QuestionEntry.Text);

            InterpretationPanel.IsVisible = true;
            InterpretationLabel.Text = result.Text;
            InterpretationStatusLabel.Text = result.IsFallback
                ? $"⚠ 降级/部分失败：{result.Error ?? "使用规则摘要"}"
                : $"✓ {App.Settings.Provider} · {App.Settings.Model}";
            InterpretationStatusLabel.TextColor = result.IsFallback
                ? Color.FromArgb("#E06C75")
                : Color.FromArgb("#7FD992");

            await MainScroll.ScrollToAsync(InterpretationPanel, ScrollToPosition.End, true);
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            InterpretButton.IsEnabled = true;
        }
    }

    private async void OnCopyInterpretationClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InterpretationLabel.Text))
        {
            return;
        }

        await Clipboard.Default.SetTextAsync(InterpretationLabel.Text);
        await DisplayAlertAsync("已复制", "解读内容已复制到剪贴板。", "好的");
    }

    private void UpdateHistoryPanel()
    {
        var recent = App.History.GetRecent();
        HistoryPanel.IsVisible = recent.Count > 0;
        HistoryCollection.ItemsSource = recent.Select(h =>
            $"{h.At.LocalDateTime:MM-dd HH:mm} · {h.SpreadTitle} · {h.CardCount}张" +
            (h.Question is { Length: > 0 } q ? $" · {Truncate(q, 20)}" : string.Empty)).ToList();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
