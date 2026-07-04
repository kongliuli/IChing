using IChing.Lab.Engines.Tarot;
using IChing.Tarot.App.Services;
using IChing.Tarot.App.Views;

namespace IChing.Tarot.App.Pages;

[QueryProperty(nameof(HistoryIndex), "index")]
public partial class HistoryDetailPage : ContentPage
{
    private int _historyIndex = -1;

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

    public HistoryDetailPage()
    {
        InitializeComponent();
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
            $"{entry.At.LocalDateTime:yyyy-MM-dd HH:mm} · 引擎 {entry.EngineId} · Deckaura {TarotReadingEnricher.DeckauraCoveragePercent(reading)}%";

        var cards = CardDisplayMapper.FromReading(reading);
        var isCeltic = reading.SpreadId == "celtic-cross";
        CelticCrossHost.IsVisible = isCeltic;
        CardsCollection.IsVisible = !isCeltic;

        if (isCeltic)
        {
            CelticCrossLayout.Render(CelticCrossHost, cards);
        }
        else
        {
            CardsCollection.ItemsSource = cards;
        }
    }
}
