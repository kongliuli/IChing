using IChing.Tarot.App.Models;
using Microsoft.Maui.Controls.Shapes;

namespace IChing.Tarot.App.Views;

/// <summary>凯尔特十字简版布局：中心十字 + 其余牌两列网格。</summary>
public static class CelticCrossLayout
{
    private static readonly (int Row, int Col)[] RingSlots =
    [
        (0, 0), (0, 2),
        (1, 0), (1, 2),
        (2, 0), (2, 2),
        (3, 0), (3, 2)
    ];

    public static void Render(Grid host, IReadOnlyList<CardDisplayItem> cards)
    {
        host.Children.Clear();
        host.RowDefinitions.Clear();
        host.ColumnDefinitions.Clear();
        host.RowSpacing = 8;
        host.ColumnSpacing = 8;

        if (cards.Count < 2)
        {
            return;
        }

        host.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        host.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        host.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        host.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        host.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        var center = new VerticalStackLayout { Spacing = 8 };
        center.Add(CreateMiniCard(cards[0]));
        if (cards.Count > 1)
        {
            center.Add(CreateMiniCard(cards[1]));
        }

        Grid.SetRow(center, 1);
        Grid.SetColumn(center, 1);
        Grid.SetColumnSpan(center, 1);
        host.Add(center);

        var ring = cards.Skip(2).Take(RingSlots.Length).ToList();
        for (var i = 0; i < ring.Count; i++)
        {
            var (row, col) = RingSlots[i];
            var cell = CreateMiniCard(ring[i]);
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, col);
            host.Add(cell);
        }
    }

    private static Border CreateMiniCard(CardDisplayItem item)
    {
        var cardFace = new Border
        {
            WidthRequest = 40,
            HeightRequest = 58,
            BackgroundColor = Color.FromArgb("#221833"),
            Stroke = item.SuitAccent,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Rotation = item.CardRotation,
            Content = item.HasImage
                ? new Image { Source = item.CardImage, Aspect = Aspect.AspectFill }
                : new Label
                {
                    Text = item.Abbrev,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = item.SuitAccent,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
        };

        return new Border
        {
            Padding = 8,
            BackgroundColor = Color.FromArgb("#161022"),
            Stroke = Color.FromArgb("#9A7B2C"),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label
                    {
                        Text = item.PositionTitle,
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#D4AF37")
                    },
                    cardFace,
                    new Label
                    {
                        Text = item.CardLine,
                        FontSize = 10,
                        TextColor = Color.FromArgb("#F5F0E8")
                    }
                }
            }
        };
    }
}
