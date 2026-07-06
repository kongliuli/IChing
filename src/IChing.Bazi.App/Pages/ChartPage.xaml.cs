using System.Text.Json;
using IChing.Lab.Core.Bazi;
using IChing.Bazi.App.Services;

namespace IChing.Bazi.App.Pages;

public partial class ChartPage : ContentPage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private BaziChart? _chart;
    private int _year, _month, _day, _hour, _minute;
    private int? _gender;
    private int? _flowYear;
    private string? _focus;

    public ChartPage()
    {
        InitializeComponent();
        BirthDate.Date = DateTime.Today.AddYears(-30);
        BirthTime.Time = new TimeSpan(12, 0, 0);
        GenderPicker.SelectedIndex = 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshHistory();
    }

    private void OnCalculateClicked(object? sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;
        SummaryLabel.Text = string.Empty;
        try
        {
            var date = BirthDate.Date ?? DateTime.Today;
            var dt = date.Add(BirthTime.Time ?? TimeSpan.Zero);
            _gender = GenderPicker.SelectedIndex == 1 ? 0 : 1;
            _year = dt.Year;
            _month = dt.Month;
            _day = dt.Day;
            _hour = dt.Hour;
            _minute = dt.Minute;
            _focus = string.IsNullOrWhiteSpace(FocusEntry.Text) ? null : FocusEntry.Text.Trim();
            _flowYear = int.TryParse(FlowYearEntry.Text, out var fy) ? fy : null;

            var input = new BaziInput(_year, _month, _day, _hour, _minute, Gender: _gender, FlowYear: _flowYear);
            _chart = App.Charts.Calculate(input);
            ShowChart(_chart);

            var pillars = PillarsLabel.Text;
            App.History.Add(new BaziHistoryEntry(
                DateTimeOffset.Now,
                _year, _month, _day, _hour, _minute,
                _gender, _flowYear, _focus,
                pillars,
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

    private void ShowChart(BaziChart chart)
    {
        _chart = chart;
        SolarLabel.Text = $"公历 {chart.Solar}";
        LunarLabel.Text = $"农历 {chart.Lunar} · 日主 {chart.DayMaster}";
        PillarsLabel.Text =
            $"{chart.YearPillar.GanZhi}  {chart.MonthPillar.GanZhi}  {chart.DayPillar.GanZhi}  {chart.HourPillar.GanZhi}";
        var flow = chart.FlowYear is not null ? $" · {chart.FlowYear.Year}流年 {chart.FlowYear.GanZhi}" : "";
        PatternLabel.Text =
            $"格局 {chart.YongShen.GeJu.Pattern} · 身{chart.YongShen.Strength} · 用神 {chart.YongShen.PrimaryYongShen}{flow}";
        ResultPanel.IsVisible = true;
        MainScroll.ScrollToAsync(ResultPanel, ScrollToPosition.Start, true);
    }

    private async void OnTier0Clicked(object? sender, EventArgs e) => await InterpretAsync(0);

    private async void OnTier1Clicked(object? sender, EventArgs e) => await InterpretAsync(1);

    private async Task InterpretAsync(int tier)
    {
        if (_chart is null)
        {
            ErrorLabel.Text = "请先排盘";
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;
        Tier0Button.IsEnabled = Tier1Button.IsEnabled = false;
        InterpretBusy.IsVisible = InterpretBusy.IsRunning = true;

        try
        {
            var result = await App.Interpretation.InterpretAsync(
                App.Settings, _chart,
                _year, _month, _day, _hour, _minute, _gender, _flowYear, _focus,
                tier);

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

    private void RestoreHistory(BaziHistoryEntry entry)
    {
        BirthDate.Date = new DateTime(entry.Year, entry.Month, entry.Day);
        BirthTime.Time = new TimeSpan(entry.Hour, entry.Minute, 0);
        GenderPicker.SelectedIndex = entry.Gender == 0 ? 1 : 0;
        FocusEntry.Text = entry.Focus ?? string.Empty;
        FlowYearEntry.Text = entry.FlowYear?.ToString() ?? string.Empty;
        SummaryLabel.Text = string.Empty;

        var chart = entry.TryGetChart();
        if (chart is not null)
        {
            _year = entry.Year;
            _month = entry.Month;
            _day = entry.Day;
            _hour = entry.Hour;
            _minute = entry.Minute;
            _gender = entry.Gender;
            _flowYear = entry.FlowYear;
            _focus = entry.Focus;
            ShowChart(chart);
            return;
        }

        OnCalculateClicked(this, EventArgs.Empty);
    }

    private void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        App.History.Clear();
        RefreshHistory();
    }
}
