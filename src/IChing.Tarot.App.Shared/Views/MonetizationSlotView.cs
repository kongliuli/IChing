using IChing.Client.Shared.Monetization;

namespace IChing.Tarot.App.Views;

/// <summary>广告/支付挂载点；NoOp 时高度为 0。</summary>
public sealed class MonetizationSlotView : ContentView
{
    public MonetizationSlotView(IMonetizationSlot slot)
    {
        if (!slot.IsEnabled)
        {
            HeightRequest = 0;
            IsVisible = false;
            return;
        }

        Content = new Border
        {
            Padding = 12,
            Stroke = Color.FromArgb("#4B3B61"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#1A1224"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = new Label
            {
                Text = $"[{slot.SlotId}] 广告位占位",
                FontSize = 12,
                TextColor = Color.FromArgb("#6E6380"),
                HorizontalTextAlignment = TextAlignment.Center
            }
        };

        Loaded += async (_, _) =>
        {
            try
            {
                await slot.ShowAsync();
            }
            catch
            {
                // ponytail: 占位失败不影响解读
            }
        };
    }
}
