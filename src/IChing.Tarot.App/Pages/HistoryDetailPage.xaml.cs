using IChing.Lab.Engines.Tarot;
using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

public partial class HistoryDetailPage : ContentPage
{
    private int _historyIndex = -1;

    public HistoryDetailPage()
    {
        InitializeComponent();
        SubPageSetup.Configure(this, "返回");
    }

    public HistoryDetailPage(int index) : this()
    {
        _historyIndex = index;
        LoadEntry();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = SubPageSetup.GoBackAsync(this);
        return true;
    }

    public string HistoryIndex
    {
        set
        {
            if (int.TryParse(value, out var index))
            {
                _historyIndex = index;
                LoadEntry();
            }
        }
    }

    private void LoadEntry()
    {
        var entry = App.History.GetAt(_historyIndex);
        var reading = entry?.TryGetReading();
        if (entry is null || reading is null)
        {
            TitleLabel.Text = "记录不存在";
            return;
        }

        TitleLabel.Text = reading.Question is { Length: > 0 } q
            ? $"「{q}」· {reading.SpreadTitleZh}"
            : reading.SpreadTitleZh;
        MetaLabel.Text =
            $"{entry.At.LocalDateTime:yyyy-MM-dd HH:mm} · 引擎 {UserFacingZh.EngineLabel(entry.EngineId)} · 牌库覆盖 {TarotReadingEnricher.DeckauraCoveragePercent(reading)}%";

        var cards = CardDisplayMapper.FromReading(reading);
        SpreadBoardLayout.Render(SpreadBoardHost, reading.SpreadId, cards);
    }
}
