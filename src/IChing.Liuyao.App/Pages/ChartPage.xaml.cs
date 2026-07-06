using System.Text.Json;
using IChing.Lab.Core.Liuyao;
using IChing.Liuyao.App.Services;

namespace IChing.Liuyao.App.Pages;

public partial class ChartPage : ContentPage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private LiuyaoNajiaResult? _chart;
    private string _method = "coin";
    private DateTimeOffset _at;
    private int? _seed;
    private string? _question;

    public ChartPage()
    {
        InitializeComponent();
        MethodPicker.SelectedIndex = 0;
        CastDate.Date = DateTime.Today;
        CastTime.Time = DateTime.Now.TimeOfDay;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshHistory();
    }

    private void OnMethodChanged(object? sender, EventArgs e) =>
        SeedPanel.IsVisible = MethodPicker.SelectedIndex == 0;

    private void OnCastClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        SummaryLabel.Text = string.Empty;
        try
        {
            _method = MethodPicker.SelectedIndex == 1 ? "time" : "coin";
            var date = CastDate.Date ?? DateTime.Today;
            var dt = date.Add(CastTime.Time ?? TimeSpan.Zero);
            _at = new DateTimeOffset(dt);
            _seed = _method == "coin" && int.TryParse(SeedEntry.Text, out var s) ? s : null;
            _question = string.IsNullOrWhiteSpace(QuestionEntry.Text) ? null : QuestionEntry.Text.Trim();

            _chart = App.Casts.Cast(_method, _at, _seed);
            ShowChart(_chart);

            App.History.Add(new LiuyaoHistoryEntry(
                DateTimeOffset.Now,
                _method, _at, _seed, _question,
                HexagramLabel.Text ?? _chart.OriginalHexagram,
                JsonSerializer.Serialize(_chart, JsonOptions)));
            RefreshHistory();
        }
        catch (Exception ex)
        {
            _chart = null;
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
            ResultPanel.IsVisible = false;
        }
    }

    private void ShowChart(LiuyaoNajiaResult chart)
    {
        _chart = chart;
        HexagramLabel.Text = chart.ChangedHexagram is null
            ? chart.OriginalHexagram
            : $"{chart.OriginalHexagram} → {chart.ChangedHexagram}";

        LinesHost.Clear();
        foreach (var line in chart.Lines.OrderByDescending(l => l.Index))
        {
            var changing = line.IsChanging ? " · 动" : "";
            LinesHost.Add(new Label
            {
                Text = $"{line.Index}爻 {line.YinYang} {line.SixKin}{line.StemBranch} {line.Role}{changing}",
                TextColor = line.IsChanging
                    ? Color.FromArgb("#E8C547")
                    : Color.FromArgb("#B8AEC9"),
                FontSize = 13
            });
        }

        ResultPanel.IsVisible = true;
        MainScroll.ScrollToAsync(ResultPanel, ScrollToPosition.Start, true);
    }

    private async void OnTier0Clicked(object? sender, EventArgs e) => await InterpretAsync(0);

    private async void OnTier1Clicked(object? sender, EventArgs e) => await InterpretAsync(1);

    private async Task InterpretAsync(int tier)
    {
        if (_chart is null)
        {
            ErrorLabel.Text = "请先起卦";
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;
        Tier0Button.IsEnabled = Tier1Button.IsEnabled = false;
        InterpretBusy.IsVisible = InterpretBusy.IsRunning = true;

        try
        {
            var result = await App.Interpretation.InterpretAsync(
                App.Settings, _chart, _method, _at, _seed, _question, tier);

            SummaryLabel.Text = result.Text;
            if (!string.IsNullOrEmpty(result.Error))
            {
                SummaryLabel.Text += $"\n\n⚠ {result.Error}";
            }

            await MainScroll.ScrollToAsync(SummaryLabel, ScrollToPosition.End, true);
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = ex.Message;
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            Tier0Button.IsEnabled = Tier1Button.IsEnabled = true;
            InterpretBusy.IsRunning = false;
            InterpretBusy.IsVisible = false;
        }
    }

    private void RefreshHistory()
    {
        HistoryHost.Clear();
        var items = App.History.GetRecent();
        HistoryPanel.IsVisible = items.Count > 0;
        foreach (var entry in items)
        {
            var btn = new Button
            {
                Text = entry.DisplayLine,
                FontSize = 12,
                LineBreakMode = LineBreakMode.WordWrap,
                BackgroundColor = Color.FromArgb("#221833"),
                TextColor = Color.FromArgb("#B8AEC9")
            };
            btn.Clicked += (_, _) => RestoreHistory(entry);
            HistoryHost.Add(btn);
        }
    }

    private void RestoreHistory(LiuyaoHistoryEntry entry)
    {
        MethodPicker.SelectedIndex = entry.Method == "time" ? 1 : 0;
        SeedPanel.IsVisible = MethodPicker.SelectedIndex == 0;
        CastDate.Date = entry.CastAt.LocalDateTime.Date;
        CastTime.Time = entry.CastAt.LocalDateTime.TimeOfDay;
        SeedEntry.Text = entry.Seed?.ToString() ?? string.Empty;
        QuestionEntry.Text = entry.Question ?? string.Empty;
        SummaryLabel.Text = string.Empty;

        var chart = entry.TryGetChart();
        if (chart is not null)
        {
            _method = entry.Method;
            _at = entry.CastAt;
            _seed = entry.Seed;
            _question = entry.Question;
            ShowChart(chart);
            return;
        }

        OnCastClicked(this, EventArgs.Empty);
    }

    private void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        App.History.Clear();
        RefreshHistory();
    }
}
