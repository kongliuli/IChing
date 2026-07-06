using IChing.Tarot.App.Models;
using IChing.Tarot.App.Services;

namespace IChing.Tarot.App.Pages;

public partial class ExplorePage : ContentPage
{
    public ExplorePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, true);
        _ = RenderModulesAsync();
    }

    private async Task RenderModulesAsync()
    {
        var config = await ExploreModuleCatalog.LoadAsync();
        HeaderTitleLabel.Text = config.HeaderTitle;
        HeaderSubtitleLabel.Text = config.HeaderSubtitle;
        ModulesHost.Children.Clear();

        foreach (var section in config.Sections.Where(s => s.Enabled))
        {
            var items = section.Items.Where(i => i.Enabled).ToList();
            if (items.Count == 0)
            {
                continue;
            }

            ModulesHost.Add(new Label
            {
                Text = section.Title,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#D4AF37"),
                Margin = new Thickness(0, 8, 0, 0)
            });

            foreach (var item in items)
            {
                ModulesHost.Add(BuildModuleCard(item));
            }
        }
    }

    private View BuildModuleCard(ExploreModuleItemConfig item)
    {
        var card = new Border
        {
            Stroke = Color.FromArgb("#9A7B2C"),
            StrokeThickness = 1,
            BackgroundColor = Color.FromArgb("#161022"),
            Padding = 16,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 }
        };

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(new Label
        {
            Text = item.Title,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#B794F6"),
            FontSize = 18
        });

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            stack.Add(new Label
            {
                Text = item.Description,
                TextColor = Color.FromArgb("#B8AE9E"),
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            });
        }

        var btn = new Button
        {
            Text = item.ButtonText,
            TextColor = Color.FromArgb("#1A1208"),
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#D4AF37"),
            CornerRadius = 24,
            Padding = new Thickness(14, 12)
        };
        btn.Clicked += async (_, _) => await ExploreModuleRouter.NavigateAsync(this, item);
        stack.Add(btn);
        card.Content = stack;
        return card;
    }
}
