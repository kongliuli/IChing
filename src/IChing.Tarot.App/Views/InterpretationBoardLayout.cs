using IChing.Lab.Core.Tarot;
using IChing.Tarot.App.Models;
using IChing.Tarot.App.Services;
using Microsoft.Maui.Controls.Shapes;

namespace IChing.Tarot.App.Views;

/// <summary>解读区：概览/建议通栏，牌位解读双列；每块含可折叠原牌 + AI 解读。</summary>
public static class InterpretationBoardLayout
{
    public static void Render(
        VerticalStackLayout host,
        IReadOnlyList<InterpretationSectionItem> sections,
        TarotReading? reading = null)
    {
        host.Children.Clear();
        host.Spacing = 12;

        if (sections.Count == 0)
        {
            return;
        }

        var items = reading is null ? sections : InterpretationSectionEnricher.Enrich(sections, reading);
        var overview = items.Where(s => s.Band == InterpretationLayoutBand.Overview).ToList();
        var cards = items.Where(s => s.Band is InterpretationLayoutBand.Card or InterpretationLayoutBand.General).ToList();
        var advice = items.Where(s => s.Band == InterpretationLayoutBand.Advice).ToList();

        if (overview.Count > 0)
        {
            host.Add(RenderBandRow(overview, columns: overview.Count >= 2 ? 2 : 1, compact: false));
        }

        if (cards.Count > 0)
        {
            host.Add(RenderBandRow(cards, columns: cards.Count == 1 ? 1 : 2, compact: true));
        }

        foreach (var item in advice)
        {
            host.Add(CreateSectionCard(item, highlight: true));
        }
    }

    private static View RenderBandRow(IReadOnlyList<InterpretationSectionItem> items, int columns, bool compact)
    {
        if (items.Count == 1)
        {
            return CreateSectionCard(items[0], compact: compact);
        }

        var rows = (items.Count + columns - 1) / columns;
        var grid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 10,
            HorizontalOptions = LayoutOptions.Fill
        };

        for (var c = 0; c < columns; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var r = 0; r < rows; r++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (var i = 0; i < items.Count; i++)
        {
            grid.Add(CreateSectionCard(items[i], compact: compact), i / columns, i % columns);
        }

        return grid;
    }

    private static Border CreateSectionCard(InterpretationSectionItem item, bool compact = false, bool highlight = false)
    {
        var titleSize = compact ? 13 : 15;
        var bodySize = compact ? 12 : 13;
        var bodyColor = Color.FromArgb("#D8D0C4");
        var mutedColor = Color.FromArgb("#A89F90");

        var content = new VerticalStackLayout { Spacing = 8 };

        if (!string.IsNullOrWhiteSpace(item.CardSource))
        {
            content.Add(CollapsibleSection.Create(
                "原牌内容",
                new Label
                {
                    Text = item.CardSource,
                    FontSize = bodySize,
                    LineBreakMode = LineBreakMode.WordWrap,
                    TextColor = mutedColor
                },
                expanded: false));
        }

        content.Add(CollapsibleSection.Create(
            "解读正文",
            new Label
            {
                Text = item.Body,
                FontSize = bodySize,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = bodyColor
            },
            expanded: true));

        return new Border
        {
            Padding = compact ? 12 : 14,
            HorizontalOptions = LayoutOptions.Fill,
            BackgroundColor = highlight ? Color.FromArgb("#1A2430") : Color.FromArgb("#161022"),
            Stroke = item.Accent,
            StrokeThickness = highlight ? 1.5 : 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = item.Title,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = titleSize,
                        TextColor = item.Accent,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new BoxView
                    {
                        HeightRequest = 2,
                        Color = item.Accent,
                        HorizontalOptions = LayoutOptions.Fill
                    },
                    content
                }
            }
        };
    }
}
