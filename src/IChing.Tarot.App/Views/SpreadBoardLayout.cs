using IChing.Tarot.App.Models;
using Microsoft.Maui.Controls.Shapes;

namespace IChing.Tarot.App.Views;

/// <summary>Render each spread with a stable board layout instead of one long list.</summary>
public static class SpreadBoardLayout
{
    // ponytail: RWS cards are close to 2:3 width:height.
    private const double CardAspect = 1.5;

    public static void Render(Grid host, string spreadId, IReadOnlyList<CardDisplayItem> cards)
    {
        host.Children.Clear();
        host.RowDefinitions.Clear();
        host.ColumnDefinitions.Clear();
        host.RowSpacing = 10;
        host.ColumnSpacing = 10;
        host.HorizontalOptions = LayoutOptions.Fill;

        if (cards.Count == 0)
        {
            return;
        }

        var pageWidth = PageWidth(host);
        var maxCols = MaxColumns(spreadId, pageWidth);
        var cardWidth = ResolveCardWidth(host, maxCols, spreadId == "single-card");

        switch (spreadId)
        {
            case "single-card":
                RenderSingle(host, cards[0], cardWidth);
                break;
            case "past-present-future":
            case "situation-action-outcome":
            case "mind-body-spirit":
                RenderColumns(host, cards, pageWidth < 520 ? 1 : 3, cardWidth);
                break;
            case "choice":
                RenderGrid(host, cards, 2, 2, cardWidth);
                break;
            case "six-card":
                RenderColumns(host, cards, pageWidth < 560 ? 2 : 3, cardWidth);
                break;
            case "relationship":
                RenderRelationship(host, cards, cardWidth);
                break;
            case "horseshoe":
                RenderHorseshoe(host, cards, cardWidth);
                break;
            case "week-ahead":
                RenderWeekAhead(host, cards, cardWidth * 0.85);
                break;
            case "celtic-cross":
                RenderCelticCross(host, cards, cardWidth * 0.9, pageWidth);
                break;
            default:
                RenderColumns(host, cards, Math.Min(pageWidth < 520 ? 1 : 3, cards.Count), cardWidth);
                break;
        }
    }

    private static int MaxColumns(string spreadId, double pageWidth) => spreadId switch
    {
        "single-card" => 1,
        "celtic-cross" => pageWidth < 620 ? 1 : 5,
        "horseshoe" => 4,
        "week-ahead" => 4,
        "six-card" => pageWidth < 560 ? 2 : 3,
        "relationship" => 3,
        "choice" => 2,
        _ => pageWidth < 520 ? 1 : 3
    };

    private static double ResolveCardWidth(Grid host, int columns, bool hero)
    {
        var pageWidth = PageWidth(host);
        var inset = 16.0;
        var spacing = 10.0;
        var cols = Math.Max(1, columns);
        var width = (pageWidth - inset - spacing * (cols - 1)) / cols;

        if (hero)
        {
            return Math.Clamp(Math.Min(width, pageWidth * 0.45), 128, 250);
        }

        return Math.Clamp(width, 86, 180);
    }

    private static double PageWidth(Grid host)
    {
        var pageWidth = host.Width > 0
            ? host.Width
            : host.Window?.Page?.Width ?? 0;
        if (pageWidth <= 0)
        {
            pageWidth = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
        }

        return pageWidth;
    }

    private static void RenderSingle(Grid host, CardDisplayItem card, double cardWidth)
    {
        AddRows(host, 1);
        AddCols(host, 1);
        Place(host, CreateMiniCard(card, cardWidth), 0, 0);
    }

    private static void RenderColumns(Grid host, IReadOnlyList<CardDisplayItem> cards, int columns, double cardWidth)
    {
        var rows = (cards.Count + columns - 1) / columns;
        AddRows(host, rows);
        AddCols(host, columns);
        for (var i = 0; i < cards.Count; i++)
        {
            Place(host, CreateMiniCard(cards[i], cardWidth), i / columns, i % columns);
        }
    }

    private static void RenderGrid(Grid host, IReadOnlyList<CardDisplayItem> cards, int cols, int rows, double cardWidth)
    {
        AddRows(host, rows);
        AddCols(host, cols);
        for (var i = 0; i < cards.Count; i++)
        {
            Place(host, CreateMiniCard(cards[i], cardWidth), i / cols, i % cols);
        }
    }

    private static void RenderRelationship(Grid host, IReadOnlyList<CardDisplayItem> cards, double cardWidth)
    {
        AddRows(host, 3);
        AddCols(host, 3);
        if (cards.Count > 1) Place(host, CreateMiniCard(cards[1], cardWidth), 0, 1);
        if (cards.Count > 0) Place(host, CreateMiniCard(cards[0], cardWidth), 1, 0);
        if (cards.Count > 2) Place(host, CreateMiniCard(cards[2], cardWidth), 1, 1);
        if (cards.Count > 3) Place(host, CreateMiniCard(cards[3], cardWidth), 1, 2);
        if (cards.Count > 4) Place(host, CreateMiniCard(cards[4], cardWidth), 2, 1);
    }

    private static void RenderHorseshoe(Grid host, IReadOnlyList<CardDisplayItem> cards, double cardWidth)
    {
        AddRows(host, 3);
        AddCols(host, 4);
        var slots = new (int R, int C)[]
        {
            (0, 0), (0, 1), (0, 2), (0, 3),
            (1, 1), (1, 2),
            (2, 1), (2, 2)
        };
        for (var i = 0; i < cards.Count && i < slots.Length; i++)
        {
            var (r, c) = slots[i];
            Place(host, CreateMiniCard(cards[i], cardWidth), r, c);
        }
    }

    private static void RenderWeekAhead(Grid host, IReadOnlyList<CardDisplayItem> cards, double cardWidth)
    {
        var row = new HorizontalStackLayout { Spacing = 10 };
        foreach (var card in cards)
        {
            row.Add(CreateMiniCard(card, cardWidth));
        }

        AddHorizontalScroll(host, row);
    }

    private static void RenderCelticCross(Grid host, IReadOnlyList<CardDisplayItem> cards, double cardWidth, double pageWidth)
    {
        if (pageWidth < 620)
        {
            var row = new HorizontalStackLayout { Spacing = 10 };
            foreach (var card in cards)
            {
                row.Add(CreateMiniCard(card, Math.Clamp(pageWidth * 0.32, 94, 128)));
            }

            AddHorizontalScroll(host, row);
            return;
        }

        AddRows(host, 5);
        AddCols(host, 5);

        void Put(int index, int row, int col)
        {
            if (index < cards.Count)
            {
                Place(host, CreateMiniCard(cards[index], cardWidth), row, col);
            }
        }

        Put(4, 0, 1);
        Put(3, 1, 0);
        if (cards.Count > 1)
        {
            var center = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Fill };
            center.Add(CreateMiniCard(cards[0], cardWidth));
            center.Add(CreateMiniCard(cards[1], cardWidth));
            Grid.SetRow(center, 1);
            Grid.SetColumn(center, 1);
            host.Add(center);
        }
        else
        {
            Put(0, 1, 1);
        }

        Put(5, 1, 2);
        Put(2, 2, 1);
        Put(9, 0, 4);
        Put(8, 1, 4);
        Put(7, 2, 4);
        Put(6, 3, 4);
    }

    private static void AddHorizontalScroll(Grid host, View content)
    {
        var scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalOptions = LayoutOptions.Fill };
        scroll.Content = content;
        AddRows(host, 1);
        AddCols(host, 1);
        Grid.SetRow(scroll, 0);
        Grid.SetColumn(scroll, 0);
        host.Add(scroll);
    }

    private static void AddRows(Grid host, int count)
    {
        for (var i = 0; i < count; i++)
        {
            host.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }
    }

    private static void AddCols(Grid host, int count)
    {
        for (var i = 0; i < count; i++)
        {
            host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }
    }

    private static void Place(Grid host, View view, int row, int col)
    {
        Grid.SetRow(view, row);
        Grid.SetColumn(view, col);
        host.Add(view);
    }

    private static Border CreateMiniCard(CardDisplayItem item, double cardWidth)
    {
        var imageHeight = cardWidth * CardAspect;
        var titleSize = cardWidth >= 120 ? 12 : 10;
        var lineSize = cardWidth >= 120 ? 12 : 10;

        View cardFaceContent = item.HasImage
            ? new Image
            {
                Source = item.CardImage,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            }
            : new Label
            {
                Text = item.Abbrev,
                FontAttributes = FontAttributes.Bold,
                FontSize = titleSize + 2,
                TextColor = item.SuitAccent,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

        var cardFace = new Border
        {
            HeightRequest = imageHeight,
            MinimumHeightRequest = imageHeight,
            HorizontalOptions = LayoutOptions.Fill,
            BackgroundColor = Color.FromArgb("#1C142A"),
            Stroke = item.SuitAccent,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Rotation = item.CardRotation,
            Padding = 0,
            Content = cardFaceContent
        };

        return new Border
        {
            Padding = 6,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Start,
            BackgroundColor = Color.FromArgb("#161022"),
            Stroke = Color.FromArgb("#3A2A50"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new VerticalStackLayout
            {
                Spacing = 5,
                Children =
                {
                    new Label
                    {
                        Text = item.PositionTitle,
                        FontSize = titleSize,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#D4AF37"),
                        LineBreakMode = LineBreakMode.WordWrap,
                        HorizontalOptions = LayoutOptions.Fill
                    },
                    cardFace,
                    new Label
                    {
                        Text = item.CardLine,
                        FontSize = lineSize,
                        TextColor = Color.FromArgb("#F5F0E8"),
                        LineBreakMode = LineBreakMode.WordWrap,
                        HorizontalOptions = LayoutOptions.Fill
                    }
                }
            }
        };
    }
}
