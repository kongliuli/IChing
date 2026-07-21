namespace IChing.Tarot.App.Views;

/// <summary>▸/▾ 折叠块（无 Expander 依赖）。</summary>
public static class CollapsibleSection
{
    public static VerticalStackLayout Create(string label, View content, bool expanded = true)
    {
        var panel = new VerticalStackLayout
        {
            IsVisible = expanded,
            Spacing = 4,
            Children = { content }
        };

        var button = new Button
        {
            Text = Prefix(expanded) + label,
            FontSize = 12,
            Padding = new Thickness(0, 2),
            HorizontalOptions = LayoutOptions.Start,
            TextColor = Color.FromArgb("#B8AE9E"),
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0
        };

        button.Clicked += (_, _) =>
        {
            panel.IsVisible = !panel.IsVisible;
            button.Text = Prefix(panel.IsVisible) + label;
        };

        return new VerticalStackLayout
        {
            Spacing = 4,
            Children = { button, panel }
        };
    }

    private static string Prefix(bool expanded) => expanded ? "▾ " : "▸ ";
}
